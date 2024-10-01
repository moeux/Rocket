using Discord;
using Newtonsoft.Json;
using static Rocket.Utils.EnumParser;

namespace Rocket.Commands.Config;

public class CommandConfig
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsDefaultPermission { get; set; }
    public bool? IsNsfw { get; set; }

    [JsonIgnore] public bool IsGuildCommand => GuildId.GetValueOrDefault() != default;

    public ulong? GuildId { get; set; }
    public IEnumerable<string>? DefaultMemberPermissions { get; set; }
    public IEnumerable<string>? IntegrationTypes { get; set; }
    public IEnumerable<string>? ContextTypes { get; set; }
    public IEnumerable<CommandOptionConfig>? Options { get; set; }
    public IDictionary<string, string>? NameLocalizations { get; set; }
    public IDictionary<string, string>? DescriptionLocalizations { get; set; }

    public SlashCommandProperties ToSlashCommand()
    {
        var builder = new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription(Description);

        if (IsDefaultPermission.HasValue)
            builder.WithDefaultPermission(IsDefaultPermission.Value);

        if (IsNsfw.HasValue)
            builder.WithNsfw(IsNsfw.Value);

        var defaultMemberPermissions = ParseToEnumValues<GuildPermission>(DefaultMemberPermissions);
        if (defaultMemberPermissions.Count != 0)
            builder.WithDefaultMemberPermissions(defaultMemberPermissions.Aggregate((current, next) => current | next));

        var integrationTypes = ParseToEnumValues<ApplicationIntegrationType>(IntegrationTypes);
        if (integrationTypes.Count != 0)
            builder.WithIntegrationTypes(integrationTypes.ToArray());

        var contextTypes = ParseToEnumValues<InteractionContextType>(ContextTypes);
        if (contextTypes.Count != 0)
            builder.WithContextTypes(contextTypes.ToArray());

        if (Options != null && Options.Any())
            builder.AddOptions(Options.Select(option => option.ToSlashCommandOption()).ToArray());

        if (NameLocalizations != null && NameLocalizations.Any())
            builder.WithNameLocalizations(NameLocalizations);

        if (DescriptionLocalizations != null && DescriptionLocalizations.Any())
            builder.WithDescriptionLocalizations(DescriptionLocalizations);

        return builder.Build();
    }
}