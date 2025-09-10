namespace Cloudflare.NET.Zones.Firewall;

using Core;
using Core.Models;
using Microsoft.Extensions.Logging;
using Security.Firewall.Models;

/// <summary>Implements the API for managing Zone Lockdown rules.</summary>
public class ZoneLockdownApi(HttpClient httpClient, ILoggerFactory loggerFactory)
  : ApiResource(httpClient, loggerFactory.CreateLogger<ZoneLockdownApi>()), IZoneLockdownApi
{
  #region Methods Impl

  /// <inheritdoc />
  public async Task<PagePaginatedResult<Lockdown>> ListAsync(string               zoneId,
                                                             ListLockdownFilters? filters           = null,
                                                             CancellationToken    cancellationToken = default)
  {
    var queryParams = new List<string>();

    if (filters?.Page is not null)
      queryParams.Add($"page={filters.Page}");
    if (filters?.PerPage is not null)
      queryParams.Add($"per_page={filters.PerPage}");

    var queryString = queryParams.Count > 0 ? $"?{string.Join('&', queryParams)}" : string.Empty;

    var endpoint = $"zones/{zoneId}/firewall/lockdowns{queryString}";
    return await GetPagePaginatedResultAsync<Lockdown>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<Lockdown> ListAllAsync(string zoneId, int? perPage = null, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/lockdowns";
    return GetPaginatedAsync<Lockdown>(endpoint, perPage, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Lockdown> GetAsync(string zoneId, string lockdownId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/lockdowns/{lockdownId}";
    return await GetAsync<Lockdown>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Lockdown> CreateAsync(string                zoneId,
                                          CreateLockdownRequest request,
                                          CancellationToken     cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/lockdowns";
    return await PostAsync<Lockdown>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<Lockdown> UpdateAsync(string                zoneId,
                                          string                lockdownId,
                                          UpdateLockdownRequest request,
                                          CancellationToken     cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/lockdowns/{lockdownId}";
    return await PutAsync<Lockdown>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<string> DeleteAsync(string zoneId, string lockdownId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/lockdowns/{lockdownId}";
    var result   = await DeleteAsync<IdResponse>(endpoint, cancellationToken);
    return result.Id;
  }

  #endregion

  private record IdResponse(string Id);
}
