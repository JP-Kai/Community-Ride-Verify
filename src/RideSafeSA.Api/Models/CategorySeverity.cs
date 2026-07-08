namespace RideSafeSA.Api.Models;

public static class CategorySeverity
{
    public static Severity Of(ReportCategory category) => category switch
    {
        ReportCategory.PhysicalThreat => Severity.High,
        ReportCategory.Harassment => Severity.High,
        ReportCategory.UnsafeDriving => Severity.Medium,
        ReportCategory.RouteDeviation => Severity.Medium,
        ReportCategory.UnwantedComments => Severity.Medium,
        ReportCategory.Other => Severity.Low,
        _ => Severity.Low
    };
}
