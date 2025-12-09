namespace Cloudflare.NET.Zones;

using AccessRules;
using Core;
using Core.Internal;
using Core.Models;
using CustomHostnames;
using Firewall;
using Microsoft.Extensions.Logging;
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

  /// <summary>The lazy-initialized Custom Hostnames API resource.</summary>
  private readonly Lazy<ICustomHostnamesApi> _customHostnames;

  #endregion

  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="ZonesApi" /> class.</summary>
  /// <param name="httpClient">The HttpClient for making requests.</param>
  /// <param name="loggerFactory">The factory to create loggers for this and child resources.</param>
  public ZonesApi(HttpClient httpClient, ILoggerFactory loggerFactory)
    : base(httpClient, loggerFactory.CreateLogger<ZonesApi>())
  {
    _accessRules     = new Lazy<IZoneAccessRulesApi>(() => new ZoneAccessRulesApi(httpClient, loggerFactory));
    _rulesets        = new Lazy<IZoneRulesetsApi>(() => new ZoneRulesetsApi(httpClient, loggerFactory));
    _lockdown        = new Lazy<IZoneLockdownApi>(() => new ZoneLockdownApi(httpClient, loggerFactory));
    _uaRules         = new Lazy<IZoneUaRulesApi>(() => new ZoneUaRulesApi(httpClient, loggerFactory));
    _customHostnames = new Lazy<ICustomHostnamesApi>(() => new CustomHostnamesApi(httpClient, loggerFactory));
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

  /// <inheritdoc />
  public ICustomHostnamesApi CustomHostnames => _customHostnames.Value;

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
    var requestBody = new CreateDnsRecordRequest(DnsRecordType.CNAME, hostname, cnameTarget, 300, true);
    var endpoint    = $"zones/{zoneId}/dns_records";
    return await PostAsync<DnsRecord>(endpoint, requestBody, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<DnsRecord> ListAllDnsRecordsAsync(
    string                 zoneId,
    ListDnsRecordsFilters? filters           = null,
    CancellationToken      cancellationToken = default)
  {
    // Ensure pagination parameters are excluded from the base URI for the helper.
    var listFilters = filters is not null ? filters with { Page = null, PerPage = null } : null;
    var queryString = BuildDnsListQueryString(listFilters);
    var endpoint    = $"zones/{zoneId}/dns_records{queryString}";
    return GetPaginatedAsync<DnsRecord>(endpoint, filters?.PerPage, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<string> ExportDnsRecordsAsync(string zoneId, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/dns_records/export";
    return await GetStringAsync(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<DnsImportResult> ImportDnsRecordsAsync(
    string            zoneId,
    Stream            bindStream,
    bool              proxied,
    bool              overwriteExisting,
    CancellationToken cancellationToken = default)
  {
    var endpoint =
      $"zones/{zoneId}/dns_records/import?proxied={proxied.ToString().ToLower()}&overwrite_existing={overwriteExisting.ToString().ToLower()}";
    return await PostMultipartFileAsync<DnsImportResult>(endpoint, bindStream, "bind_config.txt", "file", cancellationToken);
  }

  /// <inheritdoc />
  public async Task<PurgeCacheResult> PurgeCacheAsync(
    string            zoneId,
    PurgeCacheRequest request,
    CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/purge_cache";
    return await PostAsync<PurgeCacheResult>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<DnsRecord?> FindDnsRecordByNameAsync(string zoneId, string hostname, CancellationToken cancellationToken = default)
  {
    var endpoint = $"zones/{zoneId}/dns_records?name={hostname}";
    var result   = await GetAsync<List<DnsRecord>>(endpoint, cancellationToken);
    return result.FirstOrDefault();
  }

  /// <inheritdoc />
  public async Task<PagePaginatedResult<DnsRecord>> ListDnsRecordsAsync(
    string                 zoneId,
    ListDnsRecordsFilters? filters           = null,
    CancellationToken      cancellationToken = default)
  {
    var queryString = BuildDnsListQueryString(filters);
    var endpoint    = $"zones/{zoneId}/dns_records{queryString}";
    return await GetPagePaginatedResultAsync<DnsRecord>(endpoint, cancellationToken);
  }

  #endregion

  #region Methods

  /// <summary>Builds a query string for listing DNS records based on the provided filters.</summary>
  private static string BuildDnsListQueryString(ListDnsRecordsFilters? filters)
  {
    if (filters is null)
      return string.Empty;

    var queryParams = new List<string>();

    if (filters.Type.HasValue)
      queryParams.Add($"type={Uri.EscapeDataString(filters.Type.Value.Value)}");
    if (!string.IsNullOrWhiteSpace(filters.Name))
      queryParams.Add($"name={Uri.EscapeDataString(filters.Name)}");
    if (!string.IsNullOrWhiteSpace(filters.Content))
      queryParams.Add($"content={Uri.EscapeDataString(filters.Content)}");
    if (filters.Proxied.HasValue)
      queryParams.Add($"proxied={filters.Proxied.Value.ToString().ToLower()}");
    if (filters.Page.HasValue)
      queryParams.Add($"page={filters.Page.Value}");
    if (filters.PerPage.HasValue)
      queryParams.Add($"per_page={filters.PerPage.Value}");
    if (!string.IsNullOrWhiteSpace(filters.Order))
      queryParams.Add($"order={Uri.EscapeDataString(filters.Order)}");
    if (filters.Direction.HasValue)
      queryParams.Add($"direction={EnumHelper.GetEnumMemberValue(filters.Direction.Value)}");

    return queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
  }

  #endregion
}
