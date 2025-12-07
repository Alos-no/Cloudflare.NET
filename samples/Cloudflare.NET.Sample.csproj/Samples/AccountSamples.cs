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

  #endregion
}
