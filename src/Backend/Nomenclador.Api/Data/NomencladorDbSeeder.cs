using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data;

public static class NomencladorDbSeeder
{
    public static async Task SeedAsync(NomencladorDbContext dbContext)
    {
        if (dbContext.Nomencladores.Any())
        {
            return;
        }

        dbContext.Nomencladores.AddRange(
            new NomencladorCatalogEntity { Id = 1, Descripcion = "Administración Central" },
            new NomencladorCatalogEntity { Id = 2, Descripcion = "Docentes Planta Permanente" },
            new NomencladorCatalogEntity { Id = 3, Descripcion = "Salud Hospitalaria" });

        dbContext.EscalasSalariales.AddRange(
            new EscalaSalarialCatalogEntity { Id = 1, Descripcion = "Escala 2026" },
            new EscalaSalarialCatalogEntity { Id = 2, Descripcion = "Escala 2027" });

        dbContext.Zonas.AddRange(
            new ZonaCatalogEntity { Id = 1, Descripcion = "Zona Centro" },
            new ZonaCatalogEntity { Id = 2, Descripcion = "Zona Norte" },
            new ZonaCatalogEntity { Id = 3, Descripcion = "Zona Sur" });

        dbContext.Categorias.AddRange(
            new CategoriaCatalogEntity { Id = 1, EscalaSalarialId = 1, Numero = 1, Descripcion = "Categoría A" },
            new CategoriaCatalogEntity { Id = 2, EscalaSalarialId = 1, Numero = 2, Descripcion = "Categoría B" },
            new CategoriaCatalogEntity { Id = 3, EscalaSalarialId = 1, Numero = 3, Descripcion = "Categoría C" },
            new CategoriaCatalogEntity { Id = 4, EscalaSalarialId = 2, Numero = 1, Descripcion = "Categoría A 2027" },
            new CategoriaCatalogEntity { Id = 5, EscalaSalarialId = 2, Numero = 2, Descripcion = "Categoría B 2027" });

        dbContext.Conceptos.AddRange(
            new ConceptoCatalogEntity { Id = 1, Codigo = "1100", Subcodigo = 0, DescripcionBreve = "Básico", Descripcion = "Sueldo básico", Clasificacion = "HABER" },
            new ConceptoCatalogEntity { Id = 2, Codigo = "1200", Subcodigo = 0, DescripcionBreve = "Antigüedad", Descripcion = "Adicional por antigüedad", Clasificacion = "HABER" },
            new ConceptoCatalogEntity { Id = 3, Codigo = "2100", Subcodigo = 1, DescripcionBreve = "Presentismo", Descripcion = "Premio por presentismo", Clasificacion = "HABER" },
            new ConceptoCatalogEntity { Id = 4, Codigo = "9100", Subcodigo = 0, DescripcionBreve = "Aporte social", Descripcion = "Descuento de obra social", Clasificacion = "DESCUENTO" });

        dbContext.ValoresFijos.AddRange(
            new ValorFijoCatalogEntity { Id = 1, Descripcion = "Presentismo fijo", Tipo = "IMPORTE_FIJO" },
            new ValorFijoCatalogEntity { Id = 2, Descripcion = "Adicional por título", Tipo = "PORCENTAJE" },
            new ValorFijoCatalogEntity { Id = 3, Descripcion = "Zona desfavorable", Tipo = "IMPORTE_FIJO" });

        dbContext.Configuraciones.AddRange(
            new ConfiguracionNomencladorEntity
            {
                Id = 1,
                NomencladorId = 2,
                EscalaSalarialId = 1,
                ZonaId = 1,
                FechaInicio = new DateOnly(2026, 1, 1),
                FechaFin = null,
                Conceptos =
                [
                    new ConceptoConfiguradoEntity { Id = 1, ConceptoId = 1, Orden = 1, Activo = true },
                    new ConceptoConfiguradoEntity { Id = 2, ConceptoId = 2, Orden = 2, Activo = true },
                    new ConceptoConfiguradoEntity { Id = 3, ConceptoId = 4, Orden = 3, Activo = true }
                ],
                ValoresFijos =
                [
                    new ValorFijoConfiguradoEntity { Id = 1, ValorFijoId = 1, Importe = 25000m }
                ],
                ValoresCategorias =
                [
                    new ValorCategoriaConfiguradoEntity { Id = 1, CategoriaId = 1, Importe = 150000m },
                    new ValorCategoriaConfiguradoEntity { Id = 2, CategoriaId = 2, Importe = 165000m },
                    new ValorCategoriaConfiguradoEntity { Id = 3, CategoriaId = 3, Importe = 180000m }
                ]
            },
            new ConfiguracionNomencladorEntity
            {
                Id = 2,
                NomencladorId = 1,
                EscalaSalarialId = 2,
                ZonaId = 2,
                FechaInicio = new DateOnly(2027, 1, 1),
                FechaFin = null,
                Conceptos =
                [
                    new ConceptoConfiguradoEntity { Id = 4, ConceptoId = 1, Orden = 1, Activo = true },
                    new ConceptoConfiguradoEntity { Id = 5, ConceptoId = 3, Orden = 2, Activo = true }
                ],
                ValoresFijos =
                [
                    new ValorFijoConfiguradoEntity { Id = 2, ValorFijoId = 3, Importe = 10000m }
                ],
                ValoresCategorias =
                [
                    new ValorCategoriaConfiguradoEntity { Id = 4, CategoriaId = 4, Importe = 175000m },
                    new ValorCategoriaConfiguradoEntity { Id = 5, CategoriaId = 5, Importe = 190000m }
                ]
            });

        await dbContext.SaveChangesAsync();
    }
}
