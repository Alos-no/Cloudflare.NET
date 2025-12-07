namespace Cloudflare.NET.SampleCoreConsole.Samples;

using Microsoft.Extensions.Logging;
using Zones.CustomHostnames.Models;

/// <summary>
///   Demonstrates the Custom Hostnames API (Cloudflare for SaaS) capabilities.
///   <para>
///     Custom Hostnames allow SaaS providers to extend Cloudflare's security and performance benefits to their
///     customers' vanity domains. This sample covers:
///   </para>
///   <list type="bullet">
///     <item><description>Creating a custom hostname with SSL configuration.</description></item>
///     <item><description>Retrieving custom hostname status and ownership verification details.</description></item>
///     <item><description>Updating a custom hostname (e.g., changing TLS settings).</description></item>
///     <item><description>Listing all custom hostnames with automatic pagination.</description></item>
///     <item><description>Fallback origin management.</description></item>
///     <item><description>Deleting a custom hostname.</description></item>
///   </list>
/// </summary>
public class CustomHostnameSamples(ICloudflareApiClient cf, ILogger<CustomHostnameSamples> logger)
{
  #region Methods

  /// <summary>
  ///   Runs the complete Custom Hostnames API sample lifecycle.
  /// </summary>
  /// <param name="zoneId">The zone ID to operate on.</param>
  /// <param name="baseDomain">The base domain of the zone (e.g., "example.com").</param>
  /// <returns>A list of cleanup actions to be executed after the sample run.</returns>
  public async Task<List<Func<Task>>> RunCustomHostnameSamplesAsync(string zoneId, string baseDomain)
  {
    var cleanupActions = new List<Func<Task>>();

    // Generate a unique subdomain for this sample run to avoid collisions.
    // In a real SaaS scenario, this would be a customer's vanity domain (e.g., "app.customer.com").
    var customerHostname = $"saas-demo-{Guid.NewGuid():N}.{baseDomain}";

    // 1. Create a Custom Hostname with SSL configuration.
    var customHostname = await CreateCustomHostnameAsync(zoneId, customerHostname, cleanupActions);

    if (customHostname is null)
    {
      logger.LogWarning("Failed to create custom hostname. Skipping remaining samples.");

      return cleanupActions;
    }

    // 2. Retrieve the custom hostname to inspect its status and verification details.
    await GetCustomHostnameDetailsAsync(zoneId, customHostname.Id);

    // 3. Update the custom hostname (change TLS settings).
    await UpdateCustomHostnameAsync(zoneId, customHostname.Id);

    // 4. List all custom hostnames with automatic pagination.
    await ListCustomHostnamesAsync(zoneId);

    // 5. Demonstrate fallback origin operations (optional, depends on zone configuration).
    await DemonstrateFallbackOriginAsync(zoneId);

    return cleanupActions;
  }

  /// <summary>Creates a new custom hostname with TXT-based domain control validation.</summary>
  private async Task<CustomHostname?> CreateCustomHostnameAsync(
    string           zoneId,
    string           hostname,
    List<Func<Task>> cleanupActions)
  {
    logger.LogInformation("--- Creating Custom Hostname: {Hostname} ---", hostname);

    // Configure SSL with:
    // - TXT-based domain control validation (most common for SaaS providers).
    // - Domain Validation (DV) certificate type.
    // - TLS 1.2 minimum version for security.
    // - HTTP/2 enabled for performance.
    var sslConfig = new SslConfiguration(
      Method: DcvMethod.Txt,
      Type: CertificateType.Dv,
      Settings: new SslSettings(
        MinTlsVersion: MinTlsVersion.Tls12,
        Http2: SslToggle.On
      )
    );

    var request = new CreateCustomHostnameRequest(
      Hostname: hostname,
      Ssl: sslConfig
    );

    try
    {
      var result = await cf.Zones.CustomHostnames.CreateAsync(zoneId, request);

      logger.LogInformation("Created Custom Hostname:");
      logger.LogInformation("  Id:       {Id}", result.Id);
      logger.LogInformation("  Hostname: {Hostname}", result.Hostname);
      logger.LogInformation("  Status:   {Status}", result.Status);
      logger.LogInformation("  SSL Status: {SslStatus}", result.Ssl.Status);

      // Log ownership verification instructions.
      if (result.OwnershipVerification is not null)
      {
        logger.LogInformation("  Ownership Verification (TXT):");
        logger.LogInformation("    Name:  {Name}", result.OwnershipVerification.Name);
        logger.LogInformation("    Value: {Value}", result.OwnershipVerification.Value);
      }

      // Log SSL validation records.
      if (result.Ssl.ValidationRecords is { Count: > 0 })
      {
        logger.LogInformation("  SSL Validation Records:");

        foreach (var record in result.Ssl.ValidationRecords)
        {
          if (record.TxtName is not null)
            logger.LogInformation("    TXT: {Name} -> {Value}", record.TxtName, record.TxtValue);

          if (record.HttpUrl is not null)
            logger.LogInformation("    HTTP: {Url} -> {Body}", record.HttpUrl, record.HttpBody);
        }
      }

      // Add cleanup action to delete the custom hostname after the sample completes.
      cleanupActions.Add(async () =>
      {
        logger.LogInformation("Deleting custom hostname: {Id} ({Hostname})", result.Id, result.Hostname);
        await cf.Zones.CustomHostnames.DeleteAsync(zoneId, result.Id);
        logger.LogInformation("Deleted custom hostname: {Hostname}", result.Hostname);
      });

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to create custom hostname: {Hostname}", hostname);

      return null;
    }
  }

  /// <summary>Retrieves and displays detailed information about a custom hostname.</summary>
  private async Task GetCustomHostnameDetailsAsync(string zoneId, string customHostnameId)
  {
    logger.LogInformation("--- Retrieving Custom Hostname Details: {Id} ---", customHostnameId);

    var result = await cf.Zones.CustomHostnames.GetAsync(zoneId, customHostnameId);

    logger.LogInformation("Custom Hostname Details:");
    logger.LogInformation("  Id:           {Id}", result.Id);
    logger.LogInformation("  Hostname:     {Hostname}", result.Hostname);
    logger.LogInformation("  Status:       {Status}", result.Status);
    logger.LogInformation("  Created At:   {CreatedAt}", result.CreatedAt);

    // SSL Certificate details.
    logger.LogInformation("  SSL:");
    logger.LogInformation("    Status:     {Status}", result.Ssl.Status);
    logger.LogInformation("    Method:     {Method}", result.Ssl.Method);
    logger.LogInformation("    Type:       {Type}", result.Ssl.Type);

    if (result.Ssl.CertificateAuthority is not null)
      logger.LogInformation("    CA:         {CA}", result.Ssl.CertificateAuthority);

    if (result.Ssl.Settings is not null)
    {
      logger.LogInformation("    TLS Settings:");

      if (result.Ssl.Settings.MinTlsVersion is not null)
        logger.LogInformation("      Min TLS:  {Version}", result.Ssl.Settings.MinTlsVersion);

      if (result.Ssl.Settings.Http2 is not null)
        logger.LogInformation("      HTTP/2:   {Status}", result.Ssl.Settings.Http2);
    }

    // Custom origin configuration (if set).
    if (result.CustomOriginServer is not null)
      logger.LogInformation("  Custom Origin: {Origin}", result.CustomOriginServer);

    // Verification errors (if any).
    if (result.VerificationErrors is { Count: > 0 })
    {
      logger.LogWarning("  Verification Errors:");

      foreach (var error in result.VerificationErrors)
        logger.LogWarning("    - {Error}", error);
    }
  }

  /// <summary>Updates a custom hostname's TLS settings.</summary>
  private async Task UpdateCustomHostnameAsync(string zoneId, string customHostnameId)
  {
    logger.LogInformation("--- Updating Custom Hostname TLS Settings: {Id} ---", customHostnameId);

    // Update to enforce TLS 1.3 minimum and enable Early Hints.
    var updateRequest = new UpdateCustomHostnameRequest(
      Ssl: new SslConfiguration(
        Method: DcvMethod.Txt,
        Type: CertificateType.Dv,
        Settings: new SslSettings(
          MinTlsVersion: MinTlsVersion.Tls13,
          EarlyHints: SslToggle.On
        )
      )
    );

    var result = await cf.Zones.CustomHostnames.UpdateAsync(zoneId, customHostnameId, updateRequest);

    logger.LogInformation("Updated Custom Hostname:");
    logger.LogInformation("  Id:       {Id}", result.Id);
    logger.LogInformation("  Status:   {Status}", result.Status);

    if (result.Ssl.Settings is not null)
    {
      logger.LogInformation("  New TLS Settings:");

      if (result.Ssl.Settings.MinTlsVersion is not null)
        logger.LogInformation("    Min TLS:     {Version}", result.Ssl.Settings.MinTlsVersion);

      if (result.Ssl.Settings.EarlyHints is not null)
        logger.LogInformation("    Early Hints: {Status}", result.Ssl.Settings.EarlyHints);
    }
  }

  /// <summary>Lists all custom hostnames in the zone with automatic pagination.</summary>
  private async Task ListCustomHostnamesAsync(string zoneId)
  {
    logger.LogInformation("--- Listing All Custom Hostnames ---");

    var filters = new ListCustomHostnamesFilters(PerPage: 10);
    var count   = 0;

    await foreach (var hostname in cf.Zones.CustomHostnames.ListAllAsync(zoneId, filters))
    {
      count++;

      // Only log the first 10 for brevity.
      if (count <= 10)
      {
        logger.LogInformation("  [{Index}] {Hostname} (Status: {Status}, SSL: {SslStatus})",
                              count,
                              hostname.Hostname,
                              hostname.Status,
                              hostname.Ssl.Status);
      }
    }

    logger.LogInformation("Total custom hostnames in zone: {Count}", count);
  }

  /// <summary>Demonstrates fallback origin operations.</summary>
  /// <remarks>
  ///   The fallback origin is the default origin server used when a custom hostname does not have a specific
  ///   custom_origin_server configured. This sample shows how to get the current fallback origin.
  /// </remarks>
  private async Task DemonstrateFallbackOriginAsync(string zoneId)
  {
    logger.LogInformation("--- Fallback Origin Operations ---");

    try
    {
      // Try to get the current fallback origin.
      var fallbackOrigin = await cf.Zones.CustomHostnames.GetFallbackOriginAsync(zoneId);

      logger.LogInformation("Current Fallback Origin:");
      logger.LogInformation("  Origin: {Origin}", fallbackOrigin.Origin);
      logger.LogInformation("  Status: {Status}", fallbackOrigin.Status ?? "N/A");
    }
    catch (Exception ex)
    {
      // Fallback origin may not be configured for the zone.
      logger.LogWarning("Could not retrieve fallback origin: {Message}", ex.Message);
      logger.LogInformation("Fallback origin may not be configured for this zone.");
    }

    // Note: Updating the fallback origin requires the origin to be a valid hostname
    // that resolves to an IP address. For safety, we don't modify it in this sample.
    logger.LogInformation("Tip: Use UpdateFallbackOriginAsync to set a default origin for all custom hostnames.");
  }

  #endregion
}
