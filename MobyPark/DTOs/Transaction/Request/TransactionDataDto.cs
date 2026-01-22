using System.ComponentModel.DataAnnotations;

using MobyPark.Models.Repositories.Interfaces;

using Swashbuckle.AspNetCore.Annotations;

namespace MobyPark.DTOs.Transaction.Request;

[SwaggerSchema(Description = "Details regarding the transaction method provided by the payment gateway.")]
public class TransactionDataDto : ICanBeEdited
{
    [Required]
    [SwaggerSchema("The payment method used (e.g., CreditCard, PayPal).")]
    public string Method { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("The issuer of the payment method (e.g., Visa, Mastercard).")]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    [SwaggerSchema("The bank or provider handling the transaction.")]
    public string Bank { get; set; } = string.Empty;
}