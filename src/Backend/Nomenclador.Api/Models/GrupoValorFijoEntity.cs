namespace Nomenclador.Api.Models;

// Agrupa tipos de valor fijo (DESC_VALFIJO) para acelerar la clonación masiva
// mensual: en vez de re-seleccionar tipos cada mes, se elige un grupo guardado.
public class GrupoValorFijoEntity : CatalogEntityBase
{
    public virtual IList<ValorFijoTipoCatalogEntity> Tipos { get; set; } = [];
}
