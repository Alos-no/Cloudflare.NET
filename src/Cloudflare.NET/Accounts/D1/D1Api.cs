namespace Cloudflare.NET.Accounts.D1;

using System.Runtime.CompilerServices;
using Core;
using Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Cloudflare.NET.Accounts.Models;

/// <summary>Implementation of Cloudflare D1 Database API operations.</summary>
/// <remarks>
///   <para>
///     D1 is Cloudflare's native serverless SQL database built on SQLite.
///     All operations are account-scoped, using the account ID from configuration.
///   </para>
///   <para>
///     The REST API is subject to global Cloudflare API rate limits and is best suited
///     for administrative operations. For high-throughput query access outside Workers,
///     consider creating a proxy Worker with D1 Worker bindings.
///   </para>
/// </remarks>
public class D1Api : ApiResource, ID1Api
{
  #region Properties & Fields - Non-Public

  /// <summary>The Cloudflare Account ID.</summary>
  private readonly string _accountId;

  #endregion


  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="D1Api" /> class.</summary>
  /// <param name="httpClient">The HttpClient for making requests.</param>
  /// <param name="options">The Cloudflare API options containing the account ID.</param>
  /// <param name="loggerFactory">The factory to create loggers.</param>
  public D1Api(HttpClient httpClient, IOptions<CloudflareApiOptions> options, ILoggerFactory loggerFactory)
    : base(httpClient, loggerFactory.CreateLogger<D1Api>())
  {
    _accountId = options.Value.AccountId;
  }

  #endregion


  #region Database Operations

  /// <inheritdoc />
  public async Task<PagePaginatedResult<D1Database>> ListAsync(
    ListD1DatabasesFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    var queryParams = new List<string>();

    if (!string.IsNullOrEmpty(filters?.Name))
      queryParams.Add($"name={Uri.EscapeDataString(filters.Name)}");

    if (filters?.Page.HasValue == true)
      queryParams.Add($"page={filters.Page.Value}");

    if (filters?.PerPage.HasValue == true)
      queryParams.Add($"per_page={filters.PerPage.Value}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;
    var endpoint = $"accounts/{_accountId}/d1/database{queryString}";

    return await GetPagePaginatedResultAsync<D1Database>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  /// <remarks>
  ///   <para>
  ///     The D1 API returns <c>total_count=0</c> and <c>total_pages=0</c> in the pagination info, which is
  ///     inconsistent with the standard Cloudflare API schema. To work around this limitation, this implementation
  ///     checks if the current page is full (item count equals PerPage) to determine if there are more pages.
  ///   </para>
  /// </remarks>
  public async IAsyncEnumerable<D1Database> ListAllAsync(
    ListD1DatabasesFilters? filters = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    var page = 1;
    var perPage = filters?.PerPage ?? 100;
    var hasMore = true;

    while (hasMore)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var pageFilters = new ListD1DatabasesFilters(
        Name: filters?.Name,
        Page: page,
        PerPage: perPage
      );

      var result = await ListAsync(pageFilters, cancellationToken);

      foreach (var database in result.Items)
      {
        yield return database;
      }

      // D1 API limitation: TotalPages is always 0, so we cannot rely on it.
      // Instead, check if the current page is full (item count equals PerPage).
      // If the page is full, there might be more items on the next page.
      hasMore = result.Items.Count >= perPage;

      page++;
    }
  }

  /// <inheritdoc />
  public async Task<D1Database> CreateAsync(
    string name,
    R2LocationHint? primaryLocationHint = null,
    D1Jurisdiction? jurisdiction = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/d1/database";

    var request = new CreateD1DatabaseRequest(
      Name: name,
      PrimaryLocationHint: primaryLocationHint?.Value,
      Jurisdiction: jurisdiction?.Value
    );

    return await PostAsync<D1Database>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<D1Database> GetAsync(
    string databaseId,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/d1/database/{Uri.EscapeDataString(databaseId)}";

    return await GetAsync<D1Database>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<D1Database> UpdateAsync(
    string databaseId,
    UpdateD1DatabaseOptions options,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/d1/database/{Uri.EscapeDataString(databaseId)}";

    var request = new UpdateD1DatabaseRequest(
      ReadReplication: options.ReadReplication
    );

    return await PatchAsync<D1Database>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteAsync(
    string databaseId,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/d1/database/{Uri.EscapeDataString(databaseId)}";

    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  #endregion


  #region Query Operations

  /// <inheritdoc />
  public async Task<IReadOnlyList<D1QueryResult>> QueryAsync(
    string databaseId,
    string sql,
    IReadOnlyList<object?>? @params = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/d1/database/{Uri.EscapeDataString(databaseId)}/query";

    var request = new D1QueryRequest(
      Sql: sql,
      Params: @params
    );

    return await PostAsync<IReadOnlyList<D1QueryResult>>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<D1QueryResult<TRow>>> QueryAsync<TRow>(
    string databaseId,
    string sql,
    IReadOnlyList<object?>? @params = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/d1/database/{Uri.EscapeDataString(databaseId)}/query";

    var request = new D1QueryRequest(
      Sql: sql,
      Params: @params
    );

    return await PostAsync<IReadOnlyList<D1QueryResult<TRow>>>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<D1RawQueryResult>> QueryRawAsync(
    string databaseId,
    string sql,
    IReadOnlyList<object?>? @params = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/d1/database/{Uri.EscapeDataString(databaseId)}/raw";

    var request = new D1QueryRequest(
      Sql: sql,
      Params: @params
    );

    return await PostAsync<IReadOnlyList<D1RawQueryResult>>(endpoint, request, cancellationToken);
  }

  #endregion


  #region Export Operations

  /// <inheritdoc />
  public async Task<D1ExportResponse> StartExportAsync(
    string databaseId,
    D1ExportDumpOptions? dumpOptions = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/d1/database/{Uri.EscapeDataString(databaseId)}/export";

    var request = new D1ExportRequest(
      OutputFormat: "polling",
      CurrentBookmark: null,
      DumpOptions: dumpOptions
    );

    return await PostAsync<D1ExportResponse>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<D1ExportResponse> PollExportAsync(
    string databaseId,
    string bookmark,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/d1/database/{Uri.EscapeDataString(databaseId)}/export";

    var request = new D1ExportRequest(
      OutputFormat: "polling",
      CurrentBookmark: bookmark,
      DumpOptions: null
    );

    return await PostAsync<D1ExportResponse>(endpoint, request, cancellationToken);
  }

  #endregion


  #region Import Operations

  /// <inheritdoc />
  public async Task<D1ImportResponse> StartImportAsync(
    string databaseId,
    string etag,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/d1/database/{Uri.EscapeDataString(databaseId)}/import";

    var request = new D1ImportRequest(
      Action: "init",
      Etag: etag
    );

    return await PostAsync<D1ImportResponse>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<D1ImportResponse> CompleteImportAsync(
    string databaseId,
    string etag,
    string filename,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/d1/database/{Uri.EscapeDataString(databaseId)}/import";

    var request = new D1ImportRequest(
      Action: "ingest",
      Etag: etag,
      Filename: filename
    );

    return await PostAsync<D1ImportResponse>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<D1ImportResponse> PollImportAsync(
    string databaseId,
    string bookmark,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"accounts/{_accountId}/d1/database/{Uri.EscapeDataString(databaseId)}/import";

    var request = new D1ImportRequest(
      Action: "poll",
      CurrentBookmark: bookmark
    );

    return await PostAsync<D1ImportResponse>(endpoint, request, cancellationToken);
  }

  #endregion
}
