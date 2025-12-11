namespace Cloudflare.NET.AuditLogs;

using System.Runtime.CompilerServices;
using Core;
using Core.Internal;
using Core.Models;
using Microsoft.Extensions.Logging;
using Models;


/// <summary>
///   Provides methods for interacting with the Cloudflare Audit Logs API.
///   <para>
///     Audit logs record actions taken on account resources and are retained for 30 days.
///   </para>
/// </summary>
public class AuditLogsApi(HttpClient httpClient, ILoggerFactory loggerFactory)
  : ApiResource(httpClient, loggerFactory.CreateLogger<AuditLogsApi>()), IAuditLogsApi
{
  #region Account Audit Logs

  /// <inheritdoc />
  public async Task<CursorPaginatedResult<AuditLog>> GetAccountAuditLogsAsync(
    string accountId,
    ListAuditLogsFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    // Validate arguments.
    ArgumentException.ThrowIfNullOrWhiteSpace(accountId, nameof(accountId));

    // Build the endpoint with query parameters.
    var queryParams = BuildAuditLogQueryParams(filters);
    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/logs/audit";
    if (queryParams.Count > 0)
      endpoint += "?" + string.Join("&", queryParams);

    // Execute the request.
    return await GetCursorPaginatedResultAsync<AuditLog>(endpoint, cancellationToken);
  }


  /// <inheritdoc />
  public async IAsyncEnumerable<AuditLog> GetAllAccountAuditLogsAsync(
    string accountId,
    ListAuditLogsFilters? filters = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    // Validate arguments.
    ArgumentException.ThrowIfNullOrWhiteSpace(accountId, nameof(accountId));

    // Iterate through all pages using cursor-based pagination.
    string? cursor = null;
    bool hasMore;

    do
    {
      cancellationToken.ThrowIfCancellationRequested();

      // Create filters with updated cursor.
      var currentFilters = filters is null
        ? new ListAuditLogsFilters(Cursor: cursor)
        : filters with { Cursor = cursor };

      var page = await GetAccountAuditLogsAsync(accountId, currentFilters, cancellationToken);

      foreach (var log in page.Items)
        yield return log;

      // Check if there's a next page.
      cursor = page.CursorInfo?.Cursor;
      hasMore = !string.IsNullOrEmpty(cursor);
    } while (hasMore);
  }

  #endregion


  #region User Audit Logs

  /// <inheritdoc />
  public async Task<CursorPaginatedResult<AuditLog>> ListUserAuditLogsAsync(
    ListAuditLogsFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    // Build the endpoint with query parameters.
    var queryParams = BuildAuditLogQueryParams(filters);
    var endpoint = "user/audit_logs";
    if (queryParams.Count > 0)
      endpoint += "?" + string.Join("&", queryParams);

    // Execute the request.
    return await GetCursorPaginatedResultAsync<AuditLog>(endpoint, cancellationToken);
  }


  /// <inheritdoc />
  public async IAsyncEnumerable<AuditLog> ListAllUserAuditLogsAsync(
    ListAuditLogsFilters? filters = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    // Iterate through all pages using cursor-based pagination.
    string? cursor = null;
    bool hasMore;

    do
    {
      cancellationToken.ThrowIfCancellationRequested();

      // Create filters with updated cursor.
      var currentFilters = filters is null
        ? new ListAuditLogsFilters(Cursor: cursor)
        : filters with { Cursor = cursor };

      var page = await ListUserAuditLogsAsync(currentFilters, cancellationToken);

      foreach (var log in page.Items)
        yield return log;

      // Check if there's a next page.
      cursor = page.CursorInfo?.Cursor;
      hasMore = !string.IsNullOrEmpty(cursor);
    } while (hasMore);
  }

  #endregion


  #region Helpers

  /// <summary>
  ///   Builds the query parameter list from the filter model.
  /// </summary>
  private static List<string> BuildAuditLogQueryParams(ListAuditLogsFilters? filters)
  {
    var queryParams = new List<string>();
    if (filters == null)
      return queryParams;

    // Pagination parameters.
    if (filters.Cursor != null)
      queryParams.Add($"cursor={Uri.EscapeDataString(filters.Cursor)}");
    if (filters.Limit != null)
      queryParams.Add($"limit={filters.Limit}");
    if (filters.Direction != null)
      queryParams.Add($"direction={EnumHelper.GetEnumMemberValue(filters.Direction.Value)}");

    // Time filters.
    if (filters.Before != null)
      queryParams.Add($"before={Uri.EscapeDataString(filters.Before.Value.ToString("O"))}");
    if (filters.Since != null)
      queryParams.Add($"since={Uri.EscapeDataString(filters.Since.Value.ToString("O"))}");

    // ID filters.
    AddArrayFilter(queryParams, "id", filters.Ids, filters.IdsNot);

    // Actor filters.
    AddArrayFilter(queryParams, "actor_email", filters.ActorEmails, filters.ActorEmailsNot);
    AddArrayFilter(queryParams, "actor_id", filters.ActorIds, filters.ActorIdsNot);
    AddArrayFilter(queryParams, "actor_ip_address", filters.ActorIpAddresses, filters.ActorIpAddressesNot);
    AddArrayFilter(queryParams, "actor_token_id", filters.ActorTokenIds, filters.ActorTokenIdsNot);
    AddArrayFilter(queryParams, "actor_token_name", filters.ActorTokenNames, filters.ActorTokenNamesNot);
    AddArrayFilter(queryParams, "actor_context", filters.ActorContexts, filters.ActorContextsNot);
    AddArrayFilter(queryParams, "actor_type", filters.ActorTypes, filters.ActorTypesNot);

    // Action filters.
    AddArrayFilter(queryParams, "action_type", filters.ActionTypes, filters.ActionTypesNot);
    AddArrayFilter(queryParams, "action_result", filters.ActionResults, filters.ActionResultsNot);

    // Resource filters.
    AddArrayFilter(queryParams, "resource_id", filters.ResourceIds, filters.ResourceIdsNot);
    AddArrayFilter(queryParams, "resource_product", filters.ResourceProducts, filters.ResourceProductsNot);
    AddArrayFilter(queryParams, "resource_type", filters.ResourceTypes, filters.ResourceTypesNot);
    AddArrayFilter(queryParams, "resource_scope", filters.ResourceScopes, filters.ResourceScopesNot);

    // Zone filters.
    AddArrayFilter(queryParams, "zone_id", filters.ZoneIds, filters.ZoneIdsNot);
    AddArrayFilter(queryParams, "zone_name", filters.ZoneNames, filters.ZoneNamesNot);

    // Raw request filters.
    AddArrayFilter(queryParams, "raw_cf_ray_id", filters.RawCfRayIds, filters.RawCfRayIdsNot);
    AddArrayFilter(queryParams, "raw_method", filters.RawMethods, filters.RawMethodsNot);
    AddArrayFilter(queryParams, "raw_uri", filters.RawUris, filters.RawUrisNot);

    // Account filters.
    AddArrayFilter(queryParams, "account_name", filters.AccountNames, filters.AccountNamesNot);

    // Status codes (integer arrays).
    if (filters.RawStatusCodes?.Count > 0)
      foreach (var code in filters.RawStatusCodes)
        queryParams.Add($"raw_status_code={code}");
    if (filters.RawStatusCodesNot?.Count > 0)
      foreach (var code in filters.RawStatusCodesNot)
        queryParams.Add($"raw_status_code.not={code}");

    return queryParams;
  }


  /// <summary>
  ///   Adds array filter parameters to the query string.
  /// </summary>
  /// <param name="queryParams">The list of query parameters to add to.</param>
  /// <param name="paramName">The base parameter name (without .not suffix).</param>
  /// <param name="include">Values to include (uses paramName directly).</param>
  /// <param name="exclude">Values to exclude (uses paramName.not).</param>
  private static void AddArrayFilter(
    List<string> queryParams,
    string paramName,
    IReadOnlyList<string>? include,
    IReadOnlyList<string>? exclude)
  {
    if (include?.Count > 0)
      foreach (var value in include)
        queryParams.Add($"{paramName}={Uri.EscapeDataString(value)}");
    if (exclude?.Count > 0)
      foreach (var value in exclude)
        queryParams.Add($"{paramName}.not={Uri.EscapeDataString(value)}");
  }

  #endregion
}
