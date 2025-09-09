namespace Cloudflare.NET.Zones;

using AccessRules;
using Core;
using Firewall;
using Models;
using Rulesets;

/// <summary>Implements the API for managing Cloudflare Zone resources.</summary>
public class ZonesApi : ApiResource, IZonesApi
{
  #region Properties & Fields - Non-Public

  /// <summary>The lazy-initialized Zone Access Rules API resource.</summary>
  private readonly Lazy<IZoneAccessRulesApi> _accessRules;

  /// <summary>The lazy-initialized Zone Rulesets API resource.</summary>
  private readonly Lazy<IZoneRulesetsApi> _rulesets;

  /// <summary>The lazy-initialized Zone Lockdown API resource.</summary>
  private readonly Lazy<IZoneLockdownApi> _lockdown;

  /// <summary>The lazy-initialized User-Agent Rules API resource.</summary>
  private readonly Lazy<IZoneUaRulesApi> _uaRules;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="ZonesApi" /> class.</summary>
  /// <param name="httpClient">The HttpClient for making requests.</param>
  public ZonesApi(HttpClient httpClient) : base(httpClient)
  {
    _accessRules = new Lazy<IZoneAccessRulesApi>(() => new ZoneAccessRulesApi(httpClient));
    _rulesets    = new Lazy<IZoneRulesetsApi>(() => new ZoneRulesetsApi(httpClient));
    _lockdown    = new Lazy<IZoneLockdownApi>(() => new ZoneLockdownApi(httpClient));
    _uaRules     = new Lazy<IZoneUaRulesApi>(() => new ZoneUaRulesApi(httpClient));
  }

  #endregion

  #region Properties Impl - Public

  /// <inheritdoc />
  public IZoneAccessRulesApi AccessRules => _accessRules.Value;

  /// <inheritdoc />
  public IZoneRulesetsApi Rulesets => _rulesets.Value;

  /// <inheritdoc />
  public IZoneLockdownApi Lockdown => _lockdown.Value;

  /// <inheritdoc />
  public IZoneUaRulesApi UaRules => _uaRules.Value;

  #endregion

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
