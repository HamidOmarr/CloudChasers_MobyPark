namespace MobyPark.Validation;

public static class ValHelper
{
    public static string NormalizePlate(string plate) => plate.Trim().ToUpperInvariant();
}
