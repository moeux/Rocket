using Discord;

namespace Rocket.Extensions;

public static class ChannelCollectionExtensions
{
    public static IEnumerable<T> OrderByDiscordSorting<T>(this IEnumerable<T> channels) where T : IGuildChannel
    {
        return channels
            .OrderBy(channel => channel.ChannelType)
            .ThenBy(channel => channel.Position)
            .ThenBy(channel => channel.Id)
            .ToArray();
    }
}