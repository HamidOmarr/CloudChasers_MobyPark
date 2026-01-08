public class ReadBusinessDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string IBAN { get; set; }
}

public class CreateBusinessDto
{
    public string Name { get; set; }
    public string Address { get; set; }
    public string IBAN { get; set; }
}

public class PatchBusinessDto
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? IBAN { get; set; }
}