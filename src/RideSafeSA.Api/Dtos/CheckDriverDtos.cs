using RideSafeSA.Api.Models;

namespace RideSafeSA.Api.Dtos;

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
