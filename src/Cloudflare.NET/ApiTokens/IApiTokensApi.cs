namespace Cloudflare.NET.ApiTokens;

using Core.Models;
using Models;

/// <summary>
///   Defines the contract for API token management operations.
///   <para>
///     API tokens provide fine-grained access control for Cloudflare API operations.
///     This interface handles account-scoped tokens. User-scoped tokens will be
///     added in F17.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     Key features of API tokens:
///     <list type="bullet">
///       <item><description>Policy-based access control with allow/deny effects</description></item>
///       <item><description>IP address restrictions using CIDR notation</description></item>
///       <item><description>Time-bounded validity with not_before and expires_on</description></item>
///       <item><description>Secret rotation without recreating the token</description></item>
///     </list>
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Create a new token
///   var result = await client.ApiTokens.CreateAccountTokenAsync(accountId,
///     new CreateApiTokenRequest(
///       Name: "CI/CD Token",
///       Policies: new[]
///       {
///         new CreateTokenPolicyRequest(
///           Effect: "allow",
///           PermissionGroups: new[] { new TokenPermissionGroupReference(permissionGroupId) },
///           Resources: new Dictionary&lt;string, string&gt;
///           {
///             ["com.cloudflare.api.account.*"] = "*"
///           })
///       }));
///
///   // Store the token value securely - it cannot be retrieved again!
///   Console.WriteLine($"Token: {result.Value}");
///
///   // Later, roll the token to generate a new secret
///   var newValue = await client.ApiTokens.RollAccountTokenAsync(accountId, result.Id);
///   </code>
/// </example>
public interface IApiTokensApi
{
  #region Account Tokens

  /// <summary>
  ///   Lists all API tokens for the account.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional filtering and pagination options.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A page of API tokens.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   // Get first page of tokens
  ///   var result = await client.ApiTokens.ListAccountTokensAsync(accountId);
  ///   foreach (var token in result.Items)
  ///   {
  ///     Console.WriteLine($"{token.Name}: {token.Status}");
  ///   }
  ///   </code>
  /// </example>
  Task<PagePaginatedResult<ApiToken>> ListAccountTokensAsync(
    string accountId,
    ListApiTokensFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Lists all API tokens for the account, automatically handling pagination.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional filtering options. Pagination parameters are managed internally.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An async enumerable of all API tokens.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   // Iterate through all tokens with automatic pagination
  ///   await foreach (var token in client.ApiTokens.ListAllAccountTokensAsync(accountId))
  ///   {
  ///     Console.WriteLine($"{token.Name}: {token.Status}");
  ///   }
  ///   </code>
  /// </example>
  IAsyncEnumerable<ApiToken> ListAllAccountTokensAsync(
    string accountId,
    ListApiTokensFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Gets details for a specific API token.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="tokenId">The token identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The API token details.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> or <paramref name="tokenId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var token = await client.ApiTokens.GetAccountTokenAsync(accountId, tokenId);
  ///   Console.WriteLine($"Token: {token.Name}");
  ///   Console.WriteLine($"Status: {token.Status}");
  ///   Console.WriteLine($"Expires: {token.ExpiresOn}");
  ///   </code>
  /// </example>
  Task<ApiToken> GetAccountTokenAsync(
    string accountId,
    string tokenId,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Creates a new API token.
  ///   <para>
  ///     <b>Important:</b> The returned <see cref="CreateApiTokenResult.Value" /> contains
  ///     the token secret. Store it securely - it cannot be retrieved again.
  ///   </para>
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="request">The token creation parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The created token including its secret value.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> is null or whitespace.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request" /> is null.</exception>
  /// <example>
  ///   <code>
  ///   var result = await client.ApiTokens.CreateAccountTokenAsync(accountId,
  ///     new CreateApiTokenRequest(
  ///       Name: "CI/CD Token",
  ///       Policies: new[]
  ///       {
  ///         new CreateTokenPolicyRequest(
  ///           Effect: "allow",
  ///           PermissionGroups: new[] { new TokenPermissionGroupReference("...") },
  ///           Resources: new Dictionary&lt;string, string&gt;
  ///           {
  ///             ["com.cloudflare.api.account.*"] = "*"
  ///           })
  ///       },
  ///       ExpiresOn: DateTime.UtcNow.AddYears(1)));
  ///
  ///   Console.WriteLine($"Token: {result.Value}"); // Store securely!
  ///   </code>
  /// </example>
  Task<CreateApiTokenResult> CreateAccountTokenAsync(
    string accountId,
    CreateApiTokenRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Updates an existing API token.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="tokenId">The token identifier.</param>
  /// <param name="request">The update parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated token.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> or <paramref name="tokenId" /> is null or whitespace.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request" /> is null.</exception>
  /// <example>
  ///   <code>
  ///   // Disable a token
  ///   var updated = await client.ApiTokens.UpdateAccountTokenAsync(accountId, tokenId,
  ///     new UpdateApiTokenRequest(
  ///       Name: token.Name,
  ///       Policies: token.Policies.Select(p =&gt; new CreateTokenPolicyRequest(
  ///         p.Effect, p.PermissionGroups, p.Resources)).ToList(),
  ///       Status: TokenStatus.Disabled));
  ///   </code>
  /// </example>
  Task<ApiToken> UpdateAccountTokenAsync(
    string accountId,
    string tokenId,
    UpdateApiTokenRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Deletes an API token.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="tokenId">The token identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> or <paramref name="tokenId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   await client.ApiTokens.DeleteAccountTokenAsync(accountId, tokenId);
  ///   </code>
  /// </example>
  Task DeleteAccountTokenAsync(
    string accountId,
    string tokenId,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Verifies that the current token being used is valid and working.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>Token verification status.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var result = await client.ApiTokens.VerifyAccountTokenAsync(accountId);
  ///   Console.WriteLine($"Token {result.Id} is {result.Status}");
  ///   if (result.ExpiresOn.HasValue)
  ///     Console.WriteLine($"Expires: {result.ExpiresOn}");
  ///   </code>
  /// </example>
  Task<VerifyTokenResult> VerifyAccountTokenAsync(
    string accountId,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Rolls (rotates) a token's secret, generating a new value.
  ///   <para>
  ///     The old token value becomes invalid immediately. All clients
  ///     using the old value must be updated with the new one.
  ///   </para>
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="tokenId">The token identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The new token secret value.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> or <paramref name="tokenId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   // Rotate the token secret
  ///   var newValue = await client.ApiTokens.RollAccountTokenAsync(accountId, tokenId);
  ///   Console.WriteLine($"New token value: {newValue}"); // Store securely!
  ///   </code>
  /// </example>
  Task<string> RollAccountTokenAsync(
    string accountId,
    string tokenId,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Lists available permission groups for API tokens.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional filtering options.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A page of permission groups.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var result = await client.ApiTokens.GetAccountPermissionGroupsAsync(accountId);
  ///   foreach (var group in result.Items)
  ///   {
  ///     Console.WriteLine($"{group.Name} ({group.Id})");
  ///   }
  ///   </code>
  /// </example>
  Task<PagePaginatedResult<PermissionGroup>> GetAccountPermissionGroupsAsync(
    string accountId,
    ListPermissionGroupsFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Lists all available permission groups, automatically handling pagination.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional filtering options.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An async enumerable of all permission groups.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   // Find permission groups for a specific scope
  ///   await foreach (var group in client.ApiTokens.GetAllAccountPermissionGroupsAsync(accountId))
  ///   {
  ///     if (group.Scopes.Contains("zone:read"))
  ///       Console.WriteLine($"Found group with zone:read: {group.Name}");
  ///   }
  ///   </code>
  /// </example>
  IAsyncEnumerable<PermissionGroup> GetAllAccountPermissionGroupsAsync(
    string accountId,
    ListPermissionGroupsFilters? filters = null,
    CancellationToken cancellationToken = default);

  #endregion


  #region User Tokens

  /// <summary>
  ///   Lists all API tokens created by the authenticated user.
  /// </summary>
  /// <param name="filters">Optional filtering and pagination options.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A page of API tokens.</returns>
  /// <example>
  ///   <code>
  ///   // Get first page of user tokens
  ///   var result = await client.ApiTokens.ListUserTokensAsync();
  ///   foreach (var token in result.Items)
  ///   {
  ///     Console.WriteLine($"{token.Name}: {token.Status}");
  ///   }
  ///   </code>
  /// </example>
  Task<PagePaginatedResult<ApiToken>> ListUserTokensAsync(
    ListApiTokensFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Lists all API tokens for the authenticated user, automatically handling pagination.
  /// </summary>
  /// <param name="filters">Optional filtering options. Pagination parameters are managed internally.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An async enumerable of all API tokens.</returns>
  /// <example>
  ///   <code>
  ///   // Iterate through all tokens with automatic pagination
  ///   await foreach (var token in client.ApiTokens.ListAllUserTokensAsync())
  ///   {
  ///     Console.WriteLine($"{token.Name}: {token.Status}");
  ///   }
  ///   </code>
  /// </example>
  IAsyncEnumerable<ApiToken> ListAllUserTokensAsync(
    ListApiTokensFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Gets details for a specific API token.
  /// </summary>
  /// <param name="tokenId">The token identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The API token details.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="tokenId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var token = await client.ApiTokens.GetUserTokenAsync(tokenId);
  ///   Console.WriteLine($"Token: {token.Name}");
  ///   Console.WriteLine($"Status: {token.Status}");
  ///   Console.WriteLine($"Expires: {token.ExpiresOn}");
  ///   </code>
  /// </example>
  Task<ApiToken> GetUserTokenAsync(
    string tokenId,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Creates a new API token for the authenticated user.
  ///   <para>
  ///     <b>Important:</b> The returned <see cref="CreateApiTokenResult.Value" /> contains
  ///     the token secret. Store it securely - it cannot be retrieved again.
  ///   </para>
  /// </summary>
  /// <param name="request">The token creation parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The created token including its secret value.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request" /> is null.</exception>
  /// <example>
  ///   <code>
  ///   var result = await client.ApiTokens.CreateUserTokenAsync(
  ///     new CreateApiTokenRequest(
  ///       Name: "Personal Token",
  ///       Policies: new[]
  ///       {
  ///         new CreateTokenPolicyRequest(
  ///           Effect: "allow",
  ///           PermissionGroups: new[] { new TokenPermissionGroupReference("...") },
  ///           Resources: new Dictionary&lt;string, string&gt;
  ///           {
  ///             ["com.cloudflare.api.user"] = "*"
  ///           })
  ///       },
  ///       ExpiresOn: DateTime.UtcNow.AddYears(1)));
  ///
  ///   Console.WriteLine($"Token: {result.Value}"); // Store securely!
  ///   </code>
  /// </example>
  Task<CreateApiTokenResult> CreateUserTokenAsync(
    CreateApiTokenRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Updates an existing API token.
  /// </summary>
  /// <param name="tokenId">The token identifier.</param>
  /// <param name="request">The update parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated token.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="tokenId" /> is null or whitespace.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request" /> is null.</exception>
  /// <example>
  ///   <code>
  ///   // Disable a token
  ///   var updated = await client.ApiTokens.UpdateUserTokenAsync(tokenId,
  ///     new UpdateApiTokenRequest(
  ///       Name: token.Name,
  ///       Policies: token.Policies.Select(p =&gt; new CreateTokenPolicyRequest(
  ///         p.Effect, p.PermissionGroups, p.Resources)).ToList(),
  ///       Status: TokenStatus.Disabled));
  ///   </code>
  /// </example>
  Task<ApiToken> UpdateUserTokenAsync(
    string tokenId,
    UpdateApiTokenRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Deletes an API token.
  /// </summary>
  /// <param name="tokenId">The token identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <exception cref="ArgumentException">Thrown when <paramref name="tokenId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   await client.ApiTokens.DeleteUserTokenAsync(tokenId);
  ///   </code>
  /// </example>
  Task DeleteUserTokenAsync(
    string tokenId,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Verifies that the current token being used is valid and working.
  ///   <para>
  ///     This endpoint can be used to test whether a token is properly configured
  ///     and has the expected permissions.
  ///   </para>
  /// </summary>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>Token verification status.</returns>
  /// <example>
  ///   <code>
  ///   var result = await client.ApiTokens.VerifyUserTokenAsync();
  ///   Console.WriteLine($"Token {result.Id} is {result.Status}");
  ///   if (result.ExpiresOn.HasValue)
  ///     Console.WriteLine($"Expires: {result.ExpiresOn}");
  ///   </code>
  /// </example>
  Task<VerifyTokenResult> VerifyUserTokenAsync(
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Rolls (rotates) a token's secret, generating a new value.
  ///   <para>
  ///     The old token value becomes invalid immediately. All clients
  ///     using the old value must be updated with the new one.
  ///   </para>
  /// </summary>
  /// <param name="tokenId">The token identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The new token secret value.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="tokenId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   // Rotate the token secret
  ///   var newValue = await client.ApiTokens.RollUserTokenAsync(tokenId);
  ///   Console.WriteLine($"New token value: {newValue}"); // Store securely!
  ///   </code>
  /// </example>
  Task<string> RollUserTokenAsync(
    string tokenId,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Lists available permission groups for user API tokens.
  /// </summary>
  /// <param name="filters">Optional filtering options.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A page of permission groups.</returns>
  /// <example>
  ///   <code>
  ///   var result = await client.ApiTokens.GetUserPermissionGroupsAsync();
  ///   foreach (var group in result.Items)
  ///   {
  ///     Console.WriteLine($"{group.Name} ({group.Id})");
  ///   }
  ///   </code>
  /// </example>
  Task<PagePaginatedResult<PermissionGroup>> GetUserPermissionGroupsAsync(
    ListPermissionGroupsFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Lists all available permission groups for user tokens, automatically handling pagination.
  /// </summary>
  /// <param name="filters">Optional filtering options.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An async enumerable of all permission groups.</returns>
  /// <example>
  ///   <code>
  ///   // Find permission groups for a specific scope
  ///   await foreach (var group in client.ApiTokens.GetAllUserPermissionGroupsAsync())
  ///   {
  ///     if (group.Scopes.Contains("zone:read"))
  ///       Console.WriteLine($"Found group with zone:read: {group.Name}");
  ///   }
  ///   </code>
  /// </example>
  IAsyncEnumerable<PermissionGroup> GetAllUserPermissionGroupsAsync(
    ListPermissionGroupsFilters? filters = null,
    CancellationToken cancellationToken = default);

  #endregion
}
