namespace MobyPark.DTOs.PreAuth.Response;

public class PreAuthDto
{
    public bool Approved { get; set; }
    public string? Reason { get; set; }
}