namespace RideSafeSA.Api.Models;

// A report starts life as Pending and is invisible to anyone doing a
// driver check. A human moderator promotes it to Confirmed (counts
// toward the driver's flag status) or Rejected (kept for audit trail,
// but never shown or counted). This is the core anti-abuse mechanism:
// no single anonymous submission can ever directly flag a driver.
public enum ReportStatus
{
    Pending,
    Confirmed,
    Rejected
}
