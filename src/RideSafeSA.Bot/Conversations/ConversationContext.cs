namespace RideSafeSA.Bot.Conversations;

// Tracks where a single Telegram user is partway through a multi-step
// command. Telegram messages arrive one at a time, so this is the "form"
// - its answers are collected turn by turn instead of all at once.
public class ConversationContext
{
    public ConversationState State { get; set; } = ConversationState.Idle;
    public string? DriverName { get; set; }
    public string? LicensePlate { get; set; }
    public ReportCategory? Category { get; set; }
    public string? Detail { get; set; }

    public void Reset()
    {
        State = ConversationState.Idle;
        DriverName = null;
        LicensePlate = null;
        Category = null;
        Detail = null;
    }
}
