namespace Cloudflare.NET.Turnstile;

using Core;
using Core.Internal;
using Core.Models;
using Microsoft.Extensions.Logging;
using Models;


/// <summary>
///   Implementation of <see cref="ITurnstileApi"/> for Cloudflare Turnstile widgets.
///   <para>
///     Provides CRUD operations for managing Turnstile CAPTCHA widgets,
///     including secret rotation for credential management.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     <b>Important:</b> Widget secrets are only returned on creation and rotation.
///     Store them securely as they cannot be retrieved again.
///   </para>
/// </remarks>
public class TurnstileApi : ApiResource, ITurnstileApi
{
  #region Constructors

  /// <summary>
  ///   Initializes a new instance of the <see cref="TurnstileApi"/> class.
  /// </summary>
  /// <param name="httpClient">The HttpClient for making requests.</param>
  /// <param name="loggerFactory">The factory to create loggers for this resource.</param>
  public TurnstileApi(HttpClient httpClient, ILoggerFactory loggerFactory)
    : base(httpClient, loggerFactory.CreateLogger<TurnstileApi>())
  {
  }

  #endregion


  #region Widget Management

  /// <inheritdoc />
  public async Task<PagePaginatedResult<TurnstileWidget>> ListWidgetsAsync(
    string accountId,
    ListTurnstileWidgetsFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);

    var endpoint = BuildWidgetsListQueryString(accountId, filters);

    return await GetPagePaginatedResultAsync<TurnstileWidget>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<TurnstileWidget> ListAllWidgetsAsync(
    string accountId,
    ListTurnstileWidgetsFilters? filters = null,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);

    // Build base endpoint without pagination parameters (handled by GetPaginatedAsync).
    var baseFilters = filters is not null ? filters with { Page = null, PerPage = null } : null;
    var endpoint = BuildWidgetsListQueryString(accountId, baseFilters);

    return GetPaginatedAsync<TurnstileWidget>(endpoint, filters?.PerPage, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<TurnstileWidget> GetWidgetAsync(
    string accountId,
    string sitekey,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(sitekey);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/challenges/widgets/{Uri.EscapeDataString(sitekey)}";

    return await GetAsync<TurnstileWidget>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<TurnstileWidget> CreateWidgetAsync(
    string accountId,
    CreateTurnstileWidgetRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/challenges/widgets";

    return await PostAsync<TurnstileWidget>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<TurnstileWidget> UpdateWidgetAsync(
    string accountId,
    string sitekey,
    UpdateTurnstileWidgetRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(sitekey);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/challenges/widgets/{Uri.EscapeDataString(sitekey)}";

    return await PutAsync<TurnstileWidget>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteWidgetAsync(
    string accountId,
    string sitekey,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(sitekey);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/challenges/widgets/{Uri.EscapeDataString(sitekey)}";

    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  #endregion


  #region Secret Rotation

  /// <inheritdoc />
  public async Task<RotateWidgetSecretResult> RotateSecretAsync(
    string accountId,
    string sitekey,
    bool invalidateImmediately = false,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(accountId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(sitekey);

    var endpoint = $"accounts/{Uri.EscapeDataString(accountId)}/challenges/widgets/{Uri.EscapeDataString(sitekey)}/rotate_secret";
    var request = new RotateWidgetSecretRequest(invalidateImmediately);

    return await PostAsync<RotateWidgetSecretResult>(endpoint, request, cancellationToken);
  }

  #endregion


  #region Helpers

  /// <summary>
  ///   Builds the query string for the widgets list endpoint.
  /// </summary>
  /// <param name="accountId">The account identifier.</param>
  /// <param name="filters">Optional filters to apply.</param>
  /// <returns>The endpoint with query string.</returns>
  private static string BuildWidgetsListQueryString(string accountId, ListTurnstileWidgetsFilters? filters)
  {
    var queryParams = new List<string>();

    if (filters?.Order is not null)
      queryParams.Add($"order={EnumHelper.GetEnumMemberValue(filters.Order.Value)}");
    if (filters?.Direction is not null)
      queryParams.Add($"direction={EnumHelper.GetEnumMemberValue(filters.Direction.Value)}");
    if (filters?.Page is not null)
      queryParams.Add($"page={filters.Page}");
    if (filters?.PerPage is not null)
      queryParams.Add($"per_page={filters.PerPage}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;

    return $"accounts/{Uri.EscapeDataString(accountId)}/challenges/widgets{queryString}";
  }

  #endregion
}
