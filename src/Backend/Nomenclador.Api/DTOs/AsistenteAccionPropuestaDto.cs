namespace Nomenclador.Api.DTOs;

// Acción de escritura sugerida por el asistente. El backend NO la ejecuta: el frontend la muestra
// al usuario y, si la confirma, invoca el endpoint REST correspondiente.
public sealed class AsistenteAccionPropuestaDto
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    // "clonar-configuracion" | "agregar-concepto" | "quitar-concepto" | "agregar-valor-fijo" | "quitar-valor-fijo"
    public string Tipo { get; init; } = string.Empty;

    public string Descripcion { get; init; } = string.Empty;

    public int ConfiguracionId { get; init; }

    public int? ConceptoId { get; init; }

    public int? ValorFijoId { get; init; }

    public ClonarConfiguracionDto? Clonar { get; init; }
}
