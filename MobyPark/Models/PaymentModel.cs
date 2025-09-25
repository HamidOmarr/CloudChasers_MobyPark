using Npgsql;

namespace MobyPark.Models;

public class PaymentModel
{
    public string TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Initiator { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? Completed { get; set; }
    public string Hash { get; set; }
    public TransactionDataModel TransactionData { get; set; }
    public string? CoupledTo { get; set; }

    public PaymentModel()
    {
        TransactionData = new TransactionDataModel();
    }

    public PaymentModel(NpgsqlDataReader reader) : this()
    {
        TransactionId = reader.GetString(reader.GetOrdinal("Transaction"));
        Amount = reader.GetDecimal(reader.GetOrdinal("Amount"));
        Initiator = reader.GetString(reader.GetOrdinal("Initiator"));
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
        Completed = reader.GetFieldValue<DateTime?>(reader.GetOrdinal("Completed"));
        Hash = reader.GetString(reader.GetOrdinal("Hash"));
        TransactionData.Amount = reader.GetDecimal(reader.GetOrdinal("TransactionAmount"));
        TransactionData.Date = reader.GetDateTime(reader.GetOrdinal("TransactionDate"));
        TransactionData.Method = reader.GetString(reader.GetOrdinal("TransactionMethod"));
        TransactionData.Issuer = reader.GetString(reader.GetOrdinal("TransactionIssuer"));
        TransactionData.Bank = reader.GetString(reader.GetOrdinal("TransactionBank"));
        CoupledTo = reader.GetFieldValue<string?>(reader.GetOrdinal("CoupledTo"));
    }

    public override string ToString() =>
        $"TransactionId: {TransactionId}\n" +
        $"Initiator: {Initiator}\n" +
        $"Amount: {Amount}\n" +
        $"Hash: {Hash}\n" +
        $"Created At: {CreatedAt}\n" +
        $"Completed: {Completed}\n" +
        $"Transaction Data -> Amount: {TransactionData.Amount}, Date: {TransactionData.Date}, Method: {TransactionData.Method}, Issuer: {TransactionData.Issuer}, Bank: {TransactionData.Bank}" +
        (CoupledTo != null ? $"\nCoupled to transaction ID: {CoupledTo}" : "");
}

public class TransactionDataModel
{
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Method { get; set; }
    public string Issuer { get; set; }
    public string Bank { get; set; }
}
