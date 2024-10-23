using Discord;
using Discord.WebSocket;
using Rocket.Extensions;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Rocket.Handler;

public class DynamicVoiceChannelHandler(string channelName, string logPath)
{
    private readonly ILogger _logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.Console(
            theme: AnsiConsoleTheme.Literate,
            outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Properties:j}{NewLine}{Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            logPath,
            outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Properties:j}{NewLine}{Message:lj}{NewLine}{Exception}",
            rollingInterval: RollingInterval.Day)
        .CreateLogger()
        .ForContext<DynamicVoiceChannelHandler>();

    public async Task RestoreVoiceChannels(SocketVoiceState oldState, SocketVoiceState newState, SocketSelfUser botUser)
    {
        if (oldState.VoiceChannel != null) await Restore(oldState, botUser);

        if (newState.VoiceChannel != null) await Restore(newState, botUser);
    }

    private async Task Restore(SocketVoiceState state, SocketSelfUser botUser)
    {
        var logger = _logger.ForContext("VoiceSession", state.VoiceSessionId);

        if (state.VoiceChannel?.Category is not SocketCategoryChannel category
            || category.GetPermissionOverwrite(botUser) is not { ManageChannel: PermValue.Allow })
            return;

        var channels = category.Channels
            .OfType<SocketVoiceChannel>()
            .OrderByDiscordSorting()
            .ToArray();
        var emptyChannels = channels
            .Where(channel => channel.ConnectedUsers.Count == 0)
            .ToArray();

        switch (emptyChannels.Length)
        {
            case > 1:
                var removeChannels = emptyChannels[..^1];
                await Task.WhenAll(
                    removeChannels.Select(channel => channel.DeleteAsync(new RequestOptions
                    {
                        AuditLogReason = "Obsolete dynamic voice channel"
                    }))
                );

                logger.Information("Removed {Length} obsolete dynamic channels", removeChannels.Length);

                break;
            case 0:
                var channel = await category.Guild.CreateVoiceChannelAsync(
                    channelName,
                    properties =>
                    {
                        var position = channels.MaxBy(channel => channel.Position)?.Position;
                        properties.CategoryId = category.Id;
                        properties.Position = position + 1 ?? channels.Length;
                    },
                    new RequestOptions
                    {
                        AuditLogReason = "Created dynamic voice channel"
                    }
                );

                logger.Information("Created dynamic voice channel '{Channel}' ({Id})", channel, channel.Id);

                break;
        }
    }
}