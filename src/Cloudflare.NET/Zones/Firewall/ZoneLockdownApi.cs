namespace Cloudflare.NET.Zones.Firewall;

using Core;
using Security.Firewall.Models;

/// <summary>Implements the API for managing Zone Lockdown rules.</summary>
public class ZoneLockdownApi(HttpClient httpClient)
  : ApiResource(httpClient), IZoneLockdownApi
{
  #region Methods Impl

  /// <inheritdoc />
  public async Task<IReadOnlyList<Lockdown>> ListAsync(string zoneId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/firewall/lockdowns";
    return await GetAsync<IReadOnlyList<Lockdown>>(endpoint, cancellationToken);
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
