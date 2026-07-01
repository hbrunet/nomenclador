namespace Nomenclador.Api.Models;

public sealed class PagedResult<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
