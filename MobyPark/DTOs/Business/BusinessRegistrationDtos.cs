namespace MobyPark.DTOs.Business;

public class ReadBusinessRegDto //reg = registration
{
    public long Id { get; set; }
    public long BusinessId { get; set; }
    public string LicensePlateNumber { get; set; }
    public bool Active { get; set; }
    public DateTimeOffset LastSinceActive { get; set; }
}

public class CreateBusinessRegAdminDto //reg = registration
{
    public long BusinessId { get; set; }
    public string LicensePlateNumber { get; set; }
    public bool Active { get; set; }
}

public class CreateBusinessRegDto //reg = registration
{
    public string LicensePlateNumber { get; set; }
    public bool Active { get; set; }
}

public class PatchBusinessRegDto //reg = registration
{
    public long Id { get; set; }
    public bool Active { get; set; }
}
