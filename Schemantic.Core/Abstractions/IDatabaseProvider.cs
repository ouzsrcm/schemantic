using Schemantic.Core.Model;

namespace Schemantic.Core.Abstractions;

/// <summary>
/// Reads database metadata from a specific database engine and maps it to the shared schema model.
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>
    /// Provider identifier (e.g. <c>"SqlServer"</c>).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Connects using the given connection string, reads metadata, and returns a populated schema.
    /// </summary>
    /// <param name="connectionString">Connection string for the target database.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A <see cref="DatabaseSchema"/> populated from the database metadata.</returns>
    Task<DatabaseSchema> ReadSchemaAsync(
        string connectionString,
        CancellationToken cancellationToken = default);
}
