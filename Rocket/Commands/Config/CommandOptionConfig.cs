using Discord;
using static Rocket.Utils.EnumParser;

namespace Rocket.Commands.Config;

public class CommandOptionConfig
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Type { get; set; }
    public bool? IsRequired { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsAutocomplete { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public IEnumerable<string>? ChannelTypes { get; set; }
    public IEnumerable<CommandOptionConfig>? Options { get; set; }
    public IEnumerable<CommandChoiceConfig>? Choices { get; set; }
    public IDictionary<string, string>? NameLocalizations { get; set; }
    public IDictionary<string, string>? DescriptionLocalizations { get; set; }

    public SlashCommandOptionBuilder ToSlashCommandOption()
    {
        var builder = new SlashCommandOptionBuilder()
            .WithName(Name)
            .WithDescription(Description);

        var type = ParseToEnumValue<ApplicationCommandOptionType>(Type);
        if (type.HasValue)
            builder.WithType(type.Value);

        if (IsRequired.HasValue)
            builder = builder.WithRequired(IsRequired.Value);

        if (IsDefault.HasValue)
            builder = builder.WithDefault(IsDefault.Value);

        if (IsAutocomplete.HasValue)
            builder = builder.WithAutocomplete(IsAutocomplete.Value);

        if (MinValue.HasValue)
            builder = builder.WithMinValue(MinValue.Value);

        if (MaxValue.HasValue)
            builder = builder.WithMaxValue(MaxValue.Value);

        if (MinLength.HasValue)
            builder = builder.WithMinLength(MinLength.Value);

        if (MaxLength.HasValue)
            builder = builder.WithMaxLength(MaxLength.Value);

        var channelTypes = ParseToEnumValues<ChannelType>(ChannelTypes);
        if (channelTypes.Count != 0)
            foreach (var channelType in channelTypes)
                builder.AddChannelType(channelType);

        if (Options != null && Options.Any())
            builder.AddOptions(Options.Select(option => option.ToSlashCommandOption()).ToArray());

        if (Choices != null && Choices.Any())
            foreach (var choice in Choices)
            {
                dynamic? value = null;

                switch (type)
                {
                    case ApplicationCommandOptionType.String:
                        if (!string.IsNullOrWhiteSpace(choice.Value))
                            value = choice.Value;
                        break;
                    case ApplicationCommandOptionType.Integer:
                        if (long.TryParse(choice.Value, out var longValue))
                            value = longValue;
                        break;
                    case ApplicationCommandOptionType.Number:
                        if (double.TryParse(choice.Value, out var doubleValue))
                            value = doubleValue;
                        break;
                    default:
                        value = null;
                        break;
                }

                if (value == null)
                    continue;

                builder.AddChoice(choice.Name, value, choice.NameLocalizations);
            }

        if (NameLocalizations != null && NameLocalizations.Any())
            builder.WithNameLocalizations(NameLocalizations);

        if (DescriptionLocalizations != null && DescriptionLocalizations.Any())
            builder.WithDescriptionLocalizations(DescriptionLocalizations);

        return builder;
    }
}