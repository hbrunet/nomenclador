using Microsoft.EntityFrameworkCore;
using Nomenclador.Api.Models;

namespace Nomenclador.Api.Data;

public sealed class NomencladorDbContext(DbContextOptions<NomencladorDbContext> options) : DbContext(options)
{
    public DbSet<ConfiguracionNomencladorEntity> Configuraciones => Set<ConfiguracionNomencladorEntity>();

    public DbSet<ConceptoConfiguradoEntity> ConceptosConfigurados => Set<ConceptoConfiguradoEntity>();

    public DbSet<ValorFijoConfiguradoEntity> ValoresFijosConfigurados => Set<ValorFijoConfiguradoEntity>();

    public DbSet<ValorCategoriaConfiguradoEntity> ValoresCategoriasConfigurados => Set<ValorCategoriaConfiguradoEntity>();

    public DbSet<NomencladorCatalogEntity> Nomencladores => Set<NomencladorCatalogEntity>();

    public DbSet<EscalaSalarialCatalogEntity> EscalasSalariales => Set<EscalaSalarialCatalogEntity>();

    public DbSet<ZonaCatalogEntity> Zonas => Set<ZonaCatalogEntity>();

    public DbSet<CategoriaCatalogEntity> Categorias => Set<CategoriaCatalogEntity>();

    public DbSet<ConceptoCatalogEntity> Conceptos => Set<ConceptoCatalogEntity>();

    public DbSet<ValorFijoCatalogEntity> ValoresFijos => Set<ValorFijoCatalogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConfiguracionNomencladorEntity>()
            .HasMany(configuration => configuration.Conceptos)
            .WithOne()
            .HasForeignKey(item => item.ConfiguracionNomencladorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ConfiguracionNomencladorEntity>()
            .HasMany(configuration => configuration.ValoresFijos)
            .WithOne()
            .HasForeignKey(item => item.ConfiguracionNomencladorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ConfiguracionNomencladorEntity>()
            .HasMany(configuration => configuration.ValoresCategorias)
            .WithOne()
            .HasForeignKey(item => item.ConfiguracionNomencladorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
