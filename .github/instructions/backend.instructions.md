---
applyTo: "src/Backend/**"
---

# Backend Conventions (ASP.NET Core + NHibernate + Oracle 11g)

## Class Structure
- **All classes are `sealed`** — entities, DTOs, services, repositories, controllers, mappers, NHibernate map classes.
- **Constructor injection** — use primary constructor syntax: `public sealed class MyService(ISession session, MyRepo repo)`.
- **Async throughout** — all service/repository methods are async; controllers `await` them.

## Entities
```csharp
public class MyEntity
{
    public virtual int Id { get; set; }
    public virtual string Name { get; set; } = string.Empty;
    public virtual IList<ChildEntity> Children { get; set; } = [];
}
```
- Properties must be `virtual` for NHibernate proxies.
- Collections use `IList<T>`, initialized to `[]`.

## NHibernate Mappings (`Data/Mappings/`)
```csharp
public sealed class MyEntityMap : ClassMap<MyEntity>
{
    public MyEntityMap()
    {
        Table("USUARIO.MYTABLE");                          // UPPERCASE, USUARIO.* prefix
        Id(x => x.Id).Column("IDMYTABLE")
            .GeneratedBy.Sequence("USUARIO.MYTABLE_SEQ"); // sequence-based IDs
        Map(x => x.FechaInicio).Column("FECHAINICIO")
            .CustomType<DateOnlyUserType>()               // REQUIRED for every DateOnly
            .Not.Nullable();
        HasMany(x => x.Children)
            .Table("USUARIO.CHILDTABLE").KeyColumn("IDMYTABLE")
            .Inverse().Cascade.AllDeleteOrphan();
    }
}
```
- Table names: `USUARIO.*`, all UPPERCASE.
- Columns: UPPERCASE.
- Every `DateOnly` property **must** use `.CustomType<DateOnlyUserType>()` — omitting it causes silent Oracle conversion errors.
- Sequence: `USUARIO.MYTABLE_SEQ`.
- Composite keys: use `CompositeId().KeyProperty(...).KeyProperty(...)`.

## Repositories
- Use **`QueryOver`** exclusively — no LINQ to NHibernate.
- Use `Restrictions` for Oracle-compatible filtering (null checks, OR conditions, date ranges).
- Wrap writes in a manual transaction:
  ```csharp
  using var tx = _session.BeginTransaction();
  await _session.SaveOrUpdateAsync(entity);
  await _session.FlushAsync();
  await tx.CommitAsync();
  ```

## DTOs
- **Sealed**, all properties `init`-only, default empty collections to `[]`.
- **Input DTOs** (`*InputDto`, `CreateUpdateDto`): contain **IDs only** — `IdNomenclador`, `IdEscalaSalarial`.
- **Output DTOs** (`DetailDto`, `ListItemDto`): contain **resolved descriptions** — `NomencladorDescripcion`.
```csharp
public sealed class MyInputDto
{
    public int IdSomething { get; init; }
    public IReadOnlyCollection<ChildInputDto> Children { get; init; } = [];
}
```

## Mappers
- Stateless, sealed, injected as scoped.
- Methods: `ToNewEntity(dto)`, `Apply(dto, entity)`, `ToDetailDto(entity, catalogSnapshot)`, `ToListItemDto(entity, catalogSnapshot)`.
- Always receive a `CatalogSnapshot` — **never** query the DB inside a mapper.

## CatalogSnapshot
- Build via `await _catalogRepository.GetSnapshotAsync()` **before** calling any mapper method.
- `CatalogSnapshot` holds `IReadOnlyDictionary<int, T>` for every catalog type.
