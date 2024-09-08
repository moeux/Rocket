using Discord;
using Discord.WebSocket;
using Serilog;
using Serilog.Core;

namespace Rocket;

internal static class Program
{
    private static DiscordSocketClient _client = null!;
    private static Logger _logger = null!;

    private static async Task Main()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            DefaultRetryMode = RetryMode.RetryRatelimit,
            LogLevel = LogSeverity.Info,
            GatewayIntents = GatewayIntents.Guilds
        });
        _logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(GetEnvironmentVariable("ROCKET_LOG_FILE", "rocket-.log"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
        var token = GetEnvironmentVariable("ROCKET_DISCORD_TOKEN");

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.Fatal("Environment variable `ROCKET_DISCORD_TOKEN` is missing");
            return;
        }

        await InitializeDiscordClient(token);

        await Task.Delay(Timeout.Infinite);
    }

    private static string GetEnvironmentVariable(string name, string fallback = "")
    {
        return Environment.GetEnvironmentVariable(name) ?? fallback;
    }

    private static async Task InitializeDiscordClient(string token)
    {
        _client.Log += message =>
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    _logger.Fatal(message.Exception, message.Message);
                    break;
                case LogSeverity.Error:
                    _logger.Error(message.Exception, message.Message);
                    break;
                case LogSeverity.Warning:
                    _logger.Warning(message.Exception, message.Message);
                    break;
                case LogSeverity.Info:
                    _logger.Information(message.Exception, message.Message);
                    break;
                case LogSeverity.Verbose:
                    _logger.Verbose(message.Exception, message.Message);
                    break;
                case LogSeverity.Debug:
                    _logger.Debug(message.Exception, message.Message);
                    break;
                default:
                    _logger.Information(message.Exception, message.Message);
                    break;
            }

            return Task.CompletedTask;
        };

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        await _client.SetCustomStatusAsync("Ready for takeoff!");
        await _client.SetStatusAsync(UserStatus.DoNotDisturb);
    }
}