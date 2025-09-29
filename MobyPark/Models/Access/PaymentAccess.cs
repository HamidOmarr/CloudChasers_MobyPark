using Npgsql;
using MobyPark.Models.Access.DatabaseConnection;

namespace MobyPark.Models.Access;

public class PaymentAccess : Repository<PaymentModel>, IPaymentAccess
{
    protected override string TableName => "payments";
    protected override PaymentModel MapFromReader(NpgsqlDataReader reader) => new(reader);

    protected override Dictionary<string, object> GetParameters(PaymentModel payment)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@transaction", payment.TransactionId },
            { "@amount", payment.Amount },
            { "@initiator", payment.Initiator },
            { "@created_at", payment.CreatedAt },
            { "@completed", payment.Completed ?? (object)null },
            { "@hash", payment.Hash },
            { "@t_data_amount", payment.TransactionData.Amount },
            { "@t_data_date", payment.TransactionData.Date },
            { "@t_data_method", payment.TransactionData.Method },
            { "@t_data_issuer", payment.TransactionData.Issuer },
            { "@t_data_bank", payment.TransactionData.Bank }
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
        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE transaction_id = @transaction", parameters);
        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }

}