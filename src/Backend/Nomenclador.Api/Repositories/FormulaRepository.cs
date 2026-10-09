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
        try
        {
            await ExecuteProcedureDdlAsync(entity.SpName!, entity.Codigo!);
        }
        catch (Exception ddlException)
        {
            Exception? cleanupException = null;
            try
            {
                await DropProcedureAsync(entity.SpName!);
            }
            catch (Exception ex)
            {
                cleanupException = ex;
            }

            try
            {
                await DeleteFormulaAsync(entity);
            }
            catch (Exception ex)
            {
                cleanupException = cleanupException is null ? ex : new AggregateException(cleanupException, ex);
            }

            if (cleanupException is not null)
                throw new AggregateException("No se pudo crear la fórmula y falló la compensación.", ddlException, cleanupException);

            throw;
        }

        return ToDto(entity, concepto);
    }

    public async Task<FormulaDetailDto?> UpdateAsync(int id, FormulaCreateUpdateDto dto)
    {
        var entity = await session.GetAsync<FormulaEntity>(id);
        if (entity is null) return null;

        var concepto = await session.GetAsync<ConceptoCatalogEntity>(entity.ConceptoId);
        var oldCondicion = entity.Condicion;
        var oldAccion = entity.Accion;
        var oldCondicionInput = entity.CondicionInput;
        var oldAccionInput = entity.AccionInput;
        var oldCodigo = entity.Codigo;
        var oldSpName = entity.SpName;
        var spName = entity.SpName ?? FormulaCodeGenerator.BuildSpName(concepto?.DescripcionBreve ?? "FORMULA", entity.Id);

        entity.Condicion = dto.Condicion;
        entity.Accion = dto.Accion;

        using (var tx = session.BeginTransaction())
        {
            // El SpName no cambia (incluye el IDFORM, que es inmutable) — se recompila el mismo procedure.
            var primitivaNames = await GetPrimitivaNombresAsync();
            entity.SpName = spName;
            (entity.Codigo, entity.CondicionInput) = FormulaCodeGenerator.Generate(spName, dto.Condicion, dto.Accion, primitivaNames);
            entity.AccionInput = entity.CondicionInput;

            await session.FlushAsync();
            await tx.CommitAsync();
        }

        try
        {
            await ExecuteProcedureDdlAsync(entity.SpName!, entity.Codigo!);
        }
        catch (Exception ddlException)
        {
            entity.Condicion = oldCondicion;
            entity.Accion = oldAccion;
            entity.CondicionInput = oldCondicionInput;
            entity.AccionInput = oldAccionInput;
            entity.Codigo = oldCodigo;
            entity.SpName = oldSpName;

            using (var tx = session.BeginTransaction())
            {
                await session.FlushAsync();
                await tx.CommitAsync();
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(oldSpName) && !string.IsNullOrWhiteSpace(oldCodigo))
                    await ExecuteProcedureDdlAsync(oldSpName, oldCodigo);
                else
                    await DropProcedureAsync(spName);
            }
            catch (Exception restoreException)
            {
                try
                {
                    await DeleteFormulaAsync(entity);
                }
                catch (Exception cleanupException)
                {
                    throw new AggregateException("Falló la actualización y no se pudo restaurar la fórmula.", ddlException, restoreException, cleanupException);
                }

                throw new AggregateException("Falló la actualización y se eliminó la fórmula al no poder restaurar su procedure.", ddlException, restoreException);
            }

            throw;
        }

        return ToDto(entity, concepto);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await session.GetAsync<FormulaEntity>(id);
        if (entity is null) return false;

        var spName = entity.SpName;
        var codigo = entity.Codigo;

        if (!string.IsNullOrWhiteSpace(spName))
            await DropProcedureAsync(spName);

        try
        {
            using var tx = session.BeginTransaction();
            await session.DeleteAsync(entity);
            await session.FlushAsync();
            await tx.CommitAsync();
        }
        catch (Exception deleteException)
        {
            if (!string.IsNullOrWhiteSpace(spName) && !string.IsNullOrWhiteSpace(codigo))
            {
                try
                {
                    await ExecuteProcedureDdlAsync(spName, codigo);
                }
                catch (Exception restoreException)
                {
                    try
                    {
                        await DeleteFormulaAsync(entity);
                    }
                    catch (Exception cleanupException)
                    {
                        throw new AggregateException("Falló la eliminación y no se pudo restaurar el procedure ni eliminar la fórmula.", deleteException, restoreException, cleanupException);
                    }

                    throw new AggregateException("Falló la eliminación y se eliminó la fórmula al no poder restaurar su procedure.", deleteException, restoreException);
                }
            }

            throw;
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

    private async Task ExecuteProcedureDdlAsync(string spName, string ddl)
    {
        await ExecuteDdlAsync(ddl);

        var connection = (DbConnection)session.Connection!;
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT LINE || ': ' || TEXT
            FROM USER_ERRORS
            WHERE NAME = :name AND TYPE = 'PROCEDURE' AND ATTRIBUTE = 'ERROR'
            ORDER BY SEQUENCE
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "name";
        parameter.Value = spName;
        command.Parameters.Add(parameter);

        var errors = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            errors.Add(reader.GetString(0));

        if (errors.Count > 0)
            throw new InvalidOperationException($"No se pudo compilar el procedure {spName}: {string.Join(" ", errors)}");
    }

    private async Task DropProcedureAsync(string spName)
    {
        // ORA-04043 (procedure inexistente) se ignora.
        await ExecuteDdlAsync($"""
            BEGIN
                EXECUTE IMMEDIATE 'DROP PROCEDURE {spName}';
            EXCEPTION
                WHEN OTHERS THEN IF SQLCODE != -4043 THEN RAISE; END IF;
            END;
            """);
    }

    private async Task DeleteFormulaAsync(FormulaEntity entity)
    {
        using var tx = session.BeginTransaction();
        await session.DeleteAsync(entity);
        await session.FlushAsync();
        await tx.CommitAsync();
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
