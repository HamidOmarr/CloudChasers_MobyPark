using System.Globalization;
using Microsoft.Data.Sqlite;

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

    public PaymentModel(SqliteDataReader reader) : this()
    {
        TransactionId = reader.GetString(reader.GetOrdinal("Transaction"));
        Amount = reader.GetDecimal(reader.GetOrdinal("Amount"));
        Initiator = reader.GetString(reader.GetOrdinal("Initiator"));
        CreatedAt = DateTime.ParseExact(
            reader.GetString(reader.GetOrdinal("CreatedAt")),
            "dd-MM-yyyy HH:mm:ssfffffff",
            CultureInfo.InvariantCulture
        );

        int completedIndex = reader.GetOrdinal("Completed");
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

        Hash = reader.GetString(reader.GetOrdinal("Hash"));
        TransactionData.Amount = reader.GetDecimal(reader.GetOrdinal("TransactionAmount"));
        TransactionData.Date = DateTime.ParseExact(
            reader.GetString(reader.GetOrdinal("TransactionDate")),
            "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture
        );
        TransactionData.Method = reader.GetString(reader.GetOrdinal("TransactionMethod"));
        TransactionData.Issuer = reader.GetString(reader.GetOrdinal("TransactionIssuer"));
        TransactionData.Bank = reader.GetString(reader.GetOrdinal("TransactionBank"));
        CoupledTo = reader.IsDBNull(reader.GetOrdinal("CoupledTo"))
            ? null
            : reader.GetString(reader.GetOrdinal("CoupledTo"));
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
