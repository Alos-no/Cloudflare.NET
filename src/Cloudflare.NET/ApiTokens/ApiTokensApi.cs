namespace Cloudflare.NET.ApiTokens;

using Core;
using Core.Internal;
using Core.Models;
using Microsoft.Extensions.Logging;
using Models;

/// <summary>
///   Implements the API for managing Cloudflare API tokens.
///   <para>
///     API tokens provide fine-grained access control for Cloudflare API operations.
///     Each token has policies that define what resources and actions are allowed.
///   </para>
/// </summary>
public class ApiTokensApi : ApiResource, IApiTokensApi
{
  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="ApiTokensApi" /> class.</summary>
  /// <param name="httpClient">The HttpClient for making requests.</param>
  /// <param name="loggerFactory">The factory to create loggers for this resource.</param>
  public ApiTokensApi(HttpClient httpClient, ILoggerFactory loggerFactory)
    : base(httpClient, loggerFactory.CreateLogger<ApiTokensApi>())
  {
  }

  #endregion


  #region Account Tokens

  /// <inheritdoc />
  public async Task<PagePaginatedResult<ApiToken>> ListAccountTokensAsync(
    string accountId,
    ListApiTokensFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);

    var endpoint = BuildTokensListQueryString(accountId, filters);

    return await GetPagePaginatedResultAsync<ApiToken>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<ApiToken> ListAllAccountTokensAsync(
    string accountId,
    ListApiTokensFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);

    // Build base endpoint without pagination parameters (handled by GetPaginatedAsync).
    var baseFilters = filters is not null ? filters with { Page = null, PerPage = null } : null;
    var endpoint = BuildTokensListQueryString(accountId, baseFilters);

    return GetPaginatedAsync<ApiToken>(endpoint, filters?.PerPage, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<ApiToken> GetAccountTokenAsync(
    string accountId,
    string tokenId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(tokenId);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/tokens/{Uri.EscapeDataString(tokenId)}";

    return await GetAsync<ApiToken>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<CreateApiTokenResult> CreateAccountTokenAsync(
    string accountId,
    CreateApiTokenRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/tokens";

    return await PostAsync<CreateApiTokenResult>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<ApiToken> UpdateAccountTokenAsync(
    string accountId,
    string tokenId,
    UpdateApiTokenRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(tokenId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/tokens/{Uri.EscapeDataString(tokenId)}";

    return await PutAsync<ApiToken>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteAccountTokenAsync(
    string accountId,
    string tokenId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(tokenId);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/tokens/{Uri.EscapeDataString(tokenId)}";

    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<VerifyTokenResult> VerifyAccountTokenAsync(
    string accountId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/tokens/verify";

    return await GetAsync<VerifyTokenResult>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<string> RollAccountTokenAsync(
    string accountId,
    string tokenId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(tokenId);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/tokens/{Uri.EscapeDataString(tokenId)}/value";

    // The API returns the new token value directly as a string in the result field.
    return await PutAsync<string>(endpoint, new { }, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<PagePaginatedResult<PermissionGroup>> GetAccountPermissionGroupsAsync(
    string accountId,
    ListPermissionGroupsFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);

    var endpoint = BuildPermissionGroupsQueryString(accountId, filters);

    return await GetPagePaginatedResultAsync<PermissionGroup>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<PermissionGroup> GetAllAccountPermissionGroupsAsync(
    string accountId,
    ListPermissionGroupsFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);

    // Note: The permission_groups endpoint does NOT support pagination.
    // All groups are returned in a single response. GetPaginatedAsync handles this gracefully.
    var endpoint = BuildPermissionGroupsQueryString(accountId, filters);

    return GetPaginatedAsync<PermissionGroup>(endpoint, perPage: null, cancellationToken);
  }

  #endregion


  #region User Tokens

  /// <inheritdoc />
  public async Task<PagePaginatedResult<ApiToken>> ListUserTokensAsync(
    ListApiTokensFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = BuildUserTokensListQueryString(filters);

    return await GetPagePaginatedResultAsync<ApiToken>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<ApiToken> ListAllUserTokensAsync(
    ListApiTokensFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    // Build base endpoint without pagination parameters (handled by GetPaginatedAsync).
    var baseFilters = filters is not null ? filters with { Page = null, PerPage = null } : null;
    var endpoint = BuildUserTokensListQueryString(baseFilters);

    return GetPaginatedAsync<ApiToken>(endpoint, filters?.PerPage, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<ApiToken> GetUserTokenAsync(
    string tokenId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(tokenId);

    var endpoint = $"user/tokens/{Uri.EscapeDataString(tokenId)}";

    return await GetAsync<ApiToken>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<CreateApiTokenResult> CreateUserTokenAsync(
    CreateApiTokenRequest request,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(request);

    return await PostAsync<CreateApiTokenResult>("user/tokens", request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<ApiToken> UpdateUserTokenAsync(
    string tokenId,
    UpdateApiTokenRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(tokenId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"user/tokens/{Uri.EscapeDataString(tokenId)}";

    return await PutAsync<ApiToken>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteUserTokenAsync(
    string tokenId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(tokenId);

    var endpoint = $"user/tokens/{Uri.EscapeDataString(tokenId)}";

    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<VerifyTokenResult> VerifyUserTokenAsync(
    CancellationToken cancellationToken = default)
  {
    return await GetAsync<VerifyTokenResult>("user/tokens/verify", cancellationToken);
  }

  /// <inheritdoc />
  public async Task<string> RollUserTokenAsync(
    string tokenId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(tokenId);

    var endpoint = $"user/tokens/{Uri.EscapeDataString(tokenId)}/value";

    // The API returns the new token value directly as a string in the result field.
    return await PutAsync<string>(endpoint, new { }, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<PagePaginatedResult<PermissionGroup>> GetUserPermissionGroupsAsync(
    ListPermissionGroupsFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    var endpoint = BuildUserPermissionGroupsQueryString(filters);

    return await GetPagePaginatedResultAsync<PermissionGroup>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<PermissionGroup> GetAllUserPermissionGroupsAsync(
    ListPermissionGroupsFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    // Note: The permission_groups endpoint does NOT support pagination.
    // All groups are returned in a single response. GetPaginatedAsync handles this gracefully.
    var endpoint = BuildUserPermissionGroupsQueryString(filters);

    return GetPaginatedAsync<PermissionGroup>(endpoint, perPage: null, cancellationToken);
  }

  #endregion


  #region Helpers

  /// <summary>
  ///   Builds the query string for the tokens list endpoint.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional filters to apply.</param>
  /// <returns>The endpoint with query string.</returns>
  private static string BuildTokensListQueryString(string accountId, ListApiTokensFilters? filters)
  {
    var queryParams = new List<string>();

    if (filters?.Page is not null)
      queryParams.Add($"page={filters.Page}");
    if (filters?.PerPage is not null)
      queryParams.Add($"per_page={filters.PerPage}");
    if (filters?.Direction is not null)
      queryParams.Add($"direction={EnumHelper.GetEnumMemberValue(filters.Direction.Value)}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;

    return $"accounts/{Uri.EscapeDataString(accountId)}/tokens{queryString}";
  }

  /// <summary>
  ///   Builds the query string for the permission groups endpoint.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional filters to apply.</param>
  /// <returns>The endpoint with query string.</returns>
  /// <remarks>
  ///   The permission_groups endpoint does NOT support pagination. All groups are returned in a single response.
  /// </remarks>
  private static string BuildPermissionGroupsQueryString(string accountId, ListPermissionGroupsFilters? filters)
  {
    var queryParams = new List<string>();

    if (filters?.Name is not null)
      queryParams.Add($"name={Uri.EscapeDataString(filters.Name)}");
    if (filters?.Scope is not null)
      queryParams.Add($"scope={Uri.EscapeDataString(filters.Scope)}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;

    return $"accounts/{Uri.EscapeDataString(accountId)}/tokens/permission_groups{queryString}";
  }

  /// <summary>
  ///   Builds the query string for the user tokens list endpoint.
  /// </summary>
  /// <param name="filters">Optional filters to apply.</param>
  /// <returns>The endpoint with query string.</returns>
  private static string BuildUserTokensListQueryString(ListApiTokensFilters? filters)
  {
    var queryParams = new List<string>();

    if (filters?.Page is not null)
      queryParams.Add($"page={filters.Page}");
    if (filters?.PerPage is not null)
      queryParams.Add($"per_page={filters.PerPage}");
    if (filters?.Direction is not null)
      queryParams.Add($"direction={EnumHelper.GetEnumMemberValue(filters.Direction.Value)}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;

    return $"user/tokens{queryString}";
  }

  /// <summary>
  ///   Builds the query string for the user permission groups endpoint.
  /// </summary>
  /// <param name="filters">Optional filters to apply.</param>
  /// <returns>The endpoint with query string.</returns>
  /// <remarks>
  ///   The permission_groups endpoint does NOT support pagination. All groups are returned in a single response.
  /// </remarks>
  private static string BuildUserPermissionGroupsQueryString(ListPermissionGroupsFilters? filters)
  {
    var queryParams = new List<string>();

    if (filters?.Name is not null)
      queryParams.Add($"name={Uri.EscapeDataString(filters.Name)}");
    if (filters?.Scope is not null)
      queryParams.Add($"scope={Uri.EscapeDataString(filters.Scope)}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;

    return $"user/tokens/permission_groups{queryString}";
  }

  #endregion
}
