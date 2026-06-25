namespace Schemantic.Api;

/// <summary>
/// Paginated list response envelope.
/// </summary>
public sealed class PagedResult<T>
{
    /// <summary>One-based page number.</summary>
    public int Page { get; init; }

    /// <summary>Number of rows requested per page.</summary>
    public int PageSize { get; init; }

    /// <summary>Rows returned for this page.</summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
}
