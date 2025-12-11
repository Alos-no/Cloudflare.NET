namespace Cloudflare.NET.Workers;

using Core;
using Core.Internal;
using Microsoft.Extensions.Logging;
using Models;

/// <summary>
///   Implementation of the Workers API client for managing Worker routes.
/// </summary>
public class WorkersApi : ApiResource, IWorkersApi
{
  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="WorkersApi" /> class.</summary>
  /// <param name="httpClient">The HttpClient for making requests.</param>
  /// <param name="loggerFactory">The factory to create loggers for this resource.</param>
  public WorkersApi(HttpClient httpClient, ILoggerFactory loggerFactory)
    : base(httpClient, loggerFactory.CreateLogger<WorkersApi>())
  {
  }

  #endregion


  #region Worker Routes

  /// <inheritdoc />
  public async Task<IReadOnlyList<WorkerRoute>> ListRoutesAsync(
    string zoneId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(zoneId);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/workers/routes";

    return await GetAsync<IReadOnlyList<WorkerRoute>>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<WorkerRoute> GetRouteAsync(
    string zoneId,
    string routeId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(zoneId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(routeId);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/workers/routes/{Uri.EscapeDataString(routeId)}";

    return await GetAsync<WorkerRoute>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<WorkerRoute> CreateRouteAsync(
    string zoneId,
    CreateWorkerRouteRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(zoneId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/workers/routes";

    return await PostAsync<WorkerRoute>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<WorkerRoute> UpdateRouteAsync(
    string zoneId,
    string routeId,
    UpdateWorkerRouteRequest request,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(zoneId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(routeId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/workers/routes/{Uri.EscapeDataString(routeId)}";

    return await PutAsync<WorkerRoute>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteRouteAsync(
    string zoneId,
    string routeId,
    CancellationToken cancellationToken = default)
  {
    ThrowHelper.ThrowIfNullOrWhiteSpace(zoneId);
    ThrowHelper.ThrowIfNullOrWhiteSpace(routeId);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/workers/routes/{Uri.EscapeDataString(routeId)}";

    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  #endregion
}
