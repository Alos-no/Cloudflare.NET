namespace Cloudflare.NET.Zones.CustomHostnames;

using Core.Models;
using Models;

/// <summary>
///   <para>Defines the contract for managing Custom Hostnames (Cloudflare for SaaS) at the zone level.</para>
///   <para>
///     Custom Hostnames allow SaaS providers to extend Cloudflare's security and performance benefits to their
///     customers' vanity domains.
///   </para>
/// </summary>
/// <seealso href="https://developers.cloudflare.com/cloudflare-for-platforms/cloudflare-for-saas/" />
public interface ICustomHostnamesApi
{
  #region Custom Hostname CRUD

  /// <summary>Creates a new custom hostname for the specified zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="request">The request containing the custom hostname configuration.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The created custom hostname with its initial status and SSL information.</returns>
  Task<CustomHostname> CreateAsync(string                      zoneId,
                                   CreateCustomHostnameRequest request,
                                   CancellationToken           cancellationToken = default);

  /// <summary>Gets the details of a specific custom hostname.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="customHostnameId">The ID of the custom hostname to retrieve.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The custom hostname details including current status and SSL information.</returns>
  Task<CustomHostname> GetAsync(string            zoneId,
                                string            customHostnameId,
                                CancellationToken cancellationToken = default);

  /// <summary>Updates an existing custom hostname.</summary>
  /// <remarks>
  ///   This is a PATCH operation. Only the properties set in the request will be updated. Sending a PATCH request can
  ///   also be used to refresh the validation status.
  /// </remarks>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="customHostnameId">The ID of the custom hostname to update.</param>
  /// <param name="request">The request containing the fields to update.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated custom hostname.</returns>
  Task<CustomHostname> UpdateAsync(string                      zoneId,
                                   string                      customHostnameId,
                                   UpdateCustomHostnameRequest request,
                                   CancellationToken           cancellationToken = default);

  /// <summary>Deletes a custom hostname and any associated SSL certificates.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="customHostnameId">The ID of the custom hostname to delete.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  Task DeleteAsync(string            zoneId,
                   string            customHostnameId,
                   CancellationToken cancellationToken = default);

  #endregion

  #region Listing

  /// <summary>Lists custom hostnames for the specified zone, allowing for manual pagination control.</summary>
  /// <remarks>
  ///   This method is intended for developers who need to control the pagination process manually. Use the properties
  ///   of the returned <see cref="PagePaginatedResult{T}" /> to determine if there are more pages and to construct the
  ///   filter for the next call.
  /// </remarks>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="filters">Optional filters to apply, including pagination parameters.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>A single page of custom hostnames along with pagination information.</returns>
  Task<PagePaginatedResult<CustomHostname>> ListAsync(string                      zoneId,
                                                      ListCustomHostnamesFilters? filters           = null,
                                                      CancellationToken           cancellationToken = default);

  /// <summary>Lists all custom hostnames for the specified zone, automatically handling pagination.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="filters">Optional filters to apply. Pagination parameters (Page, PerPage) will be managed automatically.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>An asynchronous stream of all custom hostnames matching the criteria.</returns>
  IAsyncEnumerable<CustomHostname> ListAllAsync(string                      zoneId,
                                                ListCustomHostnamesFilters? filters           = null,
                                                CancellationToken           cancellationToken = default);

  #endregion

  #region Fallback Origin

  /// <summary>Gets the fallback origin for custom hostnames in the zone.</summary>
  /// <remarks>
  ///   The fallback origin is the default origin server used when a custom hostname does not have a specific
  ///   custom_origin_server configured.
  /// </remarks>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The fallback origin configuration.</returns>
  Task<FallbackOrigin> GetFallbackOriginAsync(string            zoneId,
                                              CancellationToken cancellationToken = default);

  /// <summary>Updates the fallback origin for custom hostnames in the zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="request">The request containing the new fallback origin.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  /// <returns>The updated fallback origin configuration.</returns>
  Task<FallbackOrigin> UpdateFallbackOriginAsync(string                      zoneId,
                                                 UpdateFallbackOriginRequest request,
                                                 CancellationToken           cancellationToken = default);

  /// <summary>Deletes the fallback origin configuration for the zone.</summary>
  /// <param name="zoneId">The ID of the zone.</param>
  /// <param name="cancellationToken">A cancellation token.</param>
  Task DeleteFallbackOriginAsync(string            zoneId,
                                 CancellationToken cancellationToken = default);

  #endregion
}
