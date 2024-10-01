using Discord.WebSocket;
using Serilog;
using Serilog.Core;

namespace Rocket.Commands.Handler;

public class DefaultCommandHandler
{
    private readonly Dictionary<string, ICommandHandler> _handlers;
    private readonly Logger _logger;

    public DefaultCommandHandler()
    {
        _handlers = new Dictionary<string, ICommandHandler>();
        _logger = _logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    public DefaultCommandHandler(IEnumerable<ICommandHandler> handlers) : this()
    {
        foreach (var handler in handlers) Register(handler);
    }

    public bool Register(ICommandHandler handler)
    {
        _logger.Information("Registering command handler: {Handler}", handler);
        return _handlers.TryAdd(handler.CommandName, handler);
    }

    public bool Unregister(ICommandHandler handler)
    {
        _logger.Information("Unregistering command handler: {Handler}", handler);
        return _handlers.Remove(handler.CommandName);
    }

    public async Task HandleAsync(SocketSlashCommand command)
    {
        var response = string.Empty;

        if (_handlers.TryGetValue(command.CommandName, out var handler))
        {
            if (handler.IsLongRunning) await command.DeferAsync(true);

            _logger.Information("Handling command {{{Command}}} with handler {{{Handler}}}", command, handler);

            response = await handler.HandleAsync(command);
        }
        else
        {
            _logger.Error("No registered command handler for command: {Command}", command);
        }

        if (string.IsNullOrWhiteSpace(response))
            response = command.UserLocale == "de"
                ? "Etwas ist schiefgelaufen"
                : "Something went wrong";

        if (handler is { IsLongRunning: true })
            await command.FollowupAsync(response, ephemeral: true);
        else
            await command.RespondAsync(response, ephemeral: true);
    }
}