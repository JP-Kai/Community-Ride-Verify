using RideSafeSA.Api.Models;

namespace RideSafeSA.Api.Dtos;

public record PendingReportDto(
    int ReportId,
    string DriverName,
    string LicensePlate,
    ReportCategory Category,
    Severity Severity,
    string? Detail,
    string? PhotoReference,
    string? SocialMediaLink,
    bool HasEvidence,
    int CorroboratingReportCount,
    DateTime CreatedAt
);

public record ModerationDecisionRequest(bool Approve);
