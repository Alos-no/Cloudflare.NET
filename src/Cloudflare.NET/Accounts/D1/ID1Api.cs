namespace Cloudflare.NET.Accounts.D1;

using Core.Models;
using Models;
using Cloudflare.NET.Accounts.Models;

/// <summary>
///   Provides access to Cloudflare D1 database operations.
///   D1 is Cloudflare's native serverless SQL database built on SQLite.
/// </summary>
/// <remarks>
///   <para>
///     All operations are account-scoped. The account ID is configured
///     via <see cref="Core.CloudflareApiOptions.AccountId" />.
///   </para>
///   <para>
///     <strong>API Limits (Workers Paid plan):</strong>
///     <list type="bullet">
///       <item><description>Max databases: 50,000 per account</description></item>
///       <item><description>Max database size: 10 GB</description></item>
///       <item><description>Max account storage: 1 TB</description></item>
///       <item><description>Max SQL statement length: 100 KB</description></item>
///       <item><description>Max query duration: 30 seconds</description></item>
///       <item><description>Max bound parameters: 100 per query</description></item>
///       <item><description>Max row/string/BLOB size: 2 MB</description></item>
///       <item><description>Max columns per table: 100</description></item>
///     </list>
///   </para>
///   <para>
///     <strong>Important:</strong> D1's REST API is best suited for administrative use
///     as the global Cloudflare API rate limit applies. For high-throughput query access
///     outside Workers, consider creating a proxy Worker with the D1 Worker binding.
///   </para>
/// </remarks>
/// <seealso href="https://developers.cloudflare.com/d1/" />
/// <seealso href="https://developers.cloudflare.com/d1/platform/limits/" />
public interface ID1Api
{
  #region Database Operations

  /// <summary>Lists D1 databases in the account.</summary>
  /// <param name="filters">Optional filters for pagination and name search.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>A page of databases with pagination info.</returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/d1/subresources/database/methods/list/" />
  Task<PagePaginatedResult<D1Database>> ListAsync(
    ListD1DatabasesFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>Lists all D1 databases, automatically handling pagination.</summary>
  /// <param name="filters">Optional filters for name search.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>An async enumerable of all databases.</returns>
  IAsyncEnumerable<D1Database> ListAllAsync(
    ListD1DatabasesFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>Creates a new D1 database.</summary>
  /// <param name="name">The name for the new database.</param>
  /// <param name="primaryLocationHint">Optional region hint for database placement.</param>
  /// <param name="jurisdiction">Optional jurisdictional restriction (overrides location hint).</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The created database.</returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/d1/subresources/database/methods/create/" />
  Task<D1Database> CreateAsync(
    string name,
    R2LocationHint? primaryLocationHint = null,
    D1Jurisdiction? jurisdiction = null,
    CancellationToken cancellationToken = default);

  /// <summary>Gets a specific D1 database by ID.</summary>
  /// <param name="databaseId">The database UUID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The database details.</returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/d1/subresources/database/methods/get/" />
  Task<D1Database> GetAsync(
    string databaseId,
    CancellationToken cancellationToken = default);

  /// <summary>Updates a D1 database's configuration.</summary>
  /// <param name="databaseId">The database UUID.</param>
  /// <param name="options">The update options (e.g., read replication settings).</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The updated database.</returns>
  /// <remarks>
  ///   Use this to enable/disable read replication by setting
  ///   ReadReplication.Mode to "auto" or "disabled".
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/d1/subresources/database/methods/update/" />
  Task<D1Database> UpdateAsync(
    string databaseId,
    UpdateD1DatabaseOptions options,
    CancellationToken cancellationToken = default);

  /// <summary>Deletes a D1 database.</summary>
  /// <param name="databaseId">The database UUID to delete.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <remarks>
  ///   This permanently deletes the database and all its data.
  ///   The operation cannot be undone.
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/d1/subresources/database/methods/delete/" />
  Task DeleteAsync(
    string databaseId,
    CancellationToken cancellationToken = default);

  #endregion


  #region Query Operations

  /// <summary>Executes a SQL query and returns results as objects.</summary>
  /// <param name="databaseId">The database UUID.</param>
  /// <param name="sql">The SQL statement to execute.</param>
  /// <param name="params">Optional parameter values for parameterized queries.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Query results with metadata.</returns>
  /// <remarks>
  ///   Use ? placeholders in SQL and provide values in params array.
  ///   Multiple statements can be separated by semicolons for batch execution.
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/d1/subresources/database/methods/query/" />
  Task<IReadOnlyList<D1QueryResult>> QueryAsync(
    string databaseId,
    string sql,
    IReadOnlyList<object?>? @params = null,
    CancellationToken cancellationToken = default);

  /// <summary>Executes a SQL query and returns strongly-typed results.</summary>
  /// <typeparam name="TRow">The type to deserialize each row into.</typeparam>
  /// <param name="databaseId">The database UUID.</param>
  /// <param name="sql">The SQL statement to execute.</param>
  /// <param name="params">Optional parameter values for parameterized queries.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Typed query results with metadata.</returns>
  Task<IReadOnlyList<D1QueryResult<TRow>>> QueryAsync<TRow>(
    string databaseId,
    string sql,
    IReadOnlyList<object?>? @params = null,
    CancellationToken cancellationToken = default);

  /// <summary>Executes a SQL query and returns results as arrays (raw format).</summary>
  /// <param name="databaseId">The database UUID.</param>
  /// <param name="sql">The SQL statement to execute.</param>
  /// <param name="params">Optional parameter values for parameterized queries.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Raw query results with rows as arrays.</returns>
  /// <remarks>
  ///   The raw endpoint returns rows as arrays instead of objects,
  ///   which is more efficient for large result sets.
  ///   Column order matches the SELECT clause order.
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/d1/subresources/database/methods/raw/" />
  Task<IReadOnlyList<D1RawQueryResult>> QueryRawAsync(
    string databaseId,
    string sql,
    IReadOnlyList<object?>? @params = null,
    CancellationToken cancellationToken = default);

  #endregion


  #region Export Operations

  /// <summary>Initiates an export of the database to SQL format.</summary>
  /// <param name="databaseId">The database UUID.</param>
  /// <param name="dumpOptions">Optional filtering options (schema only, data only, specific tables).</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Initial export response with bookmark for polling.</returns>
  /// <remarks>
  ///   <para>
  ///     Export is an async operation that requires polling.
  ///     Call <see cref="PollExportAsync" /> with the returned bookmark until status is "complete".
  ///   </para>
  ///   <para>
  ///     The database is unavailable for queries during export.
  ///     Exports that aren't polled will auto-cancel.
  ///   </para>
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/d1/subresources/database/methods/export/" />
  Task<D1ExportResponse> StartExportAsync(
    string databaseId,
    D1ExportDumpOptions? dumpOptions = null,
    CancellationToken cancellationToken = default);

  /// <summary>Polls the status of an ongoing export operation.</summary>
  /// <param name="databaseId">The database UUID.</param>
  /// <param name="bookmark">The bookmark from the previous response.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Export status. When complete, includes signed_url for download.</returns>
  Task<D1ExportResponse> PollExportAsync(
    string databaseId,
    string bookmark,
    CancellationToken cancellationToken = default);

  #endregion


  #region Import Operations

  /// <summary>Initiates an import operation by requesting an upload URL.</summary>
  /// <param name="databaseId">The database UUID.</param>
  /// <param name="etag">MD5 hash of the SQL file content.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Response containing upload_url and filename for SQL file upload.</returns>
  /// <remarks>
  ///   <para>
  ///     Import workflow:
  ///     <list type="number">
  ///       <item><description>Compute MD5 hash of your SQL file</description></item>
  ///       <item><description>Call <see cref="StartImportAsync" /> with the hash to get upload URL and filename</description></item>
  ///       <item><description>PUT your SQL file to the upload_url (verify ETag matches)</description></item>
  ///       <item><description>Call <see cref="CompleteImportAsync" /> with etag and filename to start ingestion</description></item>
  ///       <item><description>Poll with <see cref="PollImportAsync" /> until status is "complete"</description></item>
  ///     </list>
  ///   </para>
  ///   <para>
  ///     The database is blocked during import operations.
  ///   </para>
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/d1/subresources/database/methods/import/" />
  Task<D1ImportResponse> StartImportAsync(
    string databaseId,
    string etag,
    CancellationToken cancellationToken = default);

  /// <summary>Completes an import after uploading the SQL file.</summary>
  /// <param name="databaseId">The database UUID.</param>
  /// <param name="etag">The MD5 hash/ETag of the uploaded file.</param>
  /// <param name="filename">The filename returned from <see cref="StartImportAsync" />.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Import response with bookmark for polling.</returns>
  Task<D1ImportResponse> CompleteImportAsync(
    string databaseId,
    string etag,
    string filename,
    CancellationToken cancellationToken = default);

  /// <summary>Polls the status of an ongoing import operation.</summary>
  /// <param name="databaseId">The database UUID.</param>
  /// <param name="bookmark">The bookmark from the previous response.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Import status with progress information.</returns>
  Task<D1ImportResponse> PollImportAsync(
    string databaseId,
    string bookmark,
    CancellationToken cancellationToken = default);

  #endregion
}
