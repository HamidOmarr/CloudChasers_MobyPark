namespace MobyPark.Validation;

public static class ValHelper
{
    public static void ThrowIfNull(object? value, string name) =>
        ArgumentNullException.ThrowIfNull(value, name);

    public static void ThrowIfNullOrWhiteSpace(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} cannot be empty or whitespace.", name);
    }

    public static void ThrowIfNegative(decimal value, string name)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(name, $"{name} cannot be negative.");
    }

    public static void ThrowIfNotPositive(long value, string name)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(name, $"{name} must be greater than 0.");
    }

    public static DateOnly EnsureCreatedAt(DateOnly createdAt) =>
        createdAt == default ? DateOnly.FromDateTime(DateTime.UtcNow) : createdAt;

    public static DateTime EnsureCreatedAt(DateTime createdAt) =>
        createdAt == default ? DateTime.UtcNow : createdAt;

    public static string NormalizePlate(string plate) => plate.Trim().ToUpperInvariant();
}