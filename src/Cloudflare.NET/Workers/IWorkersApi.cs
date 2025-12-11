namespace Cloudflare.NET.Workers;

using Models;

/// <summary>
///   Provides access to Cloudflare Workers API operations.
///   <para>
///     This interface provides zone-scoped operations for managing Worker routes,
///     which map URL patterns to Worker scripts. Future expansion may include
///     Worker script management, bindings, and other Workers platform features.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     Worker routes are zone-scoped resources that determine which requests
///     are handled by which Workers based on URL pattern matching.
///   </para>
/// </remarks>
public interface IWorkersApi
{
  #region Worker Routes

  /// <summary>
  ///   Lists all Worker routes for a zone.
  /// </summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>All Worker routes in the zone.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var routes = await client.Workers.ListRoutesAsync(zoneId);
  ///
  ///   foreach (var route in routes)
  ///   {
  ///     Console.WriteLine($"{route.Pattern} -> {route.Script ?? "(disabled)"}");
  ///   }
  ///   </code>
  /// </example>
  Task<IReadOnlyList<WorkerRoute>> ListRoutesAsync(
    string zoneId,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Gets details for a specific Worker route.
  /// </summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="routeId">The route identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The Worker route details.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> or <paramref name="routeId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   var route = await client.Workers.GetRouteAsync(zoneId, routeId);
  ///   Console.WriteLine($"Pattern: {route.Pattern}, Script: {route.Script}");
  ///   </code>
  /// </example>
  Task<WorkerRoute> GetRouteAsync(
    string zoneId,
    string routeId,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Creates a new Worker route.
  /// </summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="request">The route creation parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The created Worker route.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> is null or whitespace.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request" /> is null.</exception>
  /// <example>
  ///   <code>
  ///   var route = await client.Workers.CreateRouteAsync(zoneId,
  ///     new CreateWorkerRouteRequest(
  ///       Pattern: "api.example.com/*",
  ///       Script: "api-handler"));
  ///
  ///   Console.WriteLine($"Created route: {route.Id}");
  ///   </code>
  /// </example>
  Task<WorkerRoute> CreateRouteAsync(
    string zoneId,
    CreateWorkerRouteRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Updates an existing Worker route.
  /// </summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="routeId">The route identifier.</param>
  /// <param name="request">The update parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated Worker route.</returns>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> or <paramref name="routeId" /> is null or whitespace.</exception>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="request" /> is null.</exception>
  /// <example>
  ///   <code>
  ///   var updated = await client.Workers.UpdateRouteAsync(zoneId, routeId,
  ///     new UpdateWorkerRouteRequest(
  ///       Pattern: "api.example.com/v2/*",
  ///       Script: "api-handler-v2"));
  ///   </code>
  /// </example>
  Task<WorkerRoute> UpdateRouteAsync(
    string zoneId,
    string routeId,
    UpdateWorkerRouteRequest request,
    CancellationToken cancellationToken = default);

  /// <summary>
  ///   Deletes a Worker route.
  /// </summary>
  /// <param name="zoneId">The zone identifier.</param>
  /// <param name="routeId">The route identifier.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <exception cref="ArgumentException">Thrown when <paramref name="zoneId" /> or <paramref name="routeId" /> is null or whitespace.</exception>
  /// <example>
  ///   <code>
  ///   await client.Workers.DeleteRouteAsync(zoneId, routeId);
  ///   Console.WriteLine($"Deleted route: {routeId}");
  ///   </code>
  /// </example>
  Task DeleteRouteAsync(
    string zoneId,
    string routeId,
    CancellationToken cancellationToken = default);

  #endregion
}
