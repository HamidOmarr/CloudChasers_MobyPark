using Npgsql;
using MobyPark.Models.Access.DatabaseConnection;

namespace MobyPark.Models.Access;

public class PaymentAccess : Repository<PaymentModel>, IPaymentAccess
{
    protected override string TableName => "Payments";
    protected override PaymentModel MapFromReader(NpgsqlDataReader reader) => new(reader);

    protected override Dictionary<string, object> GetParameters(PaymentModel payment)
    {
        var parameters = new Dictionary<string, object>
        {
            { "@TransactionId", payment.TransactionId },
            { "@Amount", payment.Amount },
            { "@Initiator", payment.Initiator },
            { "@CreatedAt", payment.CreatedAt },
            { "@Completed", payment.Completed ?? (object)null },
            { "@Hash", payment.Hash },
            { "@TransactionAmount", payment.TransactionData.Amount },
            { "@TransactionDate", payment.TransactionData.Date },
            { "@TransactionMethod", payment.TransactionData.Method },
            { "@TransactionIssuer", payment.TransactionData.Issuer },
            { "@TransactionBank", payment.TransactionData.Bank }
        };

        return parameters;
    }

    public PaymentAccess(IDatabaseConnection connection) : base(connection) { }

    public async Task<List<PaymentModel>> GetByUser(string user)
    {
        Dictionary<string, object> parameters = new() { { "@Initiator", user } };
        List<PaymentModel> payments = [];
        await using var reader =
            await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE Initiator = @Initiator", parameters);

        while (await reader.ReadAsync())
            payments.Add(MapFromReader(reader));

        return payments;
    }

    public async Task<PaymentModel?> GetByTransactionId(string transactionId)
    {
        Dictionary<string, object> parameters = new() { { "@TransactionId", transactionId } };

        await using var reader = await Connection.ExecuteQuery($"SELECT * FROM {TableName} WHERE TransactionId = @TransactionId", parameters);

        return await reader.ReadAsync() ? MapFromReader(reader) : null;
    }

}