using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
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

    public async Task<IReadOnlyCollection<ConceptoCatalogDto>> GetAllAsync(string? query)
    {
        ConceptoCatalogEntity alias = null!;
        var criteria = session.QueryOver(() => alias);

        ApplyQueryFilter(criteria, query);

        var items = await criteria
            .OrderBy(() => alias.Codigo).Asc
            .ThenBy(() => alias.Subcodigo).Asc
            .Take(100)
            .ListAsync();

        return items
            .Select(item => new ConceptoCatalogDto
            {
                Id = item.Id,
                Codigo = item.Codigo,
                Subcodigo = item.Subcodigo,
                DescripcionBreve = item.DescripcionBreve,
                Descripcion = item.Descripcion,
            })
            .ToList();
    }

    public async Task<(IReadOnlyCollection<ConceptoCatalogDto> Items, int Total)> GetPagedAsync(
        string? query, int page, int pageSize)
    {
        ConceptoCatalogEntity alias = null!;
        var criteria = session.QueryOver(() => alias);

        ApplyQueryFilter(criteria, query);

        var total = await criteria.RowCountAsync();

        var items = await criteria
            .OrderBy(() => alias.Codigo).Asc
            .ThenBy(() => alias.Subcodigo).Asc
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ListAsync();

        var dtos = items
            .Select(item => new ConceptoCatalogDto
            {
                Id = item.Id,
                Codigo = item.Codigo,
                Subcodigo = item.Subcodigo,
                DescripcionBreve = item.DescripcionBreve,
                Descripcion = item.Descripcion,
            })
            .ToList();

        return (dtos, total);
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
            var codigo = int.Parse(codigoSubcodigoMatch.Groups["codigo"].Value);
            var subcodigo = int.Parse(codigoSubcodigoMatch.Groups["subcodigo"].Value);
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


