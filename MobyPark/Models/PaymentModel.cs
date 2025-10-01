using System.Globalization;
using Microsoft.Data.Sqlite;

namespace MobyPark.Models;

public class PaymentModel
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Initiator { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? Completed { get; set; }
    public string Hash { get; set; } = string.Empty;
    public TransactionDataModel TransactionData { get; set; }
    public string? CoupledTo { get; set; }

    public PaymentModel()
    {
        TransactionData = new TransactionDataModel();
    }

    public PaymentModel(SqliteDataReader reader) : this()
    {
        TransactionId = reader.GetString(reader.GetOrdinal("transaction"));
        Amount = reader.GetDecimal(reader.GetOrdinal("amount"));
        Initiator = reader.GetString(reader.GetOrdinal("initiator"));
        CreatedAt = DateTime.ParseExact(
            reader.GetString(reader.GetOrdinal("created_at")),
            "dd-MM-yyyy HH:mm:ssfffffff",
            CultureInfo.InvariantCulture
        );

        int completedIndex = reader.GetOrdinal("completed");
        if (!reader.IsDBNull(completedIndex))
        {
            Completed = DateTime.ParseExact(
                reader.GetString(completedIndex),
                "dd-MM-yyyy HH:mm:ssfffffff",
                CultureInfo.InvariantCulture
            );
        }
        else
            Completed = null;

        Hash = reader.GetString(reader.GetOrdinal("hash"));
        TransactionData.Amount = reader.GetDecimal(reader.GetOrdinal("t_data_amount"));
        TransactionData.Date = DateTime.ParseExact(
            reader.GetString(reader.GetOrdinal("t_data_date")),
            "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture
        );
        TransactionData.Method = reader.GetString(reader.GetOrdinal("t_data_method"));
        TransactionData.Issuer = reader.GetString(reader.GetOrdinal("t_data_issuer"));
        TransactionData.Bank = reader.GetString(reader.GetOrdinal("t_data_bank"));
        CoupledTo = reader.IsDBNull(reader.GetOrdinal("coupled_to"))
            ? null
            : reader.GetString(reader.GetOrdinal("coupled_to"));
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
    public string Method { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Bank { get; set; } = string.Empty;
}
