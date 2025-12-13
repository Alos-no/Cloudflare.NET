namespace Cloudflare.NET.Subscriptions;

using Core;
using Core.Internal;
using Microsoft.Extensions.Logging;
using Models;


/// <summary>
///   Implementation of <see cref="ISubscriptionsApi"/> for Cloudflare Subscriptions.
///   <para>
///     Provides operations for managing subscriptions at account, user, and zone levels.
///     Subscriptions control billing plans and add-ons for Cloudflare services.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     <b>Billing Permissions Required:</b> All operations require appropriate Billing permissions.
///   </para>
///   <para>
///     <b>Cost Warning:</b> Creating or updating subscriptions may incur charges.
///   </para>
/// </remarks>
public class SubscriptionsApi : ApiResource, ISubscriptionsApi
{
  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="SubscriptionsApi"/> class.
  /// </summary>
  /// <param name="httpClient">The HttpClient for making requests.</param>
  /// <param name="loggerFactory">The factory to create loggers for this resource.</param>
  public SubscriptionsApi(HttpClient httpClient, ILoggerFactory loggerFactory)
    : base(httpClient, loggerFactory.CreateLogger<SubscriptionsApi>())
  {
  }

  #endregion


  #region Account Subscriptions

  /// <inheritdoc />
  public async Task<IReadOnlyList<Subscription>> ListAccountSubscriptionsAsync(
    string accountId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/subscriptions";

    return await GetAsync<IReadOnlyList<Subscription>>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Subscription> CreateAccountSubscriptionAsync(
    string accountId,
    CreateAccountSubscriptionRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/subscriptions";

    return await PostAsync<Subscription>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Subscription> UpdateAccountSubscriptionAsync(
    string accountId,
    string subscriptionId,
    UpdateAccountSubscriptionRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(subscriptionId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/subscriptions/{Uri.EscapeDataString(subscriptionId)}";

    return await PutAsync<Subscription>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteAccountSubscriptionAsync(
    string accountId,
    string subscriptionId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(subscriptionId);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/subscriptions/{Uri.EscapeDataString(subscriptionId)}";

    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  #endregion


  #region User Subscriptions

  /// <inheritdoc />
  public async Task<IReadOnlyList<Subscription>> ListUserSubscriptionsAsync(
    CancellationToken cancellationToken = default)
  {
    return await GetAsync<IReadOnlyList<Subscription>>("user/subscriptions", cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Subscription> UpdateUserSubscriptionAsync(
    string subscriptionId,
    UpdateUserSubscriptionRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(subscriptionId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"user/subscriptions/{Uri.EscapeDataString(subscriptionId)}";

    return await PutAsync<Subscription>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<DeleteUserSubscriptionResult> DeleteUserSubscriptionAsync(
    string subscriptionId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(subscriptionId);

    var endpoint = $"user/subscriptions/{Uri.EscapeDataString(subscriptionId)}";

    return await DeleteAsync<DeleteUserSubscriptionResult>(endpoint, cancellationToken);
  }

  #endregion


  #region Zone Subscriptions

  /// <inheritdoc />
  public async Task<Subscription> GetZoneSubscriptionAsync(
    string zoneId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(zoneId);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/subscription";

    return await GetAsync<Subscription>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Subscription> CreateZoneSubscriptionAsync(
    string zoneId,
    CreateZoneSubscriptionRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(zoneId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/subscription";

    return await PostAsync<Subscription>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Subscription> UpdateZoneSubscriptionAsync(
    string zoneId,
    UpdateZoneSubscriptionRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(zoneId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/subscription";

    return await PutAsync<Subscription>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<ZoneRatePlan>> ListAvailableRatePlansAsync(
    string zoneId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(zoneId);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/available_rate_plans";

    return await GetAsync<IReadOnlyList<ZoneRatePlan>>(endpoint, cancellationToken);
  }

  #endregion
}
