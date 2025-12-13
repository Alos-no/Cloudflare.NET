namespace Cloudflare.NET.Turnstile;

using Core.Models;
using Models;


/// <summary>
///   Provides access to Cloudflare Turnstile API operations.
///   <para>
///     Turnstile is Cloudflare's CAPTCHA alternative that provides bot protection
///     without user friction. Widgets are account-scoped and can be configured
///     with different modes (invisible, managed, non-interactive).
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     Turnstile is a separate Cloudflare product from the core API, with its
///     own dedicated interface following the Interface Segregation Principle.
///   </para>
///   <para>
///     <b>Important:</b> Widget secrets are only returned on creation and rotation.
///     Store them securely as they cannot be retrieved again.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Create a widget
///   var widget = await client.Turnstile.CreateWidgetAsync(accountId,
///     new CreateTurnstileWidgetRequest(
///       Name: "Login Widget",
///       Domains: new[] { "example.com", "www.example.com" },
///       Mode: WidgetMode.Managed));
///
///   // Store the secret securely - it won't be available again!
///   var secret = widget.Secret;
///
///   // Later, rotate the secret
///   var result = await client.Turnstile.RotateSecretAsync(accountId, widget.Sitekey);
///   var newSecret = result.Secret;
///   </code>
/// </example>
public interface ITurnstileApi
{
  #region Widget Management

  /// <summary>
  ///   Lists all Turnstile widgets for the account.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional filtering and pagination options.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A paginated result containing widgets.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var result = await client.Turnstile.ListWidgetsAsync(accountId,
  ///     new ListTurnstileWidgetsFilters(
  ///       Order: TurnstileOrderField.CreatedOn,
  ///       Direction: ListOrderDirection.Desc));
  ///
  ///   foreach (var widget in result.Result)
  ///   {
  ///     Console.WriteLine($"{widget.Name}: {widget.Sitekey}");
  ///   }
  ///   </code>
  /// </example>
  Task<PagePaginatedResult<TurnstileWidget>> ListWidgetsAsync(
    string accountId,
    ListTurnstileWidgetsFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Lists all Turnstile widgets, automatically handling pagination.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional filtering options (Page is ignored).</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An async enumerable of all widgets.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   await foreach (var widget in client.Turnstile.ListAllWidgetsAsync(accountId))
  ///   {
  ///     Console.WriteLine($"{widget.Name}: {widget.Mode}");
  ///   }
  ///   </code>
  /// </example>
  IAsyncEnumerable<TurnstileWidget> ListAllWidgetsAsync(
    string accountId,
    ListTurnstileWidgetsFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Gets details for a specific Turnstile widget.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="sitekey">The widget sitekey (public key).</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The widget details (without the secret).</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> or <paramref name="sitekey" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var widget = await client.Turnstile.GetWidgetAsync(accountId, sitekey);
  ///   Console.WriteLine($"Mode: {widget.Mode}, Domains: {string.Join(", ", widget.Domains)}");
  ///   </code>
  /// </example>
  Task<TurnstileWidget> GetWidgetAsync(
    string accountId,
    string sitekey,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Creates a new Turnstile widget.
  ///   <para>
  ///     The response includes the secret key which is only returned at creation time.
  ///     Store it securely as it cannot be retrieved again.
  ///   </para>
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="request">The widget creation parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The created widget including the secret key.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> is null or whitespace.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request" /> is null.</exception>
  /// <example>
  ///   <code>
  ///   var widget = await client.Turnstile.CreateWidgetAsync(accountId,
  ///     new CreateTurnstileWidgetRequest(
  ///       Name: "Contact Form",
  ///       Domains: new[] { "example.com" },
  ///       Mode: WidgetMode.Invisible,
  ///       BotFightMode: true));
  ///
  ///   // IMPORTANT: Store the secret securely!
  ///   SaveSecurely(widget.Secret);
  ///   </code>
  /// </example>
  Task<TurnstileWidget> CreateWidgetAsync(
    string accountId,
    CreateTurnstileWidgetRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Updates an existing Turnstile widget.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="sitekey">The widget sitekey.</param>
  /// <param name="request">The update parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated widget.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> or <paramref name="sitekey" /> is null or whitespace.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request" /> is null.</exception>
  /// <example>
  ///   <code>
  ///   var updated = await client.Turnstile.UpdateWidgetAsync(accountId, sitekey,
  ///     new UpdateTurnstileWidgetRequest(
  ///       Name: "Updated Name",
  ///       Domains: new[] { "example.com", "api.example.com" },
  ///       Mode: WidgetMode.Managed));
  ///   </code>
  /// </example>
  Task<TurnstileWidget> UpdateWidgetAsync(
    string accountId,
    string sitekey,
    UpdateTurnstileWidgetRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Deletes a Turnstile widget.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="sitekey">The widget sitekey.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> or <paramref name="sitekey" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   await client.Turnstile.DeleteWidgetAsync(accountId, sitekey);
  ///   Console.WriteLine($"Deleted widget: {sitekey}");
  ///   </code>
  /// </example>
  Task DeleteWidgetAsync(
    string accountId,
    string sitekey,
    CancellationToken cancellationToken = default);

  #endregion


  #region Secret Rotation

  /// <summary>
  ///   Rotates a widget's secret key.
  ///   <para>
  ///     By default, the old secret remains valid for 2 hours to allow
  ///     graceful migration. Set <paramref name="invalidateImmediately" /> to true
  ///     to revoke the old secret immediately.
  ///   </para>
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="sitekey">The widget sitekey.</param>
  /// <param name="invalidateImmediately">
  ///   If true, invalidates the old secret immediately.
  ///   If false (default), old secret remains valid for 2 hours.
  /// </param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The result containing the new secret key.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId" /> or <paramref name="sitekey" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   // Rotate with 2-hour grace period (default)
  ///   var result = await client.Turnstile.RotateSecretAsync(accountId, sitekey);
  ///
  ///   // Or invalidate old secret immediately
  ///   var result = await client.Turnstile.RotateSecretAsync(accountId, sitekey, invalidateImmediately: true);
  ///
  ///   // Store the new secret securely
  ///   UpdateStoredSecret(result.Secret);
  ///   </code>
  /// </example>
  Task<RotateWidgetSecretResult> RotateSecretAsync(
    string accountId,
    string sitekey,
    bool invalidateImmediately = false,
    CancellationToken cancellationToken = default);

  #endregion
}
