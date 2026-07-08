using System.Security.Cryptography;
using System.Text;

namespace RideSafeSA.Api.Filters;

// Shared-secret gate for /api/admin/*. Not real user auth (no accounts,
// no roles) - just enough to stop anyone who finds the URL from reading
// raw report details or moderating reports. See README "Before any real
// deployment" for what a production auth setup would need instead.
public class AdminApiKeyFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = config["AdminApiKey"];

        if (string.IsNullOrEmpty(expectedKey))
        {
            return Results.Problem(
                "Server misconfiguration: AdminApiKey is not set.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("X-Admin-Key", out var providedKey) ||
            !FixedTimeEquals(providedKey.ToString(), expectedKey))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }

    // Constant-time comparison so response timing can't be used to guess
    // the key one byte at a time.
    private static bool FixedTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return aBytes.Length == bBytes.Length && CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
