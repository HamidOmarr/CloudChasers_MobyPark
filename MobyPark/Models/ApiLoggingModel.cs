namespace MobyPark.Models;

public class ApiLoggingModel
{
    public long Id { get; set; }
    public string InputBody { get; set; }
    public string Path { get; set; }
    public int StatusCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}