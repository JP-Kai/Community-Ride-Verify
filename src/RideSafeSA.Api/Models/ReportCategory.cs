namespace RideSafeSA.Api.Models;

// Deliberately a fixed, small list rather than free-text categories.
// Free-text categories are harder to aggregate safely and easier to abuse
// (e.g. someone writing something defamatory directly into a "category" field).
public enum ReportCategory
{
    UnwantedComments,
    Harassment,
    UnsafeDriving,
    RouteDeviation,
    PhysicalThreat,
    Other
}
