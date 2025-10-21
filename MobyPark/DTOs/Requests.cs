namespace MobyPark.DTOs;
// Data Transfer Objects (DTOs). Keep these as records. They function as lighter weight classes and are still safe against malicious injection.
// TODO: Give existing requests more parameters where needed / all encompassing for the respective model if used for creation
// Also, put these in their own directory when you get the chance.

public record PaymentRefundRequest(string PaymentId, decimal? Amount);
