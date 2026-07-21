namespace RideSafeSA.Bot.Conversations;

public enum ConversationState
{
    Idle,
    CheckAwaitingName,
    CheckAwaitingPlate,
    ReportAwaitingName,
    ReportAwaitingPlate,
    ReportAwaitingCategory,
    ReportAwaitingDetail,
    ReportAwaitingEvidence
}
