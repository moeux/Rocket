using AutoCommand.Handler;
using Discord;
using Discord.WebSocket;
using Rocket.Extensions;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Rocket.Commands;

public class AssignCommandHandler : ICommandHandler
{
    private readonly ILogger _logger;

    public AssignCommandHandler(string logPath)
    {
        _logger = new LoggerConfiguration()
            .Destructure.ByTransformingWhere<dynamic>(type => typeof(SocketUser).IsAssignableFrom(type),
                user => new { user.Id, user.Username })
            .Destructure.ByTransforming<SocketSlashCommand>(command => new { command.Id, command.CommandName })
            .Destructure.ByTransforming<SocketRole>(role => new
                { role.Id, role.Name, role.Position, Permissions = role.Permissions.RawValue })
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
            .ForContext<AssignCommandHandler>();
    }

    public string CommandName => "assign";
    public bool IsLongRunning => false;

    public async Task<string> HandleAsync(SocketSlashCommand command)
    {
        var logger = _logger.ForContext("Token", command.Token);
        var response = command.UserLocale == "de"
            ? "Etwas ist schiefgelaufen"
            : "Something went wrong";
        var options = command.Data?.Options;
        var option = options?.First();

        if (options == null || options.Count == 0)
        {
            logger.Error("Received empty command options");
            return response;
        }

        if (option is not { Type: ApplicationCommandOptionType.Role } || option.Value is not SocketRole role)
        {
            logger.Error("Expected role but got {Type}: {$Value}", option?.Type, option?.Value);
            return response;
        }

        logger.Information("User {@User} executed command {@Command} with role {@Role}", command.User, command, role);

        var user = role.Guild.GetUser(command.User.Id);

        if (role.IsPrivileged())
            return command.UserLocale == "de"
                ? "Diese Rolle kannst du dir nicht selber zuweisen"
                : "You can't assign this role to yourself";

        if (user.Roles.Any(userRole => userRole.Id == role.Id))
            return command.UserLocale == "de"
                ? "Dieser Rolle bist du bereits zugewiesen"
                : "You've already been assigned to this role";

        await user.AddRoleAsync(role);

        logger.Information("Assigned role {@Role} to user {@User}", role, user);

        return command.UserLocale == "de"
            ? $"Du wurdest der Rolle {role.Mention} zugewiesen"
            : $"You've been assigned to the role {role.Mention}";
    }
}