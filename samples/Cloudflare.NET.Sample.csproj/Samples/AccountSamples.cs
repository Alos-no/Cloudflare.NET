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

    // 5. R2 Lifecycle Configuration
    await RunLifecycleConfigurationAsync(bucketName);

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
            new[] { "GET", "PUT", "POST", "DELETE" },
            new[] { "http://localhost:3000", "http://localhost:5173" },
            new[] { "Content-Type", "Authorization" }
          ),
          "Local Development",
          new[] { "ETag", "Content-Length" },
          3600
        ),
        // Rule for production
        new CorsRule(
          new CorsAllowed(
            new[] { "GET", "HEAD" },
            new[] { "https://example.com" },
            new[] { "Content-Type" }
          ),
          "Production",
          new[] { "ETag" },
          7200
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
            new[] { "GET" },
            new[] { "*" },
            new[] { "Content-Type" }
          ),
          "Public Read",
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

  private async Task RunLifecycleConfigurationAsync(string bucketName)
  {
    logger.LogInformation("--- Running Lifecycle Configuration for bucket {BucketName} ---", bucketName);

    // 1. Set Lifecycle Policy with multiple rules demonstrating all capabilities
    logger.LogInformation("Setting lifecycle policy for bucket {BucketName}", bucketName);
    var lifecyclePolicy = new BucketLifecyclePolicy(
      new[]
      {
        // Rule to delete old log files after 90 days
        new LifecycleRule(
          "Delete old logs",
          true,
          new LifecycleRuleConditions("logs/"),
          DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(90))
        ),
        // Rule to abort incomplete multipart uploads after 7 days
        new LifecycleRule(
          "Cleanup incomplete uploads",
          true,
          AbortMultipartUploadsTransition: new AbortMultipartUploadsTransition(LifecycleCondition.AfterDays(7))
        ),
        // Rule to transition archived data to Infrequent Access storage class after 30 days
        new LifecycleRule(
          "Archive to Infrequent Access",
          true,
          new LifecycleRuleConditions("archive/"),
          StorageClassTransitions: new[]
          {
            new StorageClassTransition(LifecycleCondition.AfterDays(30), R2StorageClass.InfrequentAccess)
          }
        ),
        // Combined rule: transition to IA then delete (for temp files)
        new LifecycleRule(
          "Temp file lifecycle",
          true,
          new LifecycleRuleConditions("temp/"),
          StorageClassTransitions: new[]
          {
            new StorageClassTransition(LifecycleCondition.AfterDays(14), R2StorageClass.InfrequentAccess)
          },
          DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(60))
        )
      }
    );

    await cf.Accounts.SetBucketLifecycleAsync(bucketName, lifecyclePolicy);
    logger.LogInformation("Lifecycle policy set successfully with {RuleCount} rules", lifecyclePolicy.Rules.Count);

    // 2. Get Lifecycle Policy
    logger.LogInformation("Retrieving lifecycle policy for bucket {BucketName}", bucketName);
    var retrievedPolicy = await cf.Accounts.GetBucketLifecycleAsync(bucketName);
    logger.LogInformation("Retrieved lifecycle policy with {RuleCount} rules:", retrievedPolicy.Rules.Count);

    foreach (var rule in retrievedPolicy.Rules)
    {
      logger.LogInformation("  Rule: {Id} (Enabled: {Enabled})", rule.Id ?? "Unnamed", rule.Enabled);

      if (rule.Conditions?.Prefix != null)
        logger.LogInformation("    Prefix filter: {Prefix}", rule.Conditions.Prefix);

      if (rule.DeleteObjectsTransition != null)
        logger.LogInformation("    Delete after: {Days} days", rule.DeleteObjectsTransition.Condition.MaxAge);

      if (rule.AbortMultipartUploadsTransition != null)
        logger.LogInformation("    Abort multipart after: {Days} days", rule.AbortMultipartUploadsTransition.Condition.MaxAge);

      if (rule.StorageClassTransitions != null)
        foreach (var transition in rule.StorageClassTransitions)
          logger.LogInformation("    Transition to {StorageClass} after: {Days} days", transition.StorageClass,
                                transition.Condition.MaxAge);
    }

    // 3. Update Lifecycle Policy (simpler configuration)
    logger.LogInformation("Updating lifecycle policy to a simpler configuration");
    var simpleLifecyclePolicy = new BucketLifecyclePolicy(
      new[]
      {
        // Single rule: delete all objects after 365 days
        new LifecycleRule(
          "Annual cleanup",
          true,
          DeleteObjectsTransition: new DeleteObjectsTransition(LifecycleCondition.AfterDays(365))
        )
      }
    );

    await cf.Accounts.SetBucketLifecycleAsync(bucketName, simpleLifecyclePolicy);
    logger.LogInformation("Lifecycle policy updated to annual cleanup rule");

    // 4. Delete Lifecycle Policy
    logger.LogInformation("Deleting lifecycle policy from bucket {BucketName}", bucketName);
    await cf.Accounts.DeleteBucketLifecycleAsync(bucketName);
    logger.LogInformation("Lifecycle policy deleted successfully");
  }

  #endregion
}
