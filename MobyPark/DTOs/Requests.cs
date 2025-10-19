namespace MobyPark.DTOs;
// Data Transfer Objects (DTOs). Keep these as records. They function as lighter weight classes and are still safe against malicious injection.
// TODO: Give existing requests more parameters where needed / all encompassing for the respective model if used for creation
// Also, put these in their own directory when you get the chance.

public record SessionRequest(string LicensePlate);

public record ParkingLotRequest(string Name, string Location, decimal Tariff, decimal DayTariff);

public record ReservationRequest(string LicensePlate, DateTime StartDate, DateTime EndDate, int ParkingLotId, string? Username = null); // User meant only for admin override
public record PaymentValidationRequest(string Validation);

public record PaymentRequest(string TransactionId, decimal? Amount, TransactionDataModel TransactionData);
public record PaymentRefundRequest(string? TransactionId, decimal? Amount, string? CoupledTo);

public record StartParkingSessionRequest(string LicensePlate, string CardToken, decimal EstimatedAmount, bool SimulateInsufficientFunds = false);
