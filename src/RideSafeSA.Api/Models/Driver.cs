namespace RideSafeSA.Api.Models;

public class Driver
{
    public int Id { get; set; }

    // What the rider typed in / what we parsed from a shared trip.
    public string Name { get; set; } = string.Empty;

    // Normalized (uppercase, no spaces) so "CA 123-456" and "ca123456"
    // match the same driver. See Program.cs NormalizePlate().
    public string LicensePlateNormalized { get; set; } = string.Empty;

    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;

    public List<Report> Reports { get; set; } = new();
}
