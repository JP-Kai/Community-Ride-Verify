using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RideSafeSA.Bot;
using RideSafeSA.Bot.Conversations;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

// Unlike ASP.NET Core's WebApplication.CreateBuilder, the plain Host
// builder doesn't wire up user-secrets automatically - it has to be
// added explicitly. This reads from the secret store tied to this
// project's UserSecretsId (see the .csproj), never from a committed file.
builder.Configuration.AddUserSecrets<Program>();

var botToken = builder.Configuration["BotToken"]
    ?? throw new InvalidOperationException(
        "BotToken is not configured. Run: dotnet user-secrets set \"BotToken\" \"<token-from-BotFather>\"");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5080";

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
builder.Services.AddSingleton<ConversationStore>();
builder.Services.AddSingleton<UpdateHandler>();
builder.Services.AddHttpClient<RideSafeApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});
builder.Services.AddHostedService<BotHostedService>();

var host = builder.Build();
await host.RunAsync();
