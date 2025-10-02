namespace MobyPark.Models.Requests;
// Data Transfer Objects (DTOs). Keep these as records. They function as lighter weight classes and are still safe against malicious injection.
// TODO: Give existing requests more parameters where needed / all encompassing for the respective model if used for creation

public record UserRegisterRequest(string Username, string Password, string Name);
public record UserLoginRequest(string Username, string Password);
public record SessionRequest(string LicensePlate);

public record ParkingLotRequest(string Name, string Location, decimal Tariff, decimal DayTariff);

public record VehicleRequest(string LicensePlate, string Make, string Model, string Color, int Year);
public record VehicleUpdateRequest(string LicensePlate, string Make, string Model, string Color, int Year);
public record VehicleEntryRequest(string ParkingLot);

public record ReservationRequest(string LicensePlate, DateTime StartDate, DateTime EndDate, int ParkingLotId, string? Username = null); // User meant only for admin override
public record PaymentValidationRequest(TransactionDataModel TransactionData, string Validation);

public record PaymentRequest(string TransactionId, decimal? Amount, TransactionDataModel TransactionData);
public record PaymentRefundRequest(string? TransactionId, decimal? Amount, string? CoupledTo);
