namespace Nomenclador.Api.Models;

// Agrupa tipos de valor por categoría (DESC_VALCAT) para acelerar la asociación
// masiva mensual: en vez de re-seleccionar tipos cada mes, se elige un grupo guardado.
public class GrupoValorCategoriaEntity : CatalogEntityBase
{
    public virtual IList<ValorCategoriaTipoCatalogEntity> Tipos { get; set; } = [];
}
