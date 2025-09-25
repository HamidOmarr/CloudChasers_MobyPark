using Npgsql;

namespace MobyPark.Models;

public class ParkingLotModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Location { get; set; }
    public string Address { get; set; }
    public int Capacity { get; set; }
    public int Reserved { get; set; } = 0;
    public decimal Tariff { get; set; }
    public decimal DayTariff { get; set; }
    public DateTime CreatedAt { get; set; }
    public CoordinatesModel Coordinates { get; set; }

    public ParkingLotModel()
    {
        Coordinates = new CoordinatesModel();
    }

    public ParkingLotModel(NpgsqlDataReader reader) : this()
    {
        Id = reader.GetInt32(reader.GetOrdinal("id"));
        Name = reader.GetString(reader.GetOrdinal("Name"));
        Location = reader.GetString(reader.GetOrdinal("Location"));
        Address = reader.GetString(reader.GetOrdinal("Address"));
        Capacity = reader.GetInt32(reader.GetOrdinal("Capacity"));
        Reserved = reader.GetInt32(reader.GetOrdinal("Reserved"));
        Tariff = reader.GetDecimal(reader.GetOrdinal("Tariff"));
        DayTariff = reader.GetDecimal(reader.GetOrdinal("Daytariff"));
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
        Coordinates.Lat = reader.GetDouble(reader.GetOrdinal("Lat"));
        Coordinates.Lng = reader.GetDouble(reader.GetOrdinal("Lng"));
    }

    public override string ToString() =>
        $"[{Id}] {Name} is in {Location} (Address: {Address}).\nIt has a capacity of {Capacity}, of which {Reserved} are reserved.\nThe tariff is {Tariff}, with a day tariff of {DayTariff}.\nThe parking lot was made on {CreatedAt} and is located at {Coordinates.Lat}, {Coordinates.Lng}";
}


public class CoordinatesModel
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}