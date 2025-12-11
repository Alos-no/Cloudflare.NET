namespace Cloudflare.NET.Subscriptions;

using Cloudflare.NET.Core.Exceptions;
using Cloudflare.NET.Subscriptions.Models;


/// <summary>
///   Provides access to Cloudflare Subscriptions API operations.
///   <para>
///     This interface provides unified access to subscription management across
///     account, user, and zone scopes. Subscriptions manage billing plans and
///     add-ons for Cloudflare services.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     <b>Billing Permissions Required:</b> Subscriptions API requires tokens with
///     Billing Read and/or Billing Write permissions.
///   </para>
///   <para>
///     <b>Cost Warning:</b> Creating or updating subscriptions may incur charges.
///     Use test/sandbox accounts for development.
///   </para>
///   <para>
///     <b>Externally Managed Subscriptions:</b> Some subscriptions are managed
///     outside of Cloudflare (e.g., through partners) and cannot be modified via API.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // List account subscriptions
///   var subscriptions = await cf.Subscriptions.ListAccountSubscriptionsAsync(accountId);
///   foreach (var sub in subscriptions)
///   {
///     Console.WriteLine($"{sub.RatePlan?.PublicName}: {sub.State}");
///   }
///
///   // Create a new subscription
///   var newSub = await cf.Subscriptions.CreateAccountSubscriptionAsync(accountId,
///     new CreateAccountSubscriptionRequest(
///       RatePlan: new RatePlanReference("rate_plan_id"),
///       Frequency: SubscriptionFrequency.Monthly));
///   </code>
/// </example>
public interface ISubscriptionsApi
{
  #region Account Subscriptions

  /// <summary>
  ///   Lists all subscriptions for an account.
  ///   <para>
  ///     Returns all active and inactive subscriptions associated with the account.
  ///     Requires Billing Read permission.
  ///   </para>
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>All subscriptions for the account.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> is null or whitespace.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API request fails (403 if no billing permission).</exception>
  /// <example>
  ///   <code>
  ///   var subscriptions = await cf.Subscriptions.ListAccountSubscriptionsAsync(accountId);
  ///   foreach (var sub in subscriptions)
  ///   {
  ///     Console.WriteLine($"{sub.RatePlan?.PublicName}: {sub.Price} {sub.Currency}");
  ///     Console.WriteLine($"  State: {sub.State}, Frequency: {sub.Frequency}");
  ///   }
  ///   </code>
  /// </example>
  Task<IReadOnlyList<Subscription>> ListAccountSubscriptionsAsync(
    string accountId,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Creates a new subscription for an account.
  ///   <para>
  ///     <b>Warning:</b> Creating subscriptions may incur billing charges.
  ///     Requires Billing Write permission.
  ///   </para>
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="request">The subscription creation parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The created subscription.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> is null or whitespace.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API request fails (403 if no billing permission).</exception>
  /// <exception cref="CloudflareApiException">Thrown when the rate plan is invalid or subscription cannot be created.</exception>
  /// <example>
  ///   <code>
  ///   var request = new CreateAccountSubscriptionRequest(
  ///     RatePlan: new RatePlanReference("enterprise_plan_id"),
  ///     Frequency: SubscriptionFrequency.Yearly);
  ///
  ///   var subscription = await cf.Subscriptions.CreateAccountSubscriptionAsync(accountId, request);
  ///   Console.WriteLine($"Created: {subscription.Id}");
  ///   </code>
  /// </example>
  Task<Subscription> CreateAccountSubscriptionAsync(
    string accountId,
    CreateAccountSubscriptionRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Updates an existing account subscription.
  ///   <para>
  ///     <b>Warning:</b> Updating subscriptions may affect billing.
  ///     Requires Billing Write permission.
  ///   </para>
  ///   <para>
  ///     <b>Note:</b> Externally managed subscriptions cannot be updated via API.
  ///   </para>
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="subscriptionId">The subscription identifier to update.</param>
  /// <param name="request">The update parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated subscription.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> or <paramref name="subscriptionId"/> is null or whitespace.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API request fails (404 if not found, 403 if no permission).</exception>
  /// <exception cref="CloudflareApiException">Thrown when the subscription is externally managed or update fails.</exception>
  /// <example>
  ///   <code>
  ///   // Change to yearly billing
  ///   var request = new UpdateAccountSubscriptionRequest(
  ///     Frequency: SubscriptionFrequency.Yearly);
  ///
  ///   var updated = await cf.Subscriptions.UpdateAccountSubscriptionAsync(
  ///     accountId, subscriptionId, request);
  ///   </code>
  /// </example>
  Task<Subscription> UpdateAccountSubscriptionAsync(
    string accountId,
    string subscriptionId,
    UpdateAccountSubscriptionRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Deletes an account subscription.
  ///   <para>
  ///     <b>Warning:</b> Deleting a subscription will cancel the associated plan.
  ///     This action may be irreversible.
  ///   </para>
  ///   <para>
  ///     <b>Note:</b> Externally managed subscriptions cannot be deleted via API.
  ///   </para>
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="subscriptionId">The subscription identifier to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <exception cref="ArgumentException">Thrown when <paramref name="accountId"/> or <paramref name="subscriptionId"/> is null or whitespace.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API request fails (404 if not found, 403 if no permission).</exception>
  /// <exception cref="CloudflareApiException">Thrown when the subscription is externally managed or deletion fails.</exception>
  /// <example>
  ///   <code>
  ///   await cf.Subscriptions.DeleteAccountSubscriptionAsync(accountId, subscriptionId);
  ///   Console.WriteLine("Subscription cancelled");
  ///   </code>
  /// </example>
  Task DeleteAccountSubscriptionAsync(
    string accountId,
    string subscriptionId,
    CancellationToken cancellationToken = default);

  #endregion


  #region User Subscriptions

  /// <summary>
  ///   Lists all subscriptions for the authenticated user.
  ///   <para>
  ///     Returns all active and inactive subscriptions owned by the user.
  ///     Requires Billing Read permission.
  ///   </para>
  /// </summary>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>All subscriptions for the authenticated user.</returns>
  /// <exception cref="HttpRequestException">Thrown when the API request fails (403 if no billing permission).</exception>
  /// <example>
  ///   <code>
  ///   var subscriptions = await cf.Subscriptions.ListUserSubscriptionsAsync();
  ///   foreach (var sub in subscriptions)
  ///   {
  ///     Console.WriteLine($"{sub.RatePlan?.PublicName}: {sub.State}");
  ///   }
  ///   </code>
  /// </example>
  Task<IReadOnlyList<Subscription>> ListUserSubscriptionsAsync(
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Updates an existing user subscription.
  ///   <para>
  ///     <b>Warning:</b> Updating subscriptions may affect billing.
  ///     Requires Billing Write permission.
  ///   </para>
  ///   <para>
  ///     <b>Note:</b> Externally managed subscriptions cannot be updated via API.
  ///   </para>
  /// </summary>
  /// <param name="subscriptionId">The subscription identifier to update.</param>
  /// <param name="request">The update parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated subscription.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="subscriptionId"/> is null or whitespace.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API request fails (404 if not found, 403 if no permission).</exception>
  /// <exception cref="CloudflareApiException">Thrown when the subscription is externally managed or update fails.</exception>
  /// <example>
  ///   <code>
  ///   // Change to yearly billing
  ///   var request = new UpdateUserSubscriptionRequest(
  ///     Frequency: SubscriptionFrequency.Yearly);
  ///
  ///   var updated = await cf.Subscriptions.UpdateUserSubscriptionAsync(subscriptionId, request);
  ///   </code>
  /// </example>
  Task<Subscription> UpdateUserSubscriptionAsync(
    string subscriptionId,
    UpdateUserSubscriptionRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Deletes a user subscription.
  ///   <para>
  ///     <b>Warning:</b> Deleting a subscription will cancel the associated plan.
  ///     This action may be irreversible.
  ///   </para>
  ///   <para>
  ///     <b>Note:</b> Externally managed subscriptions cannot be deleted via API.
  ///   </para>
  /// </summary>
  /// <param name="subscriptionId">The subscription identifier to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>Result containing the deleted subscription ID.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="subscriptionId"/> is null or whitespace.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API request fails (404 if not found, 403 if no permission).</exception>
  /// <exception cref="CloudflareApiException">Thrown when the subscription is externally managed or deletion fails.</exception>
  /// <example>
  ///   <code>
  ///   var result = await cf.Subscriptions.DeleteUserSubscriptionAsync(subscriptionId);
  ///   Console.WriteLine($"Deleted subscription: {result.SubscriptionId}");
  ///   </code>
  /// </example>
  Task<DeleteUserSubscriptionResult> DeleteUserSubscriptionAsync(
    string subscriptionId,
    CancellationToken cancellationToken = default);

  #endregion


  #region Zone Subscriptions

  /// <summary>
  ///   Gets the subscription details for a zone.
  ///   <para>
  ///     Returns the current subscription including the rate plan, state, and billing details.
  ///     Zones always have a subscription (at minimum, a Free plan).
  ///   </para>
  /// </summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The zone subscription details.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> is null or whitespace.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API request fails (404 if zone not found, 403 if no billing permission).</exception>
  /// <example>
  ///   <code>
  ///   var subscription = await cf.Subscriptions.GetZoneSubscriptionAsync(zoneId);
  ///   Console.WriteLine($"Current plan: {subscription.RatePlan?.PublicName}");
  ///   Console.WriteLine($"State: {subscription.State}");
  ///   </code>
  /// </example>
  Task<Subscription> GetZoneSubscriptionAsync(
    string zoneId,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Creates a zone subscription (upgrades the zone plan).
  ///   <para>
  ///     Use this to upgrade a zone from Free to a paid plan (Pro, Business, Enterprise).
  ///   </para>
  ///   <para>
  ///     <b>Warning:</b> Creating subscriptions will incur billing charges for paid plans.
  ///   </para>
  /// </summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="request">The subscription creation parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The created subscription.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> is null or whitespace.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API request fails (404 if zone not found, 403 if no billing permission).</exception>
  /// <exception cref="CloudflareApiException">Thrown when the rate plan is invalid or subscription cannot be created.</exception>
  /// <example>
  ///   <code>
  ///   // Upgrade to Pro plan
  ///   var subscription = await cf.Subscriptions.CreateZoneSubscriptionAsync(zoneId,
  ///     new CreateZoneSubscriptionRequest(
  ///       RatePlan: new RatePlanReference("pro"),
  ///       Frequency: SubscriptionFrequency.Monthly));
  ///   </code>
  /// </example>
  Task<Subscription> CreateZoneSubscriptionAsync(
    string zoneId,
    CreateZoneSubscriptionRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Updates a zone subscription.
  ///   <para>
  ///     Use this to change the plan, billing frequency, or component values.
  ///     Can be used to upgrade, downgrade, or modify an existing subscription.
  ///   </para>
  ///   <para>
  ///     <b>Warning:</b> Updating subscriptions may affect billing.
  ///   </para>
  /// </summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="request">The update parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated subscription.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> is null or whitespace.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API request fails (404 if zone not found, 403 if no billing permission).</exception>
  /// <exception cref="CloudflareApiException">Thrown when the rate plan is invalid or update fails.</exception>
  /// <example>
  ///   <code>
  ///   // Downgrade to Pro from Business
  ///   var subscription = await cf.Subscriptions.UpdateZoneSubscriptionAsync(zoneId,
  ///     new UpdateZoneSubscriptionRequest(
  ///       RatePlan: new RatePlanReference("pro")));
  ///   </code>
  /// </example>
  Task<Subscription> UpdateZoneSubscriptionAsync(
    string zoneId,
    UpdateZoneSubscriptionRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Lists all rate plans available for a zone.
  ///   <para>
  ///     Returns the plans the zone can subscribe to, including pricing information.
  ///     Use this to discover valid rate plan IDs before creating or updating subscriptions.
  ///   </para>
  /// </summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>Available rate plans for the zone.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId"/> is null or whitespace.</exception>
  /// <exception cref="HttpRequestException">Thrown when the API request fails (404 if zone not found).</exception>
  /// <example>
  ///   <code>
  ///   var plans = await cf.Subscriptions.ListAvailableRatePlansAsync(zoneId);
  ///   foreach (var plan in plans)
  ///   {
  ///     Console.WriteLine($"{plan.Name}: {plan.Currency} {plan.Frequency}");
  ///   }
  ///   </code>
  /// </example>
  Task<IReadOnlyList<ZoneRatePlan>> ListAvailableRatePlansAsync(
    string zoneId,
    CancellationToken cancellationToken = default);

  #endregion
}
