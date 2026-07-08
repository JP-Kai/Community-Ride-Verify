namespace RideSafeSA.Api.Models;

public class Report
{
    public int Id { get; set; }

    public int DriverId { get; set; }
    public Driver? Driver { get; set; }

    public ReportCategory Category { get; set; }

    // Free-text detail is stored, but is NEVER returned by the public
    // /drivers/check endpoint — only aggregate counts are. Raw detail
    // is only ever exposed via the /admin endpoints, for moderators.
    public string? Detail { get; set; }

    // MVP: just a string reference (e.g. a filename or blob-storage key),
    // not an actual upload pipeline yet. See README "What's stubbed".
    public string? PhotoReference { get; set; }

    public string? SocialMediaLink { get; set; }

    public bool HasEvidence => PhotoReference is not null || SocialMediaLink is not null;

    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ModeratedAt { get; set; }
}
