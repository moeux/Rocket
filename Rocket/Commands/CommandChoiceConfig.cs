namespace Rocket.Commands;

public class CommandChoiceConfig
{
    public string? Name { get; set; }
    public string? Value { get; set; }
    public IDictionary<string, string>? NameLocalizations { get; set; }
}