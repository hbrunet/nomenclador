namespace Nomenclador.Api.DTOs;

public sealed class ValorCategoriaConfiguradoDto
{
    public int IdValorCategoria { get; init; }

    public string Descripcion { get; init; } = string.Empty;

    public string Tipo { get; init; } = string.Empty;

    public List<ValorCategoriaConfiguradoItemDto> Items { get; init; } = new();
}
