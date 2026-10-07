namespace Nomenclador.Api.DTOs;

public sealed class PrimitivaDto
{
    public int Id { get; init; }

    public string? Nombre { get; init; }

    public string? Descripcion { get; init; }

    public bool EsResultLogico { get; init; }

    public string? Cabecera { get; init; }

    public string? Cuerpo { get; init; }

    public string? Pie { get; init; }
}
