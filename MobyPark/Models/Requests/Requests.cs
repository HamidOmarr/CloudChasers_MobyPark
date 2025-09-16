namespace MobyPark.Models.Requests;
// Data Transfer Objects (DTOs). Keep these as records. They function as lighter weight classes and are still safe against malicious injection.

public record UserRegisterRequest(string Username, string Password, string Name);
public record UserLoginRequest(string Username, string Password);
public record SessionRequest(string LicensePlate);

public record ParkingLotRequest(string Name, string Location, decimal Tariff, decimal DayTariff);

public record VehicleRequest(string LicensePlate);
public record VehicleEntryRequest(string ParkingLot);

public record ReservationRequest(string LicensePlate, DateTime StartDate, DateTime EndDate, int ParkingLotId, string? User = null); // User meant only for admin override
public record PaymentValidationRequest(TransactionDataModel TransactionData, string Validation);

public record PaymentRequest(string TransactionId, decimal? Amount);
public record PaymentRefundRequest(string? TransactionId, decimal? Amount, string? CoupledTo);
