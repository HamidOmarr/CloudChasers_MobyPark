namespace MobyPark.Models;

public class ApiLoggingModel
{
    public long Id { get; set; }
    public string InputBody { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}