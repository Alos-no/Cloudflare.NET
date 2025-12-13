namespace Cloudflare.NET.Roles;

using Core.Models;
using Models;


/// <summary>
///   Provides access to Cloudflare Account Roles API.
///   <para>
///     Roles are predefined by Cloudflare and define sets of permissions
///     that can be assigned to account members. This API is read-only;
///     roles cannot be created, modified, or deleted via the API.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     Common roles include "Administrator", "Administrator Read Only",
///     "DNS Administrator", "Audit Log Viewer", and "Billing".
///     Actual available roles depend on account type and plan.
///   </para>
///   <para>
///     Role IDs from this API are used when assigning roles to account members
///     via the <c>IMembersApi</c>.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // List all available roles
///   await foreach (var role in client.Roles.ListAllAccountRolesAsync(accountId))
///   {
///     Console.WriteLine($"{role.Name}: {role.Description}");
///
///     // Check specific permissions
///     if (role.Permissions.Dns?.Write == true)
///       Console.WriteLine("  - Can modify DNS");
///   }
///   </code>
/// </example>
public interface IRolesApi
{
  #region Account Roles

  /// <summary>
  ///   Lists all roles available in the account.
  ///   <para>
  ///     Roles are predefined by Cloudflare and define sets of permissions
  ///     that can be assigned to account members.
  ///   </para>
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional pagination options.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A page of account roles.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="accountId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var roles = await client.Roles.ListAccountRolesAsync(accountId);
  ///
  ///   foreach (var role in roles.Items)
  ///   {
  ///     Console.WriteLine($"{role.Name}: {role.Description}");
  ///     if (role.Permissions.Dns?.Write == true)
  ///       Console.WriteLine("  - Can modify DNS");
  ///   }
  ///   </code>
  /// </example>
  Task<PagePaginatedResult<AccountRole>> ListAccountRolesAsync(
    string accountId,
    ListAccountRolesFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Lists all roles available in the account, automatically handling pagination.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional pagination options. Pagination parameters are ignored.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An async enumerable of all account roles.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="accountId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> is empty or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   // Iterate through all roles without manual pagination
  ///   await foreach (var role in client.Roles.ListAllAccountRolesAsync(accountId))
  ///   {
  ///     Console.WriteLine($"{role.Id}: {role.Name}");
  ///   }
  ///   </code>
  /// </example>
  IAsyncEnumerable<AccountRole> ListAllAccountRolesAsync(
    string accountId,
    ListAccountRolesFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Gets details for a specific role.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="roleId">The role identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The role details including permissions.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="accountId"/> or <paramref name="roleId"/> is null.</exception>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> or <paramref name="roleId"/> is empty or whitespace.</exception>
  /// <exception cref="HttpRequestException">Thrown when the role is not found (HTTP 404).</exception>
  /// <example>
  ///   <code>
  ///   var role = await client.Roles.GetAccountRoleAsync(accountId, roleId);
  ///   Console.WriteLine($"Role: {role.Name}");
  ///   Console.WriteLine($"Description: {role.Description}");
  ///
  ///   // Check DNS permissions
  ///   if (role.Permissions.DnsRecords is { Read: true, Write: true })
  ///     Console.WriteLine("Has full DNS access");
  ///   </code>
  /// </example>
  Task<AccountRole> GetAccountRoleAsync(
    string accountId,
    string roleId,
    CancellationToken cancellationToken = default);

  #endregion
}
