namespace RideSafeSA.Bot;

// Mirrors RideSafeSA.Api's Dtos/Models exactly - field names, JSON shape,
// and ReportCategory's numeric order all have to match the API by hand,
// since the bot and the API are separate projects/processes that only
// ever talk over HTTP. If the API's enum order or DTO shape changes,
// this file has to change too.
public enum ReportCategory
{
    UnwantedComments,
    Harassment,
    UnsafeDriving,
    RouteDeviation,
    PhysicalThreat,
    Other
}

public record CheckDriverRequest(string Name, string LicensePlate);

public record CategoryCount(ReportCategory Category, int Count);

public record CheckDriverResponse(
    bool DriverKnown,
    string Name,
    int ConfirmedReportCount,
    int PendingReportCount,
    List<CategoryCount> ConfirmedByCategory,
    string Summary
);

public record SubmitReportRequest(
    string DriverName,
    string LicensePlate,
    ReportCategory Category,
    string? Detail,
    string? PhotoReference,
    string? SocialMediaLink
);

public record SubmitReportResponse(int ReportId, string Status, string Message);
