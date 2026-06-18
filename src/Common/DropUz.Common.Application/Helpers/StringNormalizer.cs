namespace DropUz.Common.Application.Helpers;

public static class StringNormalizer
{
    public static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
