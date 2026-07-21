using RideSafeSA.Bot.Conversations;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace RideSafeSA.Bot;

public class UpdateHandler(RideSafeApiClient apiClient, ConversationStore store)
{
    private static readonly string[] CategoryLabels = Enum.GetNames<ReportCategory>();

    public Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.CallbackQuery is { } callbackQuery)
        {
            return HandleCallbackQueryAsync(bot, callbackQuery, ct);
        }

        if (update.Message is { Text: { } text } message)
        {
            return HandleMessageAsync(bot, message.Chat.Id, text, ct);
        }

        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken ct)
    {
        var context = store.GetOrCreate(chatId);

        switch (text)
        {
            case "/start":
                context.Reset();
                await bot.SendTextMessageAsync(chatId,
                    "Welcome to RideSafeSA.\n\n" +
                    "/check - see if a driver has any reports\n" +
                    "/report - report a driver\n" +
                    "/cancel - cancel whatever you're doing",
                    cancellationToken: ct);
                return;

            case "/cancel":
                context.Reset();
                await bot.SendTextMessageAsync(chatId, "Cancelled.", cancellationToken: ct);
                return;

            case "/check":
                context.Reset();
                context.State = ConversationState.CheckAwaitingName;
                await bot.SendTextMessageAsync(chatId, "What's the driver's name?", cancellationToken: ct);
                return;

            case "/report":
                context.Reset();
                context.State = ConversationState.ReportAwaitingName;
                await bot.SendTextMessageAsync(chatId, "Who's the driver? (name)", cancellationToken: ct);
                return;
        }

        await HandleConversationStepAsync(bot, chatId, context, text, ct);
    }

    private async Task HandleConversationStepAsync(
        ITelegramBotClient bot, long chatId, ConversationContext context, string text, CancellationToken ct)
    {
        switch (context.State)
        {
            case ConversationState.CheckAwaitingName:
                context.DriverName = text;
                context.State = ConversationState.CheckAwaitingPlate;
                await bot.SendTextMessageAsync(chatId, "And the license plate?", cancellationToken: ct);
                break;

            case ConversationState.CheckAwaitingPlate:
                context.LicensePlate = text;
                var checkResult = await apiClient.CheckDriverAsync(context.DriverName!, context.LicensePlate!, ct);
                await bot.SendTextMessageAsync(chatId, checkResult.Summary, cancellationToken: ct);
                context.Reset();
                break;

            case ConversationState.ReportAwaitingName:
                context.DriverName = text;
                context.State = ConversationState.ReportAwaitingPlate;
                await bot.SendTextMessageAsync(chatId, "License plate?", cancellationToken: ct);
                break;

            case ConversationState.ReportAwaitingPlate:
                context.LicensePlate = text;
                context.State = ConversationState.ReportAwaitingCategory;
                await bot.SendTextMessageAsync(chatId, "What kind of report is this?",
                    replyMarkup: BuildCategoryKeyboard(),
                    cancellationToken: ct);
                break;

            case ConversationState.ReportAwaitingDetail:
                context.Detail = text.Equals("skip", StringComparison.OrdinalIgnoreCase) ? null : text;
                context.State = ConversationState.ReportAwaitingEvidence;
                await bot.SendTextMessageAsync(chatId,
                    "Optional: paste a link to a photo or social media post as evidence, or send \"skip\".",
                    cancellationToken: ct);
                break;

            case ConversationState.ReportAwaitingEvidence:
                var evidence = text.Equals("skip", StringComparison.OrdinalIgnoreCase) ? null : text;
                var submitResult = await apiClient.SubmitReportAsync(new SubmitReportRequest(
                    context.DriverName!,
                    context.LicensePlate!,
                    context.Category!.Value,
                    context.Detail,
                    PhotoReference: null,
                    SocialMediaLink: evidence), ct);
                await bot.SendTextMessageAsync(chatId, submitResult.Message, cancellationToken: ct);
                context.Reset();
                break;

            // Category is picked via the inline keyboard buttons (see
            // HandleCallbackQueryAsync), not typed text - if we get here
            // while awaiting it, the user typed instead of tapping.
            case ConversationState.ReportAwaitingCategory:
                await bot.SendTextMessageAsync(chatId, "Please tap one of the buttons above.", cancellationToken: ct);
                break;

            case ConversationState.Idle:
            default:
                await bot.SendTextMessageAsync(chatId,
                    "Not sure what you mean. Try /check, /report, or /cancel.", cancellationToken: ct);
                break;
        }
    }

    private async Task HandleCallbackQueryAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
    {
        // Always acknowledge the tap first, or Telegram shows a loading
        // spinner on the button until it times out.
        await bot.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: ct);

        if (callbackQuery.Message is not { } message || callbackQuery.Data is not { } data) return;

        var chatId = message.Chat.Id;
        var context = store.GetOrCreate(chatId);

        // Guards against a tap on a stale keyboard left over from an
        // earlier, already-finished or cancelled /report flow.
        if (context.State != ConversationState.ReportAwaitingCategory || !Enum.TryParse<ReportCategory>(data, out var category))
        {
            return;
        }

        context.Category = category;
        context.State = ConversationState.ReportAwaitingDetail;

        // Remove the buttons so the same keyboard can't be tapped twice.
        await bot.EditMessageReplyMarkupAsync(chatId, message.MessageId, replyMarkup: null, cancellationToken: ct);

        await bot.SendTextMessageAsync(chatId, "Describe what happened (or send \"skip\").", cancellationToken: ct);
    }

    private static InlineKeyboardMarkup BuildCategoryKeyboard() =>
        new(CategoryLabels
            .Chunk(2)
            .Select(row => row.Select(label => InlineKeyboardButton.WithCallbackData(label, label))));

    public Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
    {
        Console.Error.WriteLine($"Polling error: {exception}");
        return Task.CompletedTask;
    }
}
