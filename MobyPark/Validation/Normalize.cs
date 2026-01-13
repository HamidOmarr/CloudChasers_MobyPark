namespace MobyPark.Validation;

public static class Normalize
{
    public static string Safe(this string? input) =>
        input ?? string.Empty;

    public static string TrimSafe(this string? input) =>
        input.Safe().Trim();

    public static string Upper(this string? input) =>
        input.TrimSafe().ToUpperInvariant();

    public static string Lower(this string? input) =>
        input.TrimSafe().ToLowerInvariant();

    public static string Capitalize(this string? input)
    {
        input = input.TrimSafe();
        return string.IsNullOrEmpty(input)
            ? string.Empty
            : $"{char.ToUpper(input[0])}{input[1..].ToLower()}";
    }
}