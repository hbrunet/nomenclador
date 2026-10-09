using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using NHibernate;
using NHibernate.Linq;
using Nomenclador.Api.DTOs;
using Nomenclador.Api.Models;
using Nomenclador.Api.Services;

namespace Nomenclador.Api.Repositories;

public sealed class FormulaRepository(NHibernate.ISession session)
{
    private static readonly Regex TokenRegex = new(@"[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);
    private static readonly string[] IgnoredKeywords = ["AND", "OR", "NOT", "ROUND"];

    public async Task<FormulaDetailDto?> GetByIdAsync(int id)
    {
        var entity = await session.GetAsync<FormulaEntity>(id);
        if (entity is null) return null;

        var concepto = await session.GetAsync<ConceptoCatalogEntity>(entity.ConceptoId);

        return ToDto(entity, concepto);
    }

    public async Task<FormulaDetailDto> CreateAsync(FormulaCreateUpdateDto dto)
    {
        var concepto = await session.GetAsync<ConceptoCatalogEntity>(dto.ConceptoId)
            ?? throw new KeyNotFoundException($"No se encontró el concepto {dto.ConceptoId}.");

        var entity = new FormulaEntity
        {
            ConceptoId = dto.ConceptoId,
            Condicion = dto.Condicion,
            Accion = dto.Accion,
        };

        using (var tx = session.BeginTransaction())
        {
            var maxOrden = await session.Query<FormulaEntity>()
                .Where(x => x.ConceptoId == dto.ConceptoId)
                .Select(x => (int?)x.OrdenEjec)
                .MaxAsync() ?? 0;
            entity.OrdenEjec = maxOrden + 1;

            await session.SaveAsync(entity);

            var primitivaNames = await GetPrimitivaNombresAsync();
            var spName = FormulaCodeGenerator.BuildSpName(concepto.DescripcionBreve, entity.Id);
            entity.SpName = spName;
            (entity.Codigo, entity.CondicionInput) = FormulaCodeGenerator.Generate(spName, dto.Condicion, dto.Accion, primitivaNames);
            entity.AccionInput = entity.CondicionInput;

            await session.FlushAsync();
            await tx.CommitAsync();
        }

        // El DDL de Oracle hace commit implícito propio; se ejecuta aparte de la transacción NHibernate.
        await ExecuteDdlAsync(entity.Codigo!);

        return ToDto(entity, concepto);
    }

    public async Task<FormulaDetailDto?> UpdateAsync(int id, FormulaCreateUpdateDto dto)
    {
        var entity = await session.GetAsync<FormulaEntity>(id);
        if (entity is null) return null;

        var concepto = await session.GetAsync<ConceptoCatalogEntity>(entity.ConceptoId);

        entity.Condicion = dto.Condicion;
        entity.Accion = dto.Accion;

        using (var tx = session.BeginTransaction())
        {
            // El SpName no cambia (incluye el IDFORM, que es inmutable) — se recompila el mismo procedure.
            var spName = entity.SpName ?? FormulaCodeGenerator.BuildSpName(concepto?.DescripcionBreve ?? "FORMULA", entity.Id);
            var primitivaNames = await GetPrimitivaNombresAsync();
            entity.SpName = spName;
            (entity.Codigo, entity.CondicionInput) = FormulaCodeGenerator.Generate(spName, dto.Condicion, dto.Accion, primitivaNames);
            entity.AccionInput = entity.CondicionInput;

            await session.FlushAsync();
            await tx.CommitAsync();
        }

        await ExecuteDdlAsync(entity.Codigo!);

        return ToDto(entity, concepto);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await session.GetAsync<FormulaEntity>(id);
        if (entity is null) return false;

        var spName = entity.SpName;

        using (var tx = session.BeginTransaction())
        {
            await session.DeleteAsync(entity);
            await session.FlushAsync();
            await tx.CommitAsync();
        }

        if (!string.IsNullOrWhiteSpace(spName))
        {
            // ORA-04043 (procedure inexistente) se ignora: la fórmula ya quedó borrada igual.
            await ExecuteDdlAsync($"""
                BEGIN
                    EXECUTE IMMEDIATE 'DROP PROCEDURE {spName}';
                EXCEPTION
                    WHEN OTHERS THEN IF SQLCODE != -4043 THEN RAISE; END IF;
                END;
                """);
        }

        return true;
    }

    public async Task<FormulaVerificarResultDto> VerificarAsync(FormulaVerificarDto dto)
    {
        var errores = ValidarSintaxis(dto.Condicion, dto.Accion);

        if (string.IsNullOrWhiteSpace(dto.Condicion))
            errores.Add("La condición es obligatoria.");
        if (string.IsNullOrWhiteSpace(dto.Accion))
            errores.Add("La acción es obligatoria.");

        var primitivaNames = await GetPrimitivaNombresAsync();
        var desconocidas = new HashSet<string>(StringComparer.Ordinal);
        foreach (var texto in new[] { dto.Condicion, dto.Accion })
        {
            if (string.IsNullOrEmpty(texto)) continue;
            foreach (Match match in TokenRegex.Matches(texto))
            {
                if (primitivaNames.Contains(match.Value)) continue;
                if (IgnoredKeywords.Contains(match.Value, StringComparer.OrdinalIgnoreCase)) continue;
                desconocidas.Add(match.Value);
            }
        }
        foreach (var nombre in desconocidas)
            errores.Add($"\"{nombre}\" no es una primitiva conocida.");

        return new FormulaVerificarResultDto { Valida = errores.Count == 0, Errores = errores };
    }

    // Gramática completa (paréntesis, operadores, ROUND(...)) además del simple conteo de
    // paréntesis — reusado tanto por VerificarAsync como por Create/UpdateAsync para no aceptar
    // una expresión mal armada que después falle al compilar el procedure generado en Oracle.
    public static List<string> ValidarSintaxis(string? condicion, string? accion)
    {
        var errores = new List<string>();

        var errorCondicion = FormulaExpressionValidator.ValidarCondicion(condicion);
        if (errorCondicion is not null)
            errores.Add($"La condición tiene un error de sintaxis: {errorCondicion}");

        var errorAccion = FormulaExpressionValidator.ValidarAccion(accion);
        if (errorAccion is not null)
            errores.Add($"La acción tiene un error de sintaxis: {errorAccion}");

        return errores;
    }

    private async Task<HashSet<string>> GetPrimitivaNombresAsync()
    {
        var nombres = await session.Query<PrimitivaEntity>()
            .Select(x => x.Nombre)
            .ToListAsync();

        return new HashSet<string>(nombres.Where(n => !string.IsNullOrEmpty(n))!, StringComparer.Ordinal);
    }

    private async Task ExecuteDdlAsync(string ddl)
    {
        var connection = (DbConnection)session.Connection!;
        await using var command = connection.CreateCommand();
        command.CommandText = ddl;
        command.CommandType = CommandType.Text;
        await command.ExecuteNonQueryAsync();
    }

    private static FormulaDetailDto ToDto(FormulaEntity entity, ConceptoCatalogEntity? concepto) => new()
    {
        Id = entity.Id,
        ConceptoId = entity.ConceptoId,
        ConceptoCodigo = concepto?.Codigo ?? 0,
        ConceptoSubcodigo = concepto?.Subcodigo ?? 0,
        ConceptoDescripcion = concepto?.Descripcion ?? "No encontrado en el catálogo",
        Condicion = entity.Condicion ?? string.Empty,
        Accion = entity.Accion ?? string.Empty,
        OrdenEjec = entity.OrdenEjec,
        SpName = entity.SpName,
    };
}
