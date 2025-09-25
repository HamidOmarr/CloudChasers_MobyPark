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
        Name = reader.GetString(reader.GetOrdinal("name"));
        Location = reader.GetString(reader.GetOrdinal("location"));
        Address = reader.GetString(reader.GetOrdinal("address"));
        Capacity = reader.GetInt32(reader.GetOrdinal("capacity"));
        Reserved = reader.GetInt32(reader.GetOrdinal("reserved"));
        Tariff = (decimal)reader.GetFloat(reader.GetOrdinal("tariff"));
        DayTariff = (decimal)reader.GetFloat(reader.GetOrdinal("day_tariff"));
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"));
        Coordinates.Lat = reader.GetDouble(reader.GetOrdinal("lat"));
        Coordinates.Lng = reader.GetDouble(reader.GetOrdinal("lng"));
    }

    public override string ToString() =>
        $"[{Id}] {Name} is in {Location} (Address: {Address}).\nIt has a capacity of {Capacity}, of which {Reserved} are reserved.\nThe tariff is {Tariff}, with a day tariff of {DayTariff}.\nThe parking lot was made on {CreatedAt} and is located at {Coordinates.Lat}, {Coordinates.Lng}";
}


public class CoordinatesModel
{
    public double Lat { get; set; }
    public double Lng { get; set; }
}