namespace Cloudflare.NET.Zones;

using Core;
using Models;

/// <summary>Implements the API for managing Cloudflare Zone resources.</summary>
/// <remarks>Initializes a new instance of the <see cref="ZonesApi" /> class.</remarks>
/// <param name="httpClient">The HttpClient for making requests.</param>
public class ZonesApi(HttpClient httpClient) : ApiResource(httpClient), IZonesApi
{
  #region Methods Impl

  /// <inheritdoc />
  public async Task<Zone> GetZoneDetailsAsync(string zoneId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}";
    return await GetAsync<Zone>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task DeleteDnsRecordAsync(string zoneId, string dnsRecordId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/dns_records/{dnsRecordId}";
    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<DnsRecord> CreateCnameRecordAsync(string            zoneId,
                                                      string            hostname,
                                                      string            cnameTarget,
                                                      CancellationToken cancellationToken = default)
  {
    var requestBody = new CreateDnsRecordRequest("CNAME", hostname, cnameTarget, 300, true);
    var endpoint    = $"zones/{zoneId}/dns_records";
    return await PostAsync<DnsRecord>(endpoint, requestBody, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<DnsRecord?> FindDnsRecordByNameAsync(string zoneId, string hostname, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/dns_records?name={hostname}";
    var result   = await GetAsync<List<DnsRecord>>(endpoint, cancellationToken);
    return result.FirstOrDefault();
  }

  #endregion
}
