using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RideSafeSA.Api.Data;
using RideSafeSA.Api.Dtos;
using RideSafeSA.Api.Filters;
using RideSafeSA.Api.Models;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --- Services ---------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=ridesafe.db"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => // gives you a free test UI at /swagger
{
    // Lets you click "Authorize" in Swagger UI and paste the admin key
    // once, instead of typing it on every /api/admin/* request.
    options.AddSecurityDefinition("AdminApiKey", new OpenApiSecurityScheme
    {
        Name = "X-Admin-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Required for /api/admin/* endpoints."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "AdminApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

// Basic anti-spam defense on report submission: 5 reports per 10 minutes
// per caller IP. Deliberately loose (a genuine rider might submit a
// couple of legitimate reports in a session) - this stops mass/scripted
// spam, not a determined single abuser with multiple IPs.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("ReportSubmission", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Too many reports submitted recently. Please try again later." },
            cancellationToken);
    };
});

var app = builder.Build();

// Create the SQLite file + tables automatically on first run.
// NOTE: fine for an MVP. Once this evolves past a solo prototype,
// switch to EF Core migrations (dotnet ef migrations add ...) instead
// of EnsureCreated(), so schema changes are tracked and reversible.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter();

// --- Helpers ------------------------------------------------------------

// Uppercase + strip everything except letters/digits, so "CA 123-456",
// "ca123456" and "CA123456" all resolve to the same driver record.
static string NormalizePlate(string plate) =>
    new string(plate.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

// --- Public endpoint: check a driver ------------------------------------

app.MapPost("/api/drivers/check", async (CheckDriverRequest req, AppDbContext db) =>
{
    var normalized = NormalizePlate(req.LicensePlate);

    var driver = await db.Drivers
        .Include(d => d.Reports)
        .FirstOrDefaultAsync(d => d.LicensePlateNormalized == normalized);

    if (driver is null)
    {
        return Results.Ok(new CheckDriverResponse(
            DriverKnown: false,
            Name: req.Name,
            ConfirmedReportCount: 0,
            PendingReportCount: 0,
            ConfirmedByCategory: new List<CategoryCount>(),
            Summary: "No record found for this driver yet. That doesn't guarantee " +
                     "a safe ride — it just means nothing has been reported here."
        ));
    }

    var confirmed = driver.Reports.Where(r => r.Status == ReportStatus.Confirmed).ToList();
    var pendingCount = driver.Reports.Count(r => r.Status == ReportStatus.Pending);

    var byCategory = confirmed
        .GroupBy(r => r.Category)
        .Select(g => new CategoryCount(g.Key, g.Count()))
        .ToList();

    // Deliberately vague at low counts, more direct at higher counts.
    // Never expose raw report text here — see Report.Detail comment.
    string summary = confirmed.Count switch
    {
        0 when pendingCount >= 2 =>
            $"{pendingCount} unconfirmed reports from different riders, still under review. " +
            "Not yet verified, but worth extra caution.",
        0 when pendingCount > 0 =>
            $"{pendingCount} unconfirmed report(s) awaiting review. Nothing verified yet.",
        0 => "No confirmed reports on this driver.",
        1 => "1 confirmed report on this driver. Consider this a caution, not a verdict.",
        _ => $"{confirmed.Count} confirmed reports on this driver across " +
             $"{byCategory.Count} categor{(byCategory.Count == 1 ? "y" : "ies")}. Please take extra care."
    };

    return Results.Ok(new CheckDriverResponse(
        DriverKnown: true,
        Name: driver.Name,
        ConfirmedReportCount: confirmed.Count,
        PendingReportCount: pendingCount,
        ConfirmedByCategory: byCategory,
        Summary: summary
    ));
});

// --- Public endpoint: submit a report -----------------------------------

app.MapPost("/api/reports", async (SubmitReportRequest req, AppDbContext db) =>
{
    var normalized = NormalizePlate(req.LicensePlate);

    var driver = await db.Drivers
        .FirstOrDefaultAsync(d => d.LicensePlateNormalized == normalized);

    if (driver is null)
    {
        driver = new Driver
        {
            Name = req.DriverName,
            LicensePlateNormalized = normalized
        };
        db.Drivers.Add(driver);
        await db.SaveChangesAsync(); // need driver.Id before attaching the report
    }

    var report = new Report
    {
        DriverId = driver.Id,
        Category = req.Category,
        Detail = req.Detail,
        PhotoReference = req.PhotoReference,
        SocialMediaLink = req.SocialMediaLink,
        Status = ReportStatus.Pending
    };

    db.Reports.Add(report);
    await db.SaveChangesAsync();

    return Results.Ok(new SubmitReportResponse(
        ReportId: report.Id,
        Status: report.Status.ToString(),
        Message: "Thank you. This report is pending review and is not yet visible " +
                 "to anyone checking this driver."
    ));
}).RequireRateLimiting("ReportSubmission");

// --- Admin endpoints (moderation) ---------------------------------------
// Gated behind a shared API key (see AdminApiKeyFilter) - set AdminApiKey
// in config/environment and send it as the X-Admin-Key header. This is a
// stopgap, not real auth (no accounts/roles) - see README "Before any
// real deployment" for what a production setup would still need.

var admin = app.MapGroup("/api/admin").AddEndpointFilter<AdminApiKeyFilter>();

admin.MapGet("/reports/pending", async (AppDbContext db) =>
{
    var pendingReports = await db.Reports
        .Include(r => r.Driver)
        .Where(r => r.Status == ReportStatus.Pending)
        .ToListAsync();

    var driverIds = pendingReports.Select(r => r.DriverId).Distinct().ToList();

    var reportCountsByDriver = await db.Reports
        .Where(r => driverIds.Contains(r.DriverId) && r.Status != ReportStatus.Rejected)
        .GroupBy(r => r.DriverId)
        .Select(g => new { DriverId = g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.DriverId, x => x.Count);

    var prioritized = pendingReports
        .Select(r => new PendingReportDto(
            r.Id,
            r.Driver!.Name,
            r.Driver!.LicensePlateNormalized,
            r.Category,
            CategorySeverity.Of(r.Category),
            r.Detail,
            r.PhotoReference,
            r.SocialMediaLink,
            r.HasEvidence,
            CorroboratingReportCount: reportCountsByDriver.GetValueOrDefault(r.DriverId, 1) - 1,
            r.CreatedAt))
        .OrderByDescending(dto => dto.Severity)
        .ThenByDescending(dto => dto.CorroboratingReportCount)
        .ThenByDescending(dto => dto.HasEvidence)
        .ThenBy(dto => dto.CreatedAt)
        .ToList();

    return Results.Ok(prioritized);
});

admin.MapPost("/reports/{id}/decision", async (int id, ModerationDecisionRequest req, AppDbContext db) =>
{
    var report = await db.Reports.FindAsync(id);
    if (report is null) return Results.NotFound();

    report.Status = req.Approve ? ReportStatus.Confirmed : ReportStatus.Rejected;
    report.ModeratedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Ok(new { report.Id, report.Status });
});

app.Run();
