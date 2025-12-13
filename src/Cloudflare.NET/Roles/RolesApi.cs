namespace Cloudflare.NET.Roles;

using Core;
using Core.Internal;
using Core.Models;
using Microsoft.Extensions.Logging;
using Models;


/// <summary>
///   Implementation of <see cref="IRolesApi"/> for Cloudflare Account Roles.
///   <para>
///     Provides read-only access to predefined Cloudflare roles that define
///     sets of permissions for account members.
///   </para>
/// </summary>
public class RolesApi : ApiResource, IRolesApi
{
  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="RolesApi"/> class.
  /// </summary>
  /// <param name="httpClient">The HttpClient for making requests.</param>
  /// <param name="loggerFactory">The factory to create loggers for this resource.</param>
  public RolesApi(HttpClient httpClient, ILoggerFactory loggerFactory)
    : base(httpClient, loggerFactory.CreateLogger<RolesApi>())
  {
  }

  #endregion


  #region Account Roles

  /// <inheritdoc />
  public async Task<PagePaginatedResult<AccountRole>> ListAccountRolesAsync(
    string accountId,
    ListAccountRolesFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);

    var endpoint = BuildRolesListQueryString(accountId, filters);

    return await GetPagePaginatedResultAsync<AccountRole>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<AccountRole> ListAllAccountRolesAsync(
    string accountId,
    ListAccountRolesFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);

    // Build base endpoint without pagination parameters (handled by GetPaginatedAsync).
    var baseFilters = filters is not null ? filters with { Page = null, PerPage = null } : null;
    var endpoint = BuildRolesListQueryString(accountId, baseFilters);

    return GetPaginatedAsync<AccountRole>(endpoint, filters?.PerPage, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<AccountRole> GetAccountRoleAsync(
    string accountId,
    string roleId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(roleId);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/roles/{Uri.EscapeDataString(roleId)}";

    return await GetAsync<AccountRole>(endpoint, cancellationToken);
  }

  #endregion


  #region Helpers

  /// <summary>
  ///   Builds the query string for the roles list endpoint.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional filters to apply.</param>
  /// <returns>The endpoint with query string.</returns>
  private static string BuildRolesListQueryString(string accountId, ListAccountRolesFilters? filters)
  {
    var queryParams = new List<string>();

    if (filters?.Page is not null)
      queryParams.Add($"page={filters.Page}");
    if (filters?.PerPage is not null)
      queryParams.Add($"per_page={filters.PerPage}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;

    return $"accounts/{Uri.EscapeDataString(accountId)}/roles{queryString}";
  }

  #endregion
}
