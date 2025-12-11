namespace Cloudflare.NET.Members;

using Core;
using Core.Internal;
using Core.Models;
using Microsoft.Extensions.Logging;
using Models;


/// <summary>
///   Implementation of <see cref="IMembersApi"/> for Cloudflare Account Members.
///   <para>
///     Provides CRUD operations for managing account members, including
///     inviting new users, updating roles, and removing access.
///   </para>
/// </summary>
public class MembersApi : ApiResource, IMembersApi
{
  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="MembersApi"/> class.
  /// </summary>
  /// <param name="httpClient">The HttpClient for making requests.</param>
  /// <param name="loggerFactory">The factory to create loggers for this resource.</param>
  public MembersApi(HttpClient httpClient, ILoggerFactory loggerFactory)
    : base(httpClient, loggerFactory.CreateLogger<MembersApi>())
  {
  }

  #endregion


  #region Account Members

  /// <inheritdoc />
  public async Task<PagePaginatedResult<AccountMember>> ListAccountMembersAsync(
    string accountId,
    ListAccountMembersFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);

    var endpoint = BuildMembersListQueryString(accountId, filters);

    return await GetPagePaginatedResultAsync<AccountMember>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<AccountMember> ListAllAccountMembersAsync(
    string accountId,
    ListAccountMembersFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);

    // Build base endpoint without pagination parameters (handled by GetPaginatedAsync).
    var baseFilters = filters is not null ? filters with { Page = null, PerPage = null } : null;
    var endpoint = BuildMembersListQueryString(accountId, baseFilters);

    return GetPaginatedAsync<AccountMember>(endpoint, filters?.PerPage, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<AccountMember> GetAccountMemberAsync(
    string accountId,
    string memberId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(memberId);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/members/{Uri.EscapeDataString(memberId)}";

    return await GetAsync<AccountMember>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<AccountMember> CreateAccountMemberAsync(
    string accountId,
    CreateAccountMemberRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/members";

    return await PostAsync<AccountMember>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<AccountMember> UpdateAccountMemberAsync(
    string accountId,
    string memberId,
    UpdateAccountMemberRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(memberId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/members/{Uri.EscapeDataString(memberId)}";

    return await PutAsync<AccountMember>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<DeleteAccountMemberResult> DeleteAccountMemberAsync(
    string accountId,
    string memberId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(memberId);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/members/{Uri.EscapeDataString(memberId)}";

    return await DeleteAsync<DeleteAccountMemberResult>(endpoint, cancellationToken);
  }

  #endregion


  #region Helpers

  /// <summary>
  ///   Builds the query string for the members list endpoint.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional filters to apply.</param>
  /// <returns>The endpoint with query string.</returns>
  private static string BuildMembersListQueryString(string accountId, ListAccountMembersFilters? filters)
  {
    var queryParams = new List<string>();

    if (filters?.Status is not null)
      queryParams.Add($"status={Uri.EscapeDataString(filters.Status.Value.Value)}");
    if (filters?.Page is not null)
      queryParams.Add($"page={filters.Page}");
    if (filters?.PerPage is not null)
      queryParams.Add($"per_page={filters.PerPage}");
    if (filters?.Direction is not null)
      queryParams.Add($"direction={EnumHelper.GetEnumMemberValue(filters.Direction.Value)}");
    if (filters?.Order is not null)
      queryParams.Add($"order={EnumHelper.GetEnumMemberValue(filters.Order.Value)}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;

    return $"accounts/{Uri.EscapeDataString(accountId)}/members{queryString}";
  }

  #endregion
}
