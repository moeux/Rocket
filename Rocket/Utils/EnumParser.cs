namespace Rocket.Utils;

public static class EnumParser
{
    public static T? ParseToEnumValue<T>(string? value, bool ignoreCase = true) where T : struct
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (Enum.TryParse<T>(value, ignoreCase, out var enumValue))
            return enumValue;

        return null;
    }

    public static List<T> ParseToEnumValues<T>(IEnumerable<string>? values, bool ignoreCase = true) where T : struct
    {
        List<T> enumValues = [];

        if (values == null)
            return enumValues;

        foreach (var str in values)
        {
            if (string.IsNullOrWhiteSpace(str))
                continue;

            if (Enum.TryParse<T>(str, ignoreCase, out var value))
                enumValues.Add(value);
        }

        return enumValues;
    }
}