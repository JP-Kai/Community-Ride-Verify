using RideSafeSA.Api.Models;

namespace RideSafeSA.Api.Dtos;

public record PendingReportDto(
    int ReportId,
    string DriverName,
    string LicensePlate,
    ReportCategory Category,
    string? Detail,
    string? PhotoReference,
    DateTime CreatedAt
);

public record ModerationDecisionRequest(bool Approve);
