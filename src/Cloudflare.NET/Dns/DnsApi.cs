namespace Cloudflare.NET.Dns;

using Core;
using Core.Internal;
using Core.Models;
using Microsoft.Extensions.Logging;
using Models;
using DnsRecordType = Zones.Models.DnsRecordType;

/// <summary>Implements the API for managing DNS records in Cloudflare zones.</summary>
public class DnsApi : ApiResource, IDnsApi
{
  #region Constructors

  /// <summary>Initializes a new instance of the <see cref="DnsApi"/> class.</summary>
  /// <param name="httpClient">The HttpClient for making API requests.</param>
  /// <param name="loggerFactory">The factory to create loggers.</param>
  public DnsApi(HttpClient httpClient, ILoggerFactory loggerFactory)
    : base(httpClient, loggerFactory.CreateLogger<DnsApi>())
  {
  }

  #endregion


  #region Methods Impl - Query Operations

  /// <inheritdoc />
  public async Task<DnsRecord> GetDnsRecordAsync(
    string            zoneId,
    string            dnsRecordId,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(zoneId);
    ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
    ArgumentNullException.ThrowIfNull(dnsRecordId);
    ArgumentException.ThrowIfNullOrWhiteSpace(dnsRecordId);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/dns_records/{Uri.EscapeDataString(dnsRecordId)}";

    return await GetAsync<DnsRecord>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<PagePaginatedResult<DnsRecord>> ListDnsRecordsAsync(
    string                 zoneId,
    ListDnsRecordsFilters? filters           = null,
    CancellationToken      cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(zoneId);
    ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);

    var queryString = BuildDnsListQueryString(filters);
    var endpoint    = $"zones/{Uri.EscapeDataString(zoneId)}/dns_records{queryString}";

    return await GetPagePaginatedResultAsync<DnsRecord>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public IAsyncEnumerable<DnsRecord> ListAllDnsRecordsAsync(
    string                 zoneId,
    ListDnsRecordsFilters? filters           = null,
    CancellationToken      cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(zoneId);
    ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);

    // Ensure pagination parameters are excluded from the base URI for the helper.
    var listFilters = filters is not null ? filters with { Page = null, PerPage = null } : null;
    var queryString = BuildDnsListQueryString(listFilters);
    var endpoint    = $"zones/{Uri.EscapeDataString(zoneId)}/dns_records{queryString}";

    return GetPaginatedAsync<DnsRecord>(endpoint, filters?.PerPage, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<DnsRecord?> FindDnsRecordByNameAsync(
    string            zoneId,
    string            hostname,
    DnsRecordType?    type              = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(zoneId);
    ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
    ArgumentNullException.ThrowIfNull(hostname);
    ArgumentException.ThrowIfNullOrWhiteSpace(hostname);

    var filters = new ListDnsRecordsFilters(Name: hostname, Type: type);
    var result  = await ListDnsRecordsAsync(zoneId, filters, cancellationToken);

    return result.Items.FirstOrDefault();
  }

  #endregion


  #region Methods Impl - Create Operations

  /// <inheritdoc />
  public async Task<DnsRecord> CreateDnsRecordAsync(
    string                 zoneId,
    CreateDnsRecordRequest request,
    CancellationToken      cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(zoneId);
    ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/dns_records";

    return await PostAsync<DnsRecord>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<DnsRecord> CreateCnameRecordAsync(
    string            zoneId,
    string            name,
    string            target,
    bool              proxied           = false,
    int               ttl               = 1,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(zoneId);
    ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
    ArgumentNullException.ThrowIfNull(name);
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ArgumentNullException.ThrowIfNull(target);
    ArgumentException.ThrowIfNullOrWhiteSpace(target);

    var request = new CreateDnsRecordRequest(DnsRecordType.CNAME, name, target, ttl, proxied);

    return await CreateDnsRecordAsync(zoneId, request, cancellationToken);
  }

  #endregion


  #region Methods Impl - Update Operations

  /// <inheritdoc />
  public async Task<DnsRecord> UpdateDnsRecordAsync(
    string                  zoneId,
    string                  dnsRecordId,
    UpdateDnsRecordRequest  request,
    CancellationToken       cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(zoneId);
    ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
    ArgumentNullException.ThrowIfNull(dnsRecordId);
    ArgumentException.ThrowIfNullOrWhiteSpace(dnsRecordId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/dns_records/{Uri.EscapeDataString(dnsRecordId)}";

    return await PutAsync<DnsRecord>(endpoint, request, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<DnsRecord> PatchDnsRecordAsync(
    string                 zoneId,
    string                 dnsRecordId,
    PatchDnsRecordRequest  request,
    CancellationToken      cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(zoneId);
    ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
    ArgumentNullException.ThrowIfNull(dnsRecordId);
    ArgumentException.ThrowIfNullOrWhiteSpace(dnsRecordId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/dns_records/{Uri.EscapeDataString(dnsRecordId)}";

    return await PatchAsync<DnsRecord>(endpoint, request, cancellationToken);
  }

  #endregion


  #region Methods Impl - Delete Operations

  /// <inheritdoc />
  public async Task DeleteDnsRecordAsync(
    string            zoneId,
    string            dnsRecordId,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(zoneId);
    ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
    ArgumentNullException.ThrowIfNull(dnsRecordId);
    ArgumentException.ThrowIfNullOrWhiteSpace(dnsRecordId);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/dns_records/{Uri.EscapeDataString(dnsRecordId)}";

    await DeleteAsync<object>(endpoint, cancellationToken);
  }

  #endregion


  #region Methods Impl - Batch Operations

  /// <inheritdoc />
  public async Task<BatchDnsRecordsResult> BatchDnsRecordsAsync(
    string                  zoneId,
    BatchDnsRecordsRequest  request,
    CancellationToken       cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(zoneId);
    ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/dns_records/batch";

    return await PostAsync<BatchDnsRecordsResult>(endpoint, request, cancellationToken);
  }

  #endregion


  #region Methods Impl - Import/Export Operations

  /// <inheritdoc />
  public async Task<string> ExportDnsRecordsAsync(
    string            zoneId,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(zoneId);
    ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/dns_records/export";

    return await GetStringAsync(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<DnsImportResult> ImportDnsRecordsAsync(
    string            zoneId,
    string            bindContent,
    bool              proxied           = false,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(zoneId);
    ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
    ArgumentNullException.ThrowIfNull(bindContent);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/dns_records/import";

    // Convert string content to stream for multipart upload
    using var stream = new MemoryStream();
    await using var writer = new StreamWriter(stream, leaveOpen: true);
    await writer.WriteAsync(bindContent);
    await writer.FlushAsync(cancellationToken);
    stream.Position = 0;

    return await PostMultipartFileAsync<DnsImportResult>(
      endpoint,
      stream,
      "bind_config.txt",
      "file",
      cancellationToken);
  }

  #endregion


  #region Methods Impl - Scan Operations

  /// <inheritdoc />
  public async Task TriggerDnsRecordScanAsync(
    string            zoneId,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(zoneId);
    ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/dns_records/scan/trigger";

    // Trigger returns void (no result body) - just ensure success
    await PostAsync<object>(endpoint, null, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<IReadOnlyList<DnsRecord>> GetDnsRecordScanReviewAsync(
    string            zoneId,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(zoneId);
    ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/dns_records/scan/review";

    return await GetAsync<IReadOnlyList<DnsRecord>>(endpoint, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<DnsScanReviewResult> SubmitDnsRecordScanReviewAsync(
    string               zoneId,
    DnsScanReviewRequest request,
    CancellationToken    cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(zoneId);
    ArgumentException.ThrowIfNullOrWhiteSpace(zoneId);
    ArgumentNullException.ThrowIfNull(request);

    var endpoint = $"zones/{Uri.EscapeDataString(zoneId)}/dns_records/scan/review";

    return await PostAsync<DnsScanReviewResult>(endpoint, request, cancellationToken);
  }

  #endregion


  #region Methods - Query String Builder

  /// <summary>Builds a query string for listing DNS records based on the provided filters.</summary>
  /// <param name="filters">The filter parameters for the DNS record list operation.</param>
  /// <returns>A query string starting with '?' if any filters are specified; otherwise, an empty string.</returns>
  private static string BuildDnsListQueryString(ListDnsRecordsFilters? filters)
  {
    if (filters is null)
      return string.Empty;

    var queryParams = new List<string>();

    // Type filter (extensible enum)
    if (filters.Type.HasValue)
      queryParams.Add($"type={Uri.EscapeDataString(filters.Type.Value.Value)}");

    // String filters
    if (!string.IsNullOrWhiteSpace(filters.Name))
      queryParams.Add($"name={Uri.EscapeDataString(filters.Name)}");
    if (!string.IsNullOrWhiteSpace(filters.Content))
      queryParams.Add($"content={Uri.EscapeDataString(filters.Content)}");

    // Boolean filter
    if (filters.Proxied.HasValue)
      queryParams.Add($"proxied={filters.Proxied.Value.ToString().ToLower()}");

    // Pagination
    if (filters.Page.HasValue)
      queryParams.Add($"page={filters.Page.Value}");
    if (filters.PerPage.HasValue)
      queryParams.Add($"per_page={filters.PerPage.Value}");

    // Ordering
    if (!string.IsNullOrWhiteSpace(filters.Order))
      queryParams.Add($"order={Uri.EscapeDataString(filters.Order)}");
    if (filters.Direction.HasValue)
      queryParams.Add($"direction={EnumHelper.GetEnumMemberValue(filters.Direction.Value)}");

    return queryParams.Count > 0 ? $"?{string.Join("&", queryParams)}" : string.Empty;
  }

  #endregion
}
