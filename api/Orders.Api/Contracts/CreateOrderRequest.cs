using System.ComponentModel.DataAnnotations;
using Orders.Core.Domain.Orders;

namespace Orders.Api.Contracts;

/// <summary>Dados para criar um pedido. Status e data são definidos pelo servidor.</summary>
public sealed class CreateOrderRequest
{
    /// <example>Ana Souza</example>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Informe o cliente.")]
    [StringLength(Order.MaxTextLength, ErrorMessage = "O cliente deve ter no máximo {1} caracteres.")]
    public string? Cliente { get; init; }

    /// <example>Teclado mecânico</example>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Informe o produto.")]
    [StringLength(Order.MaxTextLength, ErrorMessage = "O produto deve ter no máximo {1} caracteres.")]
    public string? Produto { get; init; }

    /// <example>349.90</example>
    [Required(ErrorMessage = "Informe o valor.")]
    [Range(typeof(decimal), "0.01", "9999999999999999.99",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true,
        ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal? Valor { get; init; }
}
