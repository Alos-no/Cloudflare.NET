namespace Cloudflare.NET.Sample.Samples;

using Accounts.Models;
using Microsoft.Extensions.Logging;

public class AccountSamples(ICloudflareApiClient cf, ILogger<AccountSamples> logger)
{
  #region Methods

  public async Task<List<Func<Task>>> RunR2SamplesAsync(string zoneId, string baseDomain)
  {
    var cleanupActions = new List<Func<Task>>();

    // Create a unique name for the R2 bucket used in this test run, to avoid collisions.
    var bucketName = $"cfnet-sample-bucket-{Guid.NewGuid():N}";

    // 1. Create R2 Bucket
    logger.LogInformation("Creating R2 Bucket: {BucketName}", bucketName);
    var bucket = await cf.Accounts.CreateR2BucketAsync(bucketName);
    logger.LogInformation("Created R2 Bucket: {Name} on {Date}", bucket.Name, bucket.CreationDate);

    cleanupActions.Add(async () =>
    {
      logger.LogInformation("Deleting R2 Bucket: {BucketName}", bucketName);
      await cf.Accounts.DeleteR2BucketAsync(bucketName);
      logger.LogInformation("Deleted R2 Bucket: {BucketName}", bucketName);
    });

    // 2. List R2 Buckets (paginated)
    logger.LogInformation("Listing all R2 buckets...");
    var count = 0;

    await foreach (var b in cf.Accounts.ListAllR2BucketsAsync(new ListR2BucketsFilters { PerPage = 5 }))
    {
      count++;
      if (count <= 5)
        logger.LogInformation("  Found bucket: {Name}", b.Name);
    }

    logger.LogInformation("Total buckets found: {Count} (showing first 5)", count);

    // 3. R2 Custom Domain Lifecycle
    await RunCustomDomainLifecycleAsync(bucketName, zoneId, baseDomain, cleanupActions);

    // 4. R2 CORS Configuration
    await RunCorsConfigurationAsync(bucketName);

    return cleanupActions;
  }

  private async Task RunCustomDomainLifecycleAsync(string bucketName, string zoneId, string baseDomain, List<Func<Task>> cleanupActions)
  {
    var hostname = $"r2-sample-{Guid.NewGuid():N}.{baseDomain}";

    logger.LogInformation("--- Running Custom Domain Lifecycle for {Hostname} ---", hostname);

    // 1. Attach Custom Domain
    logger.LogInformation("Attaching custom domain {Hostname} to bucket {BucketName}", hostname, bucketName);
    var attachResult = await cf.Accounts.AttachCustomDomainAsync(bucketName, hostname, zoneId);
    logger.LogInformation("Attach initiated. Domain: {Domain}, Status: {Status}", attachResult.Domain, attachResult.Status);

    cleanupActions.Insert(0, async () =>
    {
      logger.LogInformation("Detaching custom domain: {Hostname}", hostname);
      await cf.Accounts.DetachCustomDomainAsync(bucketName, hostname);
      logger.LogInformation("Detached custom domain: {Hostname}", hostname);
    });

    // 2. Get Custom Domain Status
    logger.LogInformation("Getting status for custom domain: {Hostname}", hostname);
    var statusResult = await cf.Accounts.GetCustomDomainStatusAsync(bucketName, hostname);
    logger.LogInformation("Got status. Domain: {Domain}, Status: {Status}", statusResult.Domain, statusResult.Status);
  }

  private async Task RunCorsConfigurationAsync(string bucketName)
  {
    logger.LogInformation("--- Running CORS Configuration for bucket {BucketName} ---", bucketName);

    // 1. Set CORS Policy with multiple rules
    logger.LogInformation("Setting CORS policy for bucket {BucketName}", bucketName);
    var corsPolicy = new BucketCorsPolicy(
      new[]
      {
        // Rule for local development
        new CorsRule(
          new CorsAllowed(
            Methods: new[] { "GET", "PUT", "POST", "DELETE" },
            Origins: new[] { "http://localhost:3000", "http://localhost:5173" },
            Headers: new[] { "Content-Type", "Authorization" }
          ),
          Id: "Local Development",
          ExposeHeaders: new[] { "ETag", "Content-Length" },
          MaxAgeSeconds: 3600
        ),
        // Rule for production
        new CorsRule(
          new CorsAllowed(
            Methods: new[] { "GET", "HEAD" },
            Origins: new[] { "https://example.com" },
            Headers: new[] { "Content-Type" }
          ),
          Id: "Production",
          ExposeHeaders: new[] { "ETag" },
          MaxAgeSeconds: 7200
        )
      }
    );

    await cf.Accounts.SetBucketCorsAsync(bucketName, corsPolicy);
    logger.LogInformation("CORS policy set successfully with {RuleCount} rules", corsPolicy.Rules.Count);

    // 2. Get CORS Policy
    logger.LogInformation("Retrieving CORS policy for bucket {BucketName}", bucketName);
    var retrievedPolicy = await cf.Accounts.GetBucketCorsAsync(bucketName);
    logger.LogInformation("Retrieved CORS policy with {RuleCount} rules:", retrievedPolicy.Rules.Count);

    foreach (var rule in retrievedPolicy.Rules)
    {
      logger.LogInformation("  Rule: {Id}", rule.Id ?? "Unnamed");
      logger.LogInformation("    Methods: {Methods}", string.Join(", ", rule.Allowed.Methods));
      logger.LogInformation("    Origins: {Origins}", string.Join(", ", rule.Allowed.Origins));
      logger.LogInformation("    Max Age: {MaxAge}s", rule.MaxAgeSeconds);
    }

    // 3. Update CORS Policy (simpler policy)
    logger.LogInformation("Updating CORS policy to a simpler configuration");
    var simpleCorsPolicy = new BucketCorsPolicy(
      new[]
      {
        new CorsRule(
          new CorsAllowed(
            Methods: new[] { "GET" },
            Origins: new[] { "*" },
            Headers: new[] { "Content-Type" }
          ),
          Id: "Public Read",
          MaxAgeSeconds: 86400
        )
      }
    );

    await cf.Accounts.SetBucketCorsAsync(bucketName, simpleCorsPolicy);
    logger.LogInformation("CORS policy updated to public read-only access");

    // 4. Delete CORS Policy
    logger.LogInformation("Deleting CORS policy from bucket {BucketName}", bucketName);
    await cf.Accounts.DeleteBucketCorsAsync(bucketName);
    logger.LogInformation("CORS policy deleted successfully");
  }

  #endregion
}
