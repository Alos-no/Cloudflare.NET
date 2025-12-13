namespace Cloudflare.NET.AuditLogs;

using Core.Models;
using Models;


/// <summary>
///   Defines the contract for audit log operations.
///   <para>
///     Audit logs provide a record of actions taken on account and user resources.
///     Logs are retained for 30 days. This interface handles both account-scoped
///     and user-scoped audit logs.
///   </para>
/// </summary>
public interface IAuditLogsApi
{
  #region Account Audit Logs

  /// <summary>
  ///   Gets audit logs for the account.
  ///   <para>
  ///     <b>Note:</b> Audit logs are only available for the past 30 days.
  ///     This is a beta API and error handling may be incomplete.
  ///   </para>
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional filtering and pagination options.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A page of audit logs with cursor information.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> is null or whitespace.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API returns an HTTP error status code.</exception>
  /// <exception cref="Core.Exceptions.CloudflareApiException">Thrown when the API returns a non-success response.</exception>
  /// <example>
  ///   <code>
  ///   // Get recent audit logs
  ///   var logs = await client.AuditLogs.GetAccountAuditLogsAsync(accountId,
  ///     new ListAuditLogsFilters(
  ///       Since: DateTime.UtcNow.AddDays(-7),
  ///       Limit: 100));
  ///
  ///   foreach (var log in logs.Items)
  ///   {
  ///     Console.WriteLine($"{log.Action.Time}: {log.Action.Type} by {log.Actor.Email}");
  ///   }
  ///   </code>
  /// </example>
  Task<CursorPaginatedResult<AuditLog>> GetAccountAuditLogsAsync(
    string accountId,
    ListAuditLogsFilters? filters = null,
    CancellationToken cancellationToken = default);


  /// <summary>
  ///   Gets all audit logs for the account, automatically handling pagination.
  ///   <para>
  ///     <b>Note:</b> Audit logs are only available for the past 30 days.
  ///     Consider using time filters to limit results for large accounts.
  ///   </para>
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional filtering options. Cursor is ignored.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An async enumerable of all matching audit logs.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> is null or whitespace.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API returns an HTTP error status code.</exception>
  /// <exception cref="Core.Exceptions.CloudflareApiException">Thrown when the API returns a non-success response.</exception>
  /// <example>
  ///   <code>
  ///   // Get all failed actions in the last week
  ///   var filters = new ListAuditLogsFilters(
  ///     Since: DateTime.UtcNow.AddDays(-7),
  ///     ActionResults: new[] { "failure" });
  ///
  ///   await foreach (var log in client.AuditLogs.GetAllAccountAuditLogsAsync(accountId, filters))
  ///   {
  ///     Console.WriteLine($"Failed: {log.Action.Type} - {log.Action.Description}");
  ///   }
  ///   </code>
  /// </example>
  IAsyncEnumerable<AuditLog> GetAllAccountAuditLogsAsync(
    string accountId,
    ListAuditLogsFilters? filters = null,
    CancellationToken cancellationToken = default);

  #endregion


  #region User Audit Logs

  /// <summary>
  ///   Gets audit logs for actions taken by the authenticated user.
  ///   <para>
  ///     Audit logs are only available for the past 30 days.
  ///     Shows actions taken by the user across all their accounts.
  ///   </para>
  /// </summary>
  /// <param name="filters">Optional filtering and pagination options.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A page of audit logs with cursor information.</returns>
  /// <exception cref="HttpRequestException">Thrown when the API returns an HTTP error status code.</exception>
  /// <exception cref="Core.Exceptions.CloudflareApiException">Thrown when the API returns a non-success response.</exception>
  /// <example>
  ///   <code>
  ///   // Get recent user audit logs
  ///   var logs = await client.AuditLogs.ListUserAuditLogsAsync(
  ///     new ListAuditLogsFilters(
  ///       Since: DateTime.UtcNow.AddDays(-7),
  ///       Limit: 100));
  ///
  ///   foreach (var log in logs.Items)
  ///   {
  ///     Console.WriteLine($"{log.Action.Time}: {log.Action.Type} by {log.Actor.Email}");
  ///   }
  ///   </code>
  /// </example>
  Task<CursorPaginatedResult<AuditLog>> ListUserAuditLogsAsync(
    ListAuditLogsFilters? filters = null,
    CancellationToken cancellationToken = default);


  /// <summary>
  ///   Gets all audit logs for the authenticated user, automatically handling pagination.
  ///   <para>
  ///     Audit logs are only available for the past 30 days.
  ///     Consider using time filters to limit results for active users.
  ///   </para>
  /// </summary>
  /// <param name="filters">Optional filtering options. Cursor is ignored.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An async enumerable of all matching user audit logs.</returns>
  /// <exception cref="HttpRequestException">Thrown when the API returns an HTTP error status code.</exception>
  /// <exception cref="Core.Exceptions.CloudflareApiException">Thrown when the API returns a non-success response.</exception>
  /// <example>
  ///   <code>
  ///   // Get all failed actions in the last week
  ///   var filters = new ListAuditLogsFilters(
  ///     Since: DateTime.UtcNow.AddDays(-7),
  ///     ActionResults: new[] { "failure" });
  ///
  ///   await foreach (var log in client.AuditLogs.ListAllUserAuditLogsAsync(filters))
  ///   {
  ///     Console.WriteLine($"Failed: {log.Action.Type} - {log.Action.Description}");
  ///   }
  ///   </code>
  /// </example>
  IAsyncEnumerable<AuditLog> ListAllUserAuditLogsAsync(
    ListAuditLogsFilters? filters = null,
    CancellationToken cancellationToken = default);

  #endregion
}
