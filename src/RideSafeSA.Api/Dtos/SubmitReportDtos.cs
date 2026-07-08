using RideSafeSA.Api.Models;

namespace RideSafeSA.Api.Dtos;

public record SubmitReportRequest(
    string DriverName,
    string LicensePlate,
    ReportCategory Category,
    string? Detail,
    string? PhotoReference
);

public record SubmitReportResponse(int ReportId, string Status, string Message);
