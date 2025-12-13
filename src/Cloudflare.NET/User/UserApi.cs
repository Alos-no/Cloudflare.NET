namespace Cloudflare.NET.User;

using System.Runtime.CompilerServices;
using Core;
using Core.Internal;
using Core.Models;
using Microsoft.Extensions.Logging;
using Models;
using Security.Firewall.Models;

/// <summary>
///   Implements the API for managing the authenticated Cloudflare user's profile.
///   <para>
///     All operations affect only the currently authenticated user (self-only access).
///   </para>
/// </summary>
public class UserApi : ApiResource, IUserApi
{
  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="UserApi" /> class.</summary>
  /// <param name="httpClient">The HttpClient for making requests.</param>
  /// <param name="loggerFactory">The factory to create loggers for this resource.</param>
  public UserApi(HttpClient httpClient, ILoggerFactory loggerFactory)
    : base(httpClient, loggerFactory.CreateLogger<UserApi>())
  {
  }

  #endregion


  #region User Profile

  /// <inheritdoc />
  public async Task<User> GetUserAsync(CancellationToken cancellationToken = default)
  {
    return await GetAsync<User>("user", cancellationToken);
  }

  /// <inheritdoc />
  public async Task<User> EditUserAsync(
    EditUserRequest request,
    CancellationToken cancellationToken = default)
  {
    return await PatchAsync<User>("user", request, cancellationToken);
  }

  #endregion


  #region User Invitations

  /// <inheritdoc />
  public async Task<IReadOnlyList<UserInvitation>> ListInvitationsAsync(
    CancellationToken cancellationToken = default)
  {
    return await GetAsync<IReadOnlyList<UserInvitation>>("user/invites", cancellationToken);
  }


  /// <inheritdoc />
  public async Task<UserInvitation> GetInvitationAsync(
    string invitationId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(invitationId);

    var endpoint = $"user/invites/{Uri.EscapeDataString(invitationId)}";
    return await GetAsync<UserInvitation>(endpoint, cancellationToken);
  }


  /// <inheritdoc />
  public async Task<UserInvitation> RespondToInvitationAsync(
    string invitationId,
    RespondToInvitationRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(invitationId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"user/invites/{Uri.EscapeDataString(invitationId)}";
    return await PatchAsync<UserInvitation>(endpoint, request, cancellationToken);
  }

  #endregion


  #region User Memberships

  /// <inheritdoc />
  public async Task<PagePaginatedResult<Membership>> ListMembershipsAsync(
    ListMembershipsFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    var queryParams = BuildMembershipQueryParams(filters);

    var endpoint = "memberships";
    if (queryParams.Count > 0)
      endpoint += "?" + string.Join("&", queryParams);

    return await GetPagePaginatedResultAsync<Membership>(endpoint, cancellationToken);
  }


  /// <inheritdoc />
  public async IAsyncEnumerable<Membership> ListAllMembershipsAsync(
    ListMembershipsFilters? filters = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    // Start with page 1.
    var currentPage = 1;

    while (true)
    {
      // Create a new filter with the current page, preserving other filter properties.
      var pagedFilters = filters is null
        ? new ListMembershipsFilters(Page: currentPage)
        : filters with { Page = currentPage };

      var result = await ListMembershipsAsync(pagedFilters, cancellationToken);

      // Yield all items from the current page.
      foreach (var item in result.Items)
      {
        yield return item;
      }

      // Check if we've reached the last page.
      if (result.PageInfo is null || currentPage >= result.PageInfo.TotalPages)
        yield break;

      currentPage++;
    }
  }


  /// <inheritdoc />
  public async Task<Membership> GetMembershipAsync(
    string membershipId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(membershipId);

    var endpoint = $"memberships/{Uri.EscapeDataString(membershipId)}";
    return await GetAsync<Membership>(endpoint, cancellationToken);
  }


  /// <inheritdoc />
  public async Task<Membership> UpdateMembershipAsync(
    string membershipId,
    UpdateMembershipRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(membershipId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"memberships/{Uri.EscapeDataString(membershipId)}";
    return await PutAsync<Membership>(endpoint, request, cancellationToken);
  }


  /// <inheritdoc />
  public async Task DeleteMembershipAsync(
    string membershipId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(membershipId);

    var endpoint = $"memberships/{Uri.EscapeDataString(membershipId)}";
    await DeleteAsync<object>(endpoint, cancellationToken);
  }


  /// <summary>Builds query parameters for membership list requests.</summary>
  /// <param name="filters">Optional filters to apply.</param>
  /// <returns>A list of query parameter strings.</returns>
  private static List<string> BuildMembershipQueryParams(ListMembershipsFilters? filters)
  {
    var queryParams = new List<string>();

    if (filters is null)
      return queryParams;

    if (filters.Status is not null)
      queryParams.Add($"status={Uri.EscapeDataString((string)filters.Status)}");

    if (!string.IsNullOrEmpty(filters.AccountName))
      queryParams.Add($"account.name={Uri.EscapeDataString(filters.AccountName)}");

    if (filters.Order is not null)
      queryParams.Add($"order={Uri.EscapeDataString(EnumHelper.GetEnumMemberValue(filters.Order.Value))}");

    if (filters.Direction is not null)
      queryParams.Add($"direction={Uri.EscapeDataString(EnumHelper.GetEnumMemberValue(filters.Direction.Value))}");

    if (filters.Page is not null)
      queryParams.Add($"page={filters.Page}");

    if (filters.PerPage is not null)
      queryParams.Add($"per_page={filters.PerPage}");

    return queryParams;
  }

  #endregion
}
