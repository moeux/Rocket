using Discord.WebSocket;

namespace Rocket.Commands.Handler;

public interface ICommandHandler
{
    public string CommandName { get; }

    public bool IsLongRunning { get; }

    public Task<string> HandleAsync(SocketSlashCommand command);
}