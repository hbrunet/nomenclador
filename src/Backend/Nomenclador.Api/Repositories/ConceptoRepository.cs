using NHibernate;
using NHibernate.Criterion;
using NHibernate.SqlCommand;
using NHibernate.Type;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Models;
using System.Text.RegularExpressions;

namespace Nomenclador.Api.Repositories;

public sealed class ConceptoRepository(NHibernate.ISession session)
{
    // Formato "código/subcódigo", ej. "25/100".
    private static readonly Regex CodigoSubcodigoRegex =
        new(@"^\s*(?<codigo>\d+)\s*/\s*(?<subcodigo>\d+)\s*$", RegexOptions.Compiled);

    // Prefijos explícitos para desambiguar por qué campo se quiere buscar.
    private static readonly string[] DescripcionPrefixes = ["desc:", "d:"];

    public async Task<IReadOnlyCollection<ConceptoCatalogDto>> GetAllAsync(string? query, int? idTipoConcepto = null)
    {
        ConceptoCatalogEntity alias = null!;
        var criteria = session.QueryOver(() => alias);

        ApplyQueryFilter(criteria, query);
        if (idTipoConcepto.HasValue)
            criteria.Where(() => alias.TipoConcepto.Id == idTipoConcepto.Value);

        var items = await criteria
            .Fetch(SelectMode.Fetch, () => alias.TipoConcepto)
            .OrderBy(() => alias.Codigo).Asc
            .ThenBy(() => alias.Subcodigo).Asc
            .Take(100)
            .ListAsync();

        return items
            .Select(item => ToDto(item))
            .ToList();
    }

    public async Task<(IReadOnlyCollection<ConceptoCatalogDto> Items, int Total)> GetPagedAsync(
        string? query, int page, int pageSize, int? idTipoConcepto = null)
    {
        ConceptoCatalogEntity alias = null!;
        var criteria = session.QueryOver(() => alias);

        ApplyQueryFilter(criteria, query);
        if (idTipoConcepto.HasValue)
            criteria.Where(() => alias.TipoConcepto.Id == idTipoConcepto.Value);

        var total = await criteria.RowCountAsync();

        var items = await criteria
            .Fetch(SelectMode.Fetch, () => alias.TipoConcepto)
            .OrderBy(() => alias.Codigo).Asc
            .ThenBy(() => alias.Subcodigo).Asc
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ListAsync();

        var dtos = items
            .Select(item => ToDto(item))
            .ToList();

        return (dtos, total);
    }

    public async Task<ConceptoCatalogDto?> GetByIdAsync(int id)
    {
        ConceptoCatalogEntity alias = null!;
        var entity = await session.QueryOver(() => alias)
            .Fetch(SelectMode.Fetch, () => alias.TipoConcepto)
            .Fetch(SelectMode.Fetch, () => alias.TiposLiquidacion)
            .Where(() => alias.Id == id)
            .SingleOrDefaultAsync();

        if (entity is null) return null;

        var formulas = await GetFormulasAsync(id);
        return ToDto(entity, formulas);
    }

    public async Task<IReadOnlyCollection<CatalogItemDto>> GetTiposAsync()
    {
        TipoConceptoCatalogEntity alias = null!;
        var items = await session.QueryOver(() => alias)
            .OrderBy(() => alias.Descripcion).Asc
            .ListAsync();

        return items.Select(item => new CatalogItemDto
        {
            Id = item.Id,
            Descripcion = item.Descripcion,
        }).ToList();
    }

    public async Task<IReadOnlyCollection<CatalogItemDto>> GetTiposLiquidacionAsync()
    {
        TipoLiquidacionCatalogEntity alias = null!;
        var items = await session.QueryOver(() => alias)
            .OrderBy(() => alias.Descripcion).Asc
            .ListAsync();

        return items.Select(item => new CatalogItemDto
        {
            Id = item.Id,
            Descripcion = item.Descripcion,
        }).ToList();
    }

    // USUARIO.FORMULA: fórmulas asociadas al concepto, mostradas en su detalle.
    public async Task<IReadOnlyList<FormulaDto>> GetFormulasAsync(int conceptoId)
    {
        FormulaEntity alias = null!;
        var items = await session.QueryOver(() => alias)
            .Where(() => alias.ConceptoId == conceptoId)
            .OrderBy(() => alias.Id).Asc
            .ListAsync();

        return items.Select(item => new FormulaDto
        {
            Id = item.Id,
            Condicion = item.Condicion,
            Accion = item.Accion,
            OrdenEjec = item.OrdenEjec,
            CondicionInput = item.CondicionInput,
            AccionInput = item.AccionInput,
            Codigo = item.Codigo,
            SpName = item.SpName,
        }).ToList();
    }

    public async Task<ConceptoCatalogDto> CreateAsync(ConceptoCreateUpdateDto dto)
    {
        var entity = new ConceptoCatalogEntity();
        ApplyDto(entity, dto);

        using var tx = session.BeginTransaction();
        await session.SaveAsync(entity);
        await tx.CommitAsync();

        if (entity.TipoConcepto is not null)
            await NHibernateUtil.InitializeAsync(entity.TipoConcepto);

        return ToDto(entity);
    }

    public async Task<ConceptoCatalogDto?> UpdateAsync(int id, ConceptoCreateUpdateDto dto)
    {
        var entity = await session.GetAsync<ConceptoCatalogEntity>(id);
        if (entity is null) return null;

        ApplyDto(entity, dto);

        using var tx = session.BeginTransaction();
        await session.FlushAsync();
        await tx.CommitAsync();

        if (entity.TipoConcepto is not null)
            await NHibernateUtil.InitializeAsync(entity.TipoConcepto);

        return ToDto(entity);
    }

    private void ApplyDto(ConceptoCatalogEntity entity, ConceptoCreateUpdateDto dto)
    {
        entity.Codigo = dto.Codigo;
        entity.Subcodigo = dto.Subcodigo;
        entity.DescripcionBreve = dto.DescripcionBreve;
        entity.Descripcion = dto.Descripcion;
        entity.AcumulaJubilacion = dto.AcumulaJubilacion;
        entity.AcumulaObraSocial = dto.AcumulaObraSocial;
        entity.AcumulaRemunerativo = dto.AcumulaRemunerativo;
        entity.Basico = dto.Basico;
        entity.Bonificable = dto.Bonificable;
        entity.CalculaTicket = dto.CalculaTicket;
        entity.CalculaPorPersona = dto.CalculaPorPersona;
        entity.CalculoMeses = BuildCalculoMeses(dto.MesesAplicables);
        entity.DeduceJubilacion = dto.DeduceJubilacion;
        entity.DeducePension = dto.DeducePension;
        entity.Especial = dto.Especial;
        entity.Ganancia = dto.Ganancia;
        entity.IdPartidaPresupuestaria = dto.IdPartidaPresupuestaria;
        entity.TipoConcepto = dto.IdTipoConcepto.HasValue
            ? session.Load<TipoConceptoCatalogEntity>(dto.IdTipoConcepto.Value)
            : null;
        entity.ImprimeCantidad = dto.ImprimeCantidad;
        entity.LiquidaSiempre = dto.LiquidaSiempre;
        entity.ParticipaFondo = dto.ParticipaFondo;
        entity.Ppp = dto.Ppp;
        entity.Reliquidar = dto.Reliquidar;

        // Igual patrón que GrupoValorFijoEntity.Tipos: Clear()+Add(Load(...)) es seguro acá porque
        // son referencias a entidades ya persistentes vía Load, no instancias nuevas con mismo id compuesto.
        entity.TiposLiquidacion.Clear();
        foreach (var tipoLiquidacionId in dto.TiposLiquidacionIds.Distinct())
            entity.TiposLiquidacion.Add(session.Load<TipoLiquidacionCatalogEntity>(tipoLiquidacionId));
    }

    // CALMESES es una cadena de 12 caracteres (uno por mes, ene..dic); '1' = aplica ese mes.
    private static string BuildCalculoMeses(IReadOnlyCollection<int> mesesAplicables)
    {
        var meses = new HashSet<int>(mesesAplicables);
        var chars = new char[12];
        for (var i = 0; i < 12; i++)
            chars[i] = meses.Contains(i + 1) ? '1' : '0';

        return new string(chars);
    }

    private static ConceptoCatalogDto ToDto(ConceptoCatalogEntity item, IReadOnlyList<FormulaDto>? formulas = null) => new()
    {
        Id = item.Id,
        Codigo = item.Codigo,
        Subcodigo = item.Subcodigo,
        DescripcionBreve = item.DescripcionBreve,
        Descripcion = item.Descripcion,
        AcumulaJubilacion = item.AcumulaJubilacion,
        AcumulaObraSocial = item.AcumulaObraSocial,
        AcumulaRemunerativo = item.AcumulaRemunerativo,
        Basico = item.Basico,
        Bonificable = item.Bonificable,
        CalculaTicket = item.CalculaTicket,
        CalculaPorPersona = item.CalculaPorPersona,
        MesesAplicables = ParseMesesAplicables(item.CalculoMeses),
        DeduceJubilacion = item.DeduceJubilacion,
        DeducePension = item.DeducePension,
        Especial = item.Especial,
        Ganancia = item.Ganancia,
        IdPartidaPresupuestaria = item.IdPartidaPresupuestaria,
        IdTipoConcepto = item.TipoConcepto?.Id,
        TipoConcepto = item.TipoConcepto?.Descripcion,
        ImprimeCantidad = item.ImprimeCantidad,
        LiquidaSiempre = item.LiquidaSiempre,
        ParticipaFondo = item.ParticipaFondo,
        Ppp = item.Ppp,
        Reliquidar = item.Reliquidar,
        // Solo viene poblado cuando el caller hizo .Fetch(x => x.TiposLiquidacion) (GetByIdAsync);
        // en las búsquedas paginadas se deja vacío para no disparar un N+1 por fila.
        TiposLiquidacion = NHibernateUtil.IsInitialized(item.TiposLiquidacion)
            ? item.TiposLiquidacion.Select(t => new CatalogItemDto { Id = t.Id, Descripcion = t.Descripcion }).ToList()
            : [],
        Formulas = formulas ?? [],
    };

    // CALMESES es una cadena de 12 caracteres (uno por mes, ene..dic); '1' = aplica ese mes.
    private static IReadOnlyList<int> ParseMesesAplicables(string? calculoMeses)
    {
        if (string.IsNullOrEmpty(calculoMeses))
            return [];

        var meses = new List<int>();
        for (var i = 0; i < calculoMeses.Length && i < 12; i++)
        {
            if (calculoMeses[i] == '1')
                meses.Add(i + 1);
        }

        return meses;
    }

    // Sin desambiguación, "25" matchea tanto código como descripción y trae demasiados
    // resultados. Por eso: "cod/subcod" busca por ambos campos exactos, un prefijo
    // "d:"/"desc:" fuerza búsqueda por descripción, y cualquier otro texto busca solo por código.
    private static void ApplyQueryFilter(
        IQueryOver<ConceptoCatalogEntity, ConceptoCatalogEntity> criteria,
        string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return;

        var trimmed = query.Trim();

        var codigoSubcodigoMatch = CodigoSubcodigoRegex.Match(trimmed);
        if (codigoSubcodigoMatch.Success)
        {
            if (!int.TryParse(codigoSubcodigoMatch.Groups["codigo"].Value, out var codigo)
                || !int.TryParse(codigoSubcodigoMatch.Groups["subcodigo"].Value, out var subcodigo))
                return;
            criteria.Where(Restrictions.Eq("Codigo", codigo));
            criteria.Where(Restrictions.Eq("Subcodigo", subcodigo));
            return;
        }

        var descripcionPrefix = DescripcionPrefixes.FirstOrDefault(
            prefix => trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        if (descripcionPrefix is not null)
        {
            var texto = $"%{trimmed[descripcionPrefix.Length..].Trim()}%";
            criteria.Where(Restrictions.Disjunction()
                .Add(Restrictions.InsensitiveLike("DescripcionBreve", texto))
                .Add(Restrictions.InsensitiveLike("Descripcion", texto)));
            return;
        }

        var codigoLike = $"%{trimmed}%";
        // SqlString.Parse (no el constructor directo) reconoce "?" como parámetro.
        criteria.Where(new SQLCriterion(
            SqlString.Parse("lower(to_char({alias}.CODIGO)) like ?"),
            [codigoLike.ToLowerInvariant()],
            [NHibernateUtil.String]));
    }
}

