using System.ComponentModel.DataAnnotations;
using Ordering.Domain;

namespace Ordering.Api.Contracts;

/// <summary>
/// Cuerpo de POST /api/orders. Valida la forma de la entrada en el borde HTTP (ADR-0001);
/// el dominio sigue siendo la garantía final.
/// </summary>
public sealed class CreateOrderRequest : IValidatableObject
{
    // Guid? y no Guid: si falta en el JSON, un Guid valdría Guid.Empty y [Required] no lo detectaría (ADR-0003).
    [Required]
    public Guid? CustomerId { get; init; }

    // Reutiliza Order.MaxLines para no duplicar el límite del dominio (ADR-0004).
    [Required]
    [MinLength(1, ErrorMessage = "The order must have at least one line.")]
    [MaxLength(Order.MaxLines, ErrorMessage = "The order cannot have more than {1} lines.")]
    public List<CreateOrderLineRequest>? Lines { get; init; }

    // MVC no valida elementos null dentro de la lista ("lines": [null]); sin esto el mapeo fallaría con un 500.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Lines is not null && Lines.Any(l => l is null))
        {
            yield return new ValidationResult("Lines cannot contain null items.", new[] { nameof(Lines) });
        }
    }
}

public sealed class CreateOrderLineRequest
{
    // [Required] en string ya rechaza vacío y solo espacios (AllowEmptyStrings = false por defecto).
    // MaxLength explícito: SQLite no aplica el HasMaxLength(64) de EF Core (ADR-0004).
    [Required]
    [MaxLength(64)]
    public string? Sku { get; init; }

    // int basta: si falta vale 0 y [Range] lo rechaza.
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
    public int Quantity { get; init; }

    // decimal? y no decimal: si falta en el JSON valdría 0, que es un precio válido, y se aceptaría en silencio (ADR-0003).
    [Required]
    [Range(0d, double.MaxValue, ErrorMessage = "UnitPrice cannot be negative.")]
    public decimal? UnitPrice { get; init; }
}
