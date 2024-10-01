using Discord;
using Discord.WebSocket;
using Serilog;
using Serilog.Core;

namespace Rocket.Commands.Handler;

public class AssignCommandHandler : ICommandHandler
{
    private readonly Logger _logger;

    public AssignCommandHandler()
    {
        _logger = _logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    public string CommandName => "assign";
    public bool IsLongRunning => false;

    public async Task<string> HandleAsync(SocketSlashCommand command)
    {
        var response = command.UserLocale == "de"
            ? "Etwas ist schiefgelaufen"
            : "Something went wrong";
        var options = command.Data?.Options;
        var option = options?.First();

        if (options == null || options.Count == 0)
        {
            _logger.Error("Received empty command options");
            return response;
        }

        if (option is not { Type: ApplicationCommandOptionType.Role } || option.Value is not SocketRole role)
        {
            _logger.Error("Expected role but got {Type}: {Value}", option?.Type, option?.Value);
            return response;
        }

        _logger.Information(
            "User {Username} executed command {CommandName} with role {Role}",
            command.User.Username, command.CommandName, role);

        var user = role.Guild.GetUser(command.User.Id);

        if (IsPrivilegedRole(role))
            return command.UserLocale == "de"
                ? "Diese Rolle kannst du dir nicht selber zuweisen"
                : "You can't assign this role to yourself";

        if (user.Roles.Any(userRole => userRole.Id == role.Id))
            return command.UserLocale == "de"
                ? "Dieser Rolle bist du bereits zugewiesen"
                : "You've already been assigned to this role";

        await user.AddRoleAsync(role);

        _logger.Information("Assigned role {Role} to user {User}", role, user);

        return command.UserLocale == "de"
            ? $"Du wurdest der Rolle {role.Mention} zugewiesen"
            : $"You've been assigned to the role {role.Mention}";
    }

    private static bool IsPrivilegedRole(SocketRole role)
    {
        return role is { IsEveryone: true, IsManaged: true } ||
               role.Permissions.Has(GuildPermission.Administrator) ||
               role.Guild.CurrentUser.Roles.All(botRole => botRole.Position >= role.Position);
    }
}