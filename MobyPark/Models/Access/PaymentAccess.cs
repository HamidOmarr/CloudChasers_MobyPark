using Microsoft.Data.Sqlite;
using MobyPark.Services.DatabaseConnection;

namespace MobyPark.Models.Access;

public class PaymentAccess : Repository<PaymentModel>, IPaymentAccess
{
    protected override string TableName => "payments";
    protected override PaymentModel MapFromReader(SqliteDataReader reader) => new(reader);

    protected override Dictionary<string, object> GetParameters(PaymentModel payment)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@transaction", payment.TransactionId },
            { "@amount", payment.Amount },
            { "@initiator", payment.Initiator },
            { "@created_at", payment.CreatedAt.ToString("dd-MM-yyyy HH:mm:ssfffffff") },
            { "@completed", payment.Completed?.ToString("dd-MM-yyyy HH:mm:ssfffffff") ?? (object)DBNull.Value },
            { "@hash", payment.Hash },
            { "@t_data_amount", payment.TransactionData.Amount },
            { "@t_data_date", payment.TransactionData.Date.ToString("yyyy-MM-dd HH:mm:ss") },
            { "@t_data_method", payment.TransactionData.Method },
            { "@t_data_issuer", payment.TransactionData.Issuer },
            { "@t_data_bank", payment.TransactionData.Bank },
            { "@coupled_to", payment.CoupledTo ?? (object)DBNull.Value }
        };

        return parameters;
    }

    public PaymentAccess(IDatabaseConnection connection) : base(connection) { }

    public async Task<List<PaymentModel>> GetByUser(string user)
    {
        Dictionary<string, object> parameters = new() { { "@initiator", user } };
        List<PaymentModel> payments = [];
        await using var reader =
            await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE initiator = @initiator", parameters);

        while (await reader.ReadAsync())
            payments.Add(MapFromReader(reader));

        return payments;
    }

    public async Task<PaymentModel?> GetByTransactionId(string transactionId)
    {
    Dictionary<string, object> parameters = new() { { "@transaction", transactionId } };

    await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE transaction = @transaction", parameters);

        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }

}