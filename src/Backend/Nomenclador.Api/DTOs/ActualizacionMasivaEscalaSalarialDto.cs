namespace Nomenclador.Api.DTOs;

public sealed class ActualizacionMasivaEscalaSalarialDto
{
    public IReadOnlyCollection<int> ConfiguracionesIds { get; init; } = [];
    public DateOnly NuevoPeriodo { get; init; }
    public decimal CoeficienteAjuste { get; init; }

    // Si alguna escala referenciada ya tiene un clon para el período pedido, true fuerza a
    // actualizar los montos de esa escala existente en lugar de devolver un conflicto.
    public bool ActualizarSiExiste { get; init; }
}
