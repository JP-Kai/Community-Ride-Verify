using Microsoft.Extensions.Hosting;
using Telegram.Bot;

namespace RideSafeSA.Bot;

public class BotHostedService(ITelegramBotClient botClient, UpdateHandler updateHandler) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var me = await botClient.GetMeAsync(stoppingToken);
        Console.WriteLine($"RideSafeSA bot started as @{me.Username}");

        botClient.StartReceiving(
            updateHandler.HandleUpdateAsync,
            updateHandler.HandlePollingErrorAsync,
            cancellationToken: stoppingToken);

        // StartReceiving launches its own background polling loop and
        // returns immediately, so keep this service "running" until the
        // host shuts down - otherwise BackgroundService would consider
        // its job done and the host would exit right away.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
