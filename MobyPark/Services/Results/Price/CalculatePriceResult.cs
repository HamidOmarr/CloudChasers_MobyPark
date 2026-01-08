namespace MobyPark.Services.Results.Price;

public abstract record CalculatePriceResult
{
    public sealed record Success(decimal Price, int BillableHours, int BillableDays) : CalculatePriceResult;
    public sealed record Error(string Message) : CalculatePriceResult;
}