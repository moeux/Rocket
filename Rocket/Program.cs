using AutoCommand.Handler;
using AutoCommand.Utils;
using Discord;
using Discord.WebSocket;
using Rocket.Commands;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;

namespace Rocket;

internal static class Program
{
    private static readonly string CommandPath = EnvironmentUtils.GetVariable("ROCKET_COMMAND_PATH", "config");
    private static readonly string LogPath = EnvironmentUtils.GetVariable("ROCKET_LOG_FILE", "rocket-.log");
    private static readonly string Token = EnvironmentUtils.GetVariable("ROCKET_DISCORD_TOKEN");
    private static readonly string ChannelName = EnvironmentUtils.GetVariable("ROCKET_DYNAMIC_CHANNEL_NAME", "Lounge");

    private static readonly DiscordSocketClient Client = new(new DiscordSocketConfig
    {
        DefaultRetryMode = RetryMode.RetryRatelimit,
        LogLevel = LogSeverity.Info,
        GatewayIntents = GatewayIntents.Guilds
    });

    private static readonly Logger Logger = new LoggerConfiguration()
        .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
        .WriteTo.File(LogPath, rollingInterval: RollingInterval.Day)
        .CreateLogger();

    private static readonly DefaultCommandHandler CommandHandler = new(logPath: LogPath);

    private static async Task Main()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            Logger.Fatal("Environment variable `ROCKET_DISCORD_TOKEN` is missing");
            return;
        }

        InitializeDiscordClient();

        await Client.LoginAsync(TokenType.Bot, Token);
        await Client.StartAsync();

        await Task.Delay(Timeout.Infinite);
    }

    private static void InitializeDiscordClient()
    {
        Client.Log += message =>
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    Logger.Fatal(message.Exception, message.Message);
                    break;
                case LogSeverity.Error:
                    Logger.Error(message.Exception, message.Message);
                    break;
                case LogSeverity.Warning:
                    Logger.Warning(message.Exception, message.Message);
                    break;
                case LogSeverity.Info:
                    Logger.Information(message.Exception, message.Message);
                    break;
                case LogSeverity.Verbose:
                    Logger.Verbose(message.Exception, message.Message);
                    break;
                case LogSeverity.Debug:
                    Logger.Debug(message.Exception, message.Message);
                    break;
                default:
                    Logger.Information(message.Exception, message.Message);
                    break;
            }

            return Task.CompletedTask;
        };
        Client.Ready += () => Client.CreateSlashCommands(CommandPath);
        Client.Ready += () =>
        {
            CommandHandler.Register(new AssignCommandHandler(LogPath));
            Client.SlashCommandExecuted += command => CommandHandler.HandleAsync(command);
            return Task.CompletedTask;
        };
        Client.Ready += async () =>
        {
            await Client.SetCustomStatusAsync("Ready for takeoff!");
            await Client.SetStatusAsync(UserStatus.DoNotDisturb);
        };
        Client.UserVoiceStateUpdated += async (_, _, state) => await RestoreVoiceChannels(state);
    }

    private static async Task RestoreVoiceChannels(SocketVoiceState state)
    {
        if (state.VoiceChannel.Category is not SocketCategoryChannel category
            || category.GetPermissionOverwrite(Client.CurrentUser) is not { ManageChannel: PermValue.Allow })
            return;

        var emptyChannels = category.Channels.Where(channel => channel.Users.Count == 0).ToArray();

        switch (emptyChannels.Length)
        {
            case > 1:
                var orderedChannels = emptyChannels.OrderByDescending(channel => channel.Position).ToArray();
                var removeChannels = orderedChannels[1..];

                await Task.WhenAll(
                    removeChannels.Select(channel => channel.DeleteAsync(new RequestOptions
                    {
                        AuditLogReason = "Obsolete dynamic voice channel"
                    }))
                );

                break;
            case 0:
                await category.Guild.CreateVoiceChannelAsync(
                    ChannelName,
                    properties =>
                    {
                        var position = category.Channels.MaxBy(channel => channel.Position)?.Position;

                        properties.CategoryId = category.Id;
                        properties.Position = position ?? category.Position + 1;
                    },
                    new RequestOptions
                    {
                        AuditLogReason = "Created dynamic voice channel"
                    }
                );

                break;
        }
    }
}