namespace Nomenclador.Api.DTOs;

public sealed class ClonacionMasivaValoresCategoriaDto
{
    public IReadOnlyCollection<int> ValoresCategoriaIds { get; init; } = [];
    public DateOnly NuevoPeriodo { get; init; }
    public decimal CoeficienteAjuste { get; init; }
    public bool ActualizarValoresExistentes { get; init; }

    // Si algún valor a clonar ya tiene un clon con el mismo nombre para el período pedido
    // (mismo Tipo), true fuerza a actualizar ese valor existente en lugar de devolver un conflicto.
    public bool ActualizarSiExiste { get; init; }
}
