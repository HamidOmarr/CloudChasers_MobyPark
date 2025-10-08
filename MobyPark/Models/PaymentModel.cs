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
        TransactionId = reader.GetString(reader.GetOrdinal("transaction"));
        Amount = (decimal)reader.GetFloat(reader.GetOrdinal("amount"));
        Initiator = reader.GetString(reader.GetOrdinal("initiator"));
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"));
        Completed = reader.GetFieldValue<DateTime?>(reader.GetOrdinal("completed"));
        Hash = reader.GetString(reader.GetOrdinal("hash"));
        TransactionData.Amount = (decimal)reader.GetFloat(reader.GetOrdinal("t_data_amount"));
        TransactionData.Date = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("t_data_date")));
        TransactionData.Method = reader.GetString(reader.GetOrdinal("t_data_method"));
        TransactionData.Issuer = reader.GetString(reader.GetOrdinal("t_data_issuer"));
        TransactionData.Bank = reader.GetString(reader.GetOrdinal("t_data_bank"));
        CoupledTo = reader.IsDBNull(reader.GetOrdinal("coupled_to")) ? null : reader.GetString(reader.GetOrdinal("coupled_to"));
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
    public DateOnly Date { get; set; }
    public string Method { get; set; }
    public string Issuer { get; set; }
    public string Bank { get; set; }
}
