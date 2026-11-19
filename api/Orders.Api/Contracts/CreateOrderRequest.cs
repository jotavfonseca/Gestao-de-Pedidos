using System.ComponentModel.DataAnnotations;
using Orders.Core.Domain.Orders;

namespace Orders.Api.Contracts;

/// <summary>Data to create an order. Status and creation date are set by the server.</summary>
public sealed class CreateOrderRequest
{
    /// <example>Jane Doe</example>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Customer is required.")]
    [StringLength(Order.MaxTextLength, ErrorMessage = "Customer must be at most {1} characters.")]
    public string? Customer { get; init; }

    /// <example>Mechanical keyboard</example>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Product is required.")]
    [StringLength(Order.MaxTextLength, ErrorMessage = "Product must be at most {1} characters.")]
    public string? Product { get; init; }

    /// <example>349.90</example>
    [Required(ErrorMessage = "Amount is required.")]
    [Range(typeof(decimal), "0.01", "9999999999999999.99",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "Amount must be greater than zero.")]
    public decimal? Amount { get; init; }
}
