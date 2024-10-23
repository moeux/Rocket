using Discord;
using Discord.WebSocket;

namespace Rocket.Extensions;

public static class SocketRoleExtensions
{
    public static bool IsPrivileged(this SocketRole role)
    {
        return role is { IsEveryone: true } ||
               role is { IsManaged: true } ||
               role.Permissions.Has(GuildPermission.Administrator) ||
               role.Guild.CurrentUser.Roles.All(botRole => botRole.Position <= role.Position);
    }
}