using Discord.WebSocket;
using Rocket.Extensions;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Rocket.Handler;

public class NewsRoleAssignmentButtonHandler(DiscordSocketClient client, string customId, string roleId, string logPath)
{
    private readonly ILogger _logger = new LoggerConfiguration()
        .Destructure.ByTransformingWhere<dynamic>(type => typeof(SocketUser).IsAssignableFrom(type),
            user => new { user.Id, user.Username })
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
        .ForContext<NewsRoleAssignmentButtonHandler>();

    private readonly ulong _roleId = ulong.Parse(roleId);

    public async Task Handle(SocketMessageComponent component)
    {
        if (component.Data.CustomId != customId) return;

        await component.DeferAsync(true);
        var response = await GetResponse(component);
        await component.FollowupAsync(response, ephemeral: true);
    }

    private async Task<string> GetResponse(SocketMessageComponent component)
    {
        var logger = _logger.ForContext("Token", component.Token);
        var userLocale = component.UserLocale;
        var guildId = component.GuildId.GetValueOrDefault();
        var response = userLocale == "de"
            ? "Etwas ist schiefgelaufen"
            : "Something went wrong";

        logger.Information("User {@User} triggered button interaction {@CustomId}",
            component.User, component.Data.CustomId);

        if (guildId == default)
        {
            logger.Error("Button interaction has no Guild ID");
            return response;
        }

        var guild = client.GetGuild(guildId);
        var role = guild?.GetRole(_roleId);
        var user = guild?.GetUser(component.User.Id);

        if (guild is null || role is null || user is null)
        {
            logger.Error("Could not retrieve role ({RoleId}) and user ({UserId}) from guild ({GuildId})",
                _roleId, component.User.Id, guildId);
            return response;
        }

        if (role.IsPrivileged())
            return userLocale == "de"
                ? "Diese Rolle kannst du dir nicht selber zuweisen"
                : "You can't assign this role to yourself";

        if (user.Roles.Any(userRole => userRole.Id == role.Id))
        {
            await user.RemoveRoleAsync(role);

            logger.Information("Removed role {@Role} from user {@User}", role, user);

            return userLocale == "de"
                ? $"Du wurdest der von Rolle {role.Mention} entfernt und erhältst ab sofort keine Benachrichtigungen mehr"
                : $"You have been removed from the {role.Mention} role and will no longer receive notifications";
        }

        await user.AddRoleAsync(role);

        logger.Information("Assigned role {@Role} to user {@User}", role, user);

        return userLocale == "de"
            ? $"Du wurdest der Rolle {role.Mention} zugewiesen und erhältst ab sofort Benachrichtigungen"
            : $"You've been assigned to the {role.Mention} role and will receive notifications from now on";
    }
}