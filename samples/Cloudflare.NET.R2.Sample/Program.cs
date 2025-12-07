namespace Cloudflare.NET.R2.Sample;

using Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
///   Entry point for the R2 sample. This demonstrates how to: - Build a Generic Host and wire up both the
///   Cloudflare REST client and the R2 client - Create or reuse a bucket - Upload small objects - List and download
///   objects - Clean up uploaded objects and (optionally) the temporary bucket
/// </summary>
public static class Program
{
  #region Methods

  /// <summary>Main entry point. Configures DI for Cloudflare REST + R2 clients and runs a short scenario against R2.</summary>
  public static async Task Main(string[] args)
  {
    // Build a Generic Host with configuration and logging.
    var builder = Host.CreateApplicationBuilder(args);

    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole(o =>
    {
      o.SingleLine      = true;
      o.IncludeScopes   = true;
      o.TimestampFormat = "HH:mm:ss.fff zzz ";
    });
    builder.Logging.SetMinimumLevel(LogLevel.Information);

    // Register the REST client (for account-scoped operations like bucket create/delete).
    builder.Services.AddCloudflareApiClient(builder.Configuration);

    // Register the R2 client (data-plane S3-compatible operations).
    builder.Services.AddCloudflareR2Client(builder.Configuration);

    using var host       = builder.Build();
    var       logger     = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("R2Sample");
    var       accounts   = host.Services.GetRequiredService<ICloudflareApiClient>().Accounts;
    var       r2         = host.Services.GetRequiredService<IR2Client>();
    var       cfOptions  = host.Services.GetRequiredService<IOptions<CloudflareApiOptions>>().Value;
    var       r2Settings = host.Services.GetRequiredService<IOptions<Configuration.R2Settings>>().Value;

    // Validate critical configuration.
    if (string.IsNullOrWhiteSpace(cfOptions.AccountId)
        || cfOptions.AccountId.Equals("from-user-secrets", StringComparison.OrdinalIgnoreCase))
    {
      logger.LogError("Cloudflare:AccountId is missing. Please configure appsettings, environment variables, or user secrets.");
      return;
    }

    if (string.IsNullOrWhiteSpace(cfOptions.ApiToken) || cfOptions.ApiToken.Equals("from-user-secrets", StringComparison.OrdinalIgnoreCase))
    {
      logger.LogError("Cloudflare:ApiToken is missing. Please configure appsettings, environment variables, or user secrets.");
      return;
    }

    if (string.IsNullOrWhiteSpace(r2Settings.AccessKeyId)
        || r2Settings.AccessKeyId.Equals("from-user-secrets", StringComparison.OrdinalIgnoreCase) ||
        string.IsNullOrWhiteSpace(r2Settings.SecretAccessKey)
        || r2Settings.SecretAccessKey.Equals("from-user-secrets", StringComparison.OrdinalIgnoreCase))
    {
      logger.LogError("R2 credentials are missing. Please configure R2:AccessKeyId and R2:SecretAccessKey.");
      return;
    }

    // Determine bucket name:
    // - If R2:SampleBucketName is provided, reuse it and DO NOT delete it.
    // - Otherwise, create a temporary unique bucket and delete it at the end.
    var configuredName = GetOptionalConfig(host, "R2:SampleBucketName");
    var bucketName = string.IsNullOrWhiteSpace(configuredName)
      ? $"cfnet-sample-{Guid.NewGuid():N}"
      : configuredName;
    var createdBucket = string.IsNullOrWhiteSpace(configuredName); // delete only if we created it

    try
    {
      // If creating, call the Accounts API (management plane) to create the bucket.
      if (createdBucket)
      {
        logger.LogInformation("Creating temporary R2 bucket: {Bucket}", bucketName);
        await accounts.CreateR2BucketAsync(bucketName);
      }
      else
      {
        logger.LogInformation("Using existing R2 bucket: {Bucket}", bucketName);
      }

      // Upload a small object from memory using single-part upload.
      var smallKey = $"samples/small-{Guid.NewGuid():N}.txt";

      await using (var smallStream = new MemoryStream(CreateRandomBytes(512)))
      {
        logger.LogInformation("Uploading small object: s3://{Bucket}/{Key} ({Size} bytes)", bucketName, smallKey, smallStream.Length);
        var result = await r2.UploadAsync(bucketName, smallKey, smallStream);
        logger.LogInformation("Upload complete. ClassA={A}, Ingress={Ingress} bytes", result.ClassAOperations, result.IngressBytes);
      }

      // List under the "samples/" prefix.
      logger.LogInformation("Listing objects under prefix 'samples/'...");
      var listResult = await r2.ListObjectsAsync(bucketName, "samples/");

      foreach (var obj in listResult.Data.Take(5))
        logger.LogInformation("  {Key} ({Size} bytes)", obj.Key, obj.Size);

      logger.LogInformation("Total objects under prefix: {Count}", listResult.Data.Count);

      // Download the small object we just uploaded to a temporary file.
      var downloadPath = Path.Combine(Path.GetTempPath(), $"cfnet-sample-{Guid.NewGuid():N}.txt");
      logger.LogInformation("Downloading first 'samples/' object to: {Path}", downloadPath);

      var first = listResult.Data.FirstOrDefault();

      if (first is not null)
      {
        var dl = await r2.DownloadFileAsync(bucketName, first.Key, downloadPath);
        logger.LogInformation("Download complete. ClassB={B}, Egress={Egress} bytes", dl.ClassBOperations, dl.EgressBytes);
      }
      else
      {
        logger.LogWarning("No objects found under 'samples/'. Skipping download.");
      }

      // Delete uploaded objects under "samples/" to keep the bucket tidy.
      logger.LogInformation("Deleting 'samples/' objects...");
      var keys = listResult.Data.Select(o => o.Key).ToList();

      if (keys.Count > 0)
      {
        var del = await r2.DeleteObjectsAsync(bucketName, keys);
        logger.LogInformation("Deleted {Count} objects. ClassA={A}", keys.Count, del.ClassAOperations);
      }
      else
      {
        logger.LogInformation("Nothing to delete.");
      }
    }
    finally
    {
      // Clean up the temporary bucket only if we created it in this run.
      if (createdBucket)
        try
        {
          logger.LogInformation("Deleting temporary bucket: {Bucket}", bucketName);
          await accounts.DeleteR2BucketAsync(bucketName);
        }
        catch (Exception ex)
        {
          logger.LogWarning(ex, "Failed to delete the temporary bucket {Bucket}. You may need to clean it up manually.", bucketName);
        }
    }
  }

  /// <summary>
  ///   Gets a single configuration value if present; returns an empty string if not found. This keeps the sample
  ///   resilient if the optional key is omitted.
  /// </summary>
  private static string GetOptionalConfig(IHost host, string key)
  {
    var cfg = host.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
    return cfg[key] ?? string.Empty;
  }

  /// <summary>Creates a buffer of random bytes of a given size for demonstration purposes.</summary>
  /// <param name="size">The number of bytes to generate.</param>
  private static byte[] CreateRandomBytes(int size)
  {
    var data = new byte[size];
    Random.Shared.NextBytes(data);

    return data;
  }

  #endregion
}
