namespace Nomenclador.Api.DTOs;

public sealed class ClonarEscalaDto
{
    public DateOnly NuevoPeriodo { get; init; }
    public decimal CoeficienteAjuste { get; init; }

    // Si ya existe una escala con el nombre que tendría el clon (mismo período), true
    // fuerza a actualizar los montos de esa escala existente en lugar de abortar con conflicto.
    public bool ActualizarSiExiste { get; init; }
}
