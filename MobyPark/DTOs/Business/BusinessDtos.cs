public class ReadBusinessDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
}

public class CreateBusinessDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string IBAN { get; set; } = string.Empty;
}

public class PatchBusinessDto
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? IBAN { get; set; }
}