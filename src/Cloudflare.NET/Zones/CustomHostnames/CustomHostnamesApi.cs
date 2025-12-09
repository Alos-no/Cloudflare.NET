namespace Cloudflare.NET.Zones.CustomHostnames;

using System.Runtime.CompilerServices;
using Core;
using Core.Models;
using Microsoft.Extensions.Logging;
using Models;

/// <summary>Implements the API for managing Custom Hostnames (Cloudflare for SaaS) at the zone level.</summary>
public class CustomHostnamesApi(HttpClient httpClient, ILoggerFactory loggerFactory)
  : ApiResource(httpClient, loggerFactory.CreateLogger<CustomHostnamesApi>()), ICustomHostnamesApi
{
  #region Methods Impl

  /// <inheritdoc />
  public async Task<CustomHostname> CreateAsync(string                      zoneId,
                                                CreateCustomHostnameRequest request,
                                                CancellationToken           cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/custom_hostnames";

    return await PostAsync<CustomHostname>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<CustomHostname> GetAsync(string            zoneId,
                                             string            customHostnameId,
                                             CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/custom_hostnames/{customHostnameId}";

    return await GetAsync<CustomHostname>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<CustomHostname> UpdateAsync(string                      zoneId,
                                                string                      customHostnameId,
                                                UpdateCustomHostnameRequest request,
                                                CancellationToken           cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/custom_hostnames/{customHostnameId}";

    return await PatchAsync<CustomHostname>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteAsync(string            zoneId,
                                string            customHostnameId,
                                CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/custom_hostnames/{customHostnameId}";

    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<PagePaginatedResult<CustomHostname>> ListAsync(string                      zoneId,
                                                                   ListCustomHostnamesFilters? filters           = null,
                                                                   CancellationToken           cancellationToken = default)
  {
    var queryString = CustomHostnameQueryBuilder.Build(filters);
    var endpoint    = $"zones/{zoneId}/custom_hostnames{queryString}";

    return await GetPagePaginatedResultAsync<CustomHostname>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async IAsyncEnumerable<CustomHostname> ListAllAsync(
    string                                     zoneId,
    ListCustomHostnamesFilters?                filters           = null,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    // Create a new filter object for the query builder, ensuring pagination parameters are not included.
    var listFilters = filters is not null
      ? filters with { Page = null, PerPage = null }
      : null;

    var queryString = CustomHostnameQueryBuilder.Build(listFilters);
    var endpoint    = $"zones/{zoneId}/custom_hostnames{queryString}";

    // Track seen IDs to deduplicate. Cloudflare's pagination can return the same item
    // on multiple pages when data is being modified concurrently.
    var seenIds = new HashSet<string>();

    // Use the base class helper to handle the pagination loop, passing the original per_page value.
    // We wrap the enumeration to deduplicate by ID before yielding.
    await foreach (var hostname in GetPaginatedAsync<CustomHostname>(endpoint, filters?.PerPage, cancellationToken))
      // Only yield items we haven't seen before.
      if (seenIds.Add(hostname.Id))
        yield return hostname;
  }

  /// <inheritdoc />
  public async Task<FallbackOrigin> GetFallbackOriginAsync(string            zoneId,
                                                           CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/custom_hostnames/fallback_origin";

    return await GetAsync<FallbackOrigin>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<FallbackOrigin> UpdateFallbackOriginAsync(string                      zoneId,
                                                              UpdateFallbackOriginRequest request,
                                                              CancellationToken           cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/custom_hostnames/fallback_origin";

    return await PutAsync<FallbackOrigin>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteFallbackOriginAsync(string            zoneId,
                                              CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/custom_hostnames/fallback_origin";

    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  #endregion
}
