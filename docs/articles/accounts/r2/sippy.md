# R2 Sippy (Incremental Migration)

Sippy is Cloudflare's incremental migration service that helps you migrate data from other cloud storage providers to R2 without upfront egress fees. When enabled, objects not found in R2 are fetched from the source bucket, returned to the client, and copied to R2 for future requests.

## Overview

```csharp
public class MigrationService(ICloudflareApiClient cf)
{
    public async Task EnableMigrationAsync(string bucket, string sourceBucket, string region)
    {
        var request = new EnableSippyFromAwsRequest(
            SippyAwsSource.Create(
                sourceBucket,
                region,
                Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")!,
                Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")!
            )
        );

        await cf.Accounts.Buckets.EnableSippyAsync(bucket, request);
    }
}
```

> [!NOTE]
> **Jurisdictional Buckets:** If your bucket was created with a jurisdiction (e.g., `R2Jurisdiction.EuropeanUnion`), you must pass the jurisdiction parameter to all Sippy operations. See [Working with Jurisdictional Buckets](buckets.md#working-with-jurisdictional-buckets).
>
> ```csharp
> await cf.Accounts.Buckets.EnableSippyAsync("my-eu-bucket", request, R2Jurisdiction.EuropeanUnion);
> ```

## Supported Sources

Sippy supports migration from:

| Provider | Source Class |
|----------|-------------|
| Amazon S3 | `SippyAwsSource` |
| Google Cloud Storage | `SippyGcsSource` |

## Getting Sippy Status

Check if Sippy is enabled and view the configuration:

```csharp
var config = await cf.Accounts.Buckets.GetSippyAsync("my-bucket");

Console.WriteLine($"Sippy Enabled: {config.Enabled}");

if (config.Source is not null)
{
    Console.WriteLine($"Source Provider: {config.Source.Provider}");
    Console.WriteLine($"Source Bucket: {config.Source.Bucket}");
    Console.WriteLine($"Source URL: {config.Source.BucketUrl}");
}

if (config.Destination is not null)
{
    Console.WriteLine($"Destination: {config.Destination.Bucket}");
}
```

## Enabling Migration from AWS S3

Configure Sippy to migrate data from an Amazon S3 bucket:

```csharp
var awsSource = SippyAwsSource.Create(
    bucket: "my-source-bucket",
    region: "us-east-1",
    accessKeyId: "AKIAIOSFODNN7EXAMPLE",
    secretAccessKey: "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
);

var request = new EnableSippyFromAwsRequest(awsSource);

var result = await cf.Accounts.Buckets.EnableSippyAsync("my-r2-bucket", request);

Console.WriteLine($"Migration enabled: {result.Enabled}");
```

### AWS IAM Policy

The source credentials need at minimum these permissions:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:GetObject",
        "s3:ListBucket"
      ],
      "Resource": [
        "arn:aws:s3:::my-source-bucket",
        "arn:aws:s3:::my-source-bucket/*"
      ]
    }
  ]
}
```

## Enabling Migration from Google Cloud Storage

Configure Sippy to migrate data from a GCS bucket:

```csharp
var gcsSource = SippyGcsSource.Create(
    bucket: "my-gcs-bucket",
    clientEmail: "sippy@my-project.iam.gserviceaccount.com",
    privateKey: "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n"
);

var request = new EnableSippyFromGcsRequest(gcsSource);

var result = await cf.Accounts.Buckets.EnableSippyAsync("my-r2-bucket", request);
```

### GCS Service Account Permissions

The service account needs:
- `storage.objects.get` - Read objects
- `storage.objects.list` - List bucket contents

## Disabling Sippy

Disable incremental migration when migration is complete:

```csharp
await cf.Accounts.Buckets.DisableSippyAsync("my-bucket");
```

> [!NOTE]
> Disabling Sippy does not delete any objects already copied to R2. Objects already in R2 remain accessible.

## Models Reference

### SippyConfig

| Property | Type | Description |
|----------|------|-------------|
| `Enabled` | `bool` | Whether Sippy is enabled |
| `Source` | `SippySourceInfo?` | Source bucket information |
| `Destination` | `SippyDestination?` | Destination R2 bucket information |

### SippySourceInfo

| Property | Type | Description |
|----------|------|-------------|
| `Provider` | `SippyProvider?` | Source provider (aws, gcs) |
| `Bucket` | `string?` | Source bucket name |
| `BucketUrl` | `string?` | Full URL of source bucket |
| `Region` | `string?` | Source region (for AWS) |

### SippyAwsSource

| Property | Type | Description |
|----------|------|-------------|
| `Provider` | `SippyProvider` | Always `Aws` |
| `Bucket` | `string` | AWS S3 bucket name |
| `Region` | `string?` | AWS region (e.g., "us-east-1") |
| `AccessKeyId` | `string?` | AWS access key ID |
| `SecretAccessKey` | `string?` | AWS secret access key |

### SippyGcsSource

| Property | Type | Description |
|----------|------|-------------|
| `Provider` | `SippyProvider` | Always `Gcs` |
| `Bucket` | `string` | GCS bucket name |
| `ClientEmail` | `string?` | Service account email |
| `PrivateKey` | `string?` | Service account private key |

### SippyProvider

| Value | Description |
|-------|-------------|
| `Aws` | Amazon Web Services S3 |
| `Gcs` | Google Cloud Storage |
| `R2` | Cloudflare R2 (destination only) |

## Common Patterns

### Migration Workflow

```csharp
public async Task MigrateFromAwsAsync(
    string r2Bucket,
    string awsBucket,
    string awsRegion,
    string awsAccessKeyId,
    string awsSecretKey)
{
    // 1. Enable Sippy
    var source = SippyAwsSource.Create(awsBucket, awsRegion, awsAccessKeyId, awsSecretKey);
    await cf.Accounts.Buckets.EnableSippyAsync(r2Bucket, new EnableSippyFromAwsRequest(source));

    Console.WriteLine($"Sippy enabled. Objects will be migrated on-demand.");
    Console.WriteLine("Update your application to point to R2.");
    Console.WriteLine("Once migration is complete, disable Sippy.");
}
```

### Check Migration Status

```csharp
public async Task<bool> IsMigrationActiveAsync(string bucket)
{
    var config = await cf.Accounts.Buckets.GetSippyAsync(bucket);
    return config.Enabled;
}
```

### Environment-Based Configuration

```csharp
public async Task ConfigureMigrationAsync(string r2Bucket)
{
    var provider = Environment.GetEnvironmentVariable("MIGRATION_SOURCE_PROVIDER");

    EnableSippyRequest request = provider switch
    {
        "aws" => new EnableSippyFromAwsRequest(SippyAwsSource.Create(
            Environment.GetEnvironmentVariable("AWS_SOURCE_BUCKET")!,
            Environment.GetEnvironmentVariable("AWS_REGION")!,
            Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")!,
            Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY")!
        )),
        "gcs" => new EnableSippyFromGcsRequest(SippyGcsSource.Create(
            Environment.GetEnvironmentVariable("GCS_SOURCE_BUCKET")!,
            Environment.GetEnvironmentVariable("GCS_CLIENT_EMAIL")!,
            Environment.GetEnvironmentVariable("GCS_PRIVATE_KEY")!
        )),
        _ => throw new InvalidOperationException($"Unknown provider: {provider}")
    };

    await cf.Accounts.Buckets.EnableSippyAsync(r2Bucket, request);
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Workers R2 Storage | Account | Write |

## How Sippy Works

1. **Request arrives**: Client requests an object from R2
2. **Check R2**: If object exists in R2, return it immediately
3. **Fetch from source**: If not in R2, fetch from the source bucket
4. **Return and copy**: Return object to client while copying to R2
5. **Future requests**: Object now served directly from R2

```
     Client Request
           │
           ▼
    ┌─────────────┐
    │     R2      │
    │   Bucket    │
    └─────────────┘
           │
     Object exists?
         / \
       Yes  No
        │    │
        │    ▼
        │  ┌─────────────┐
        │  │   Source    │──── Fetch object
        │  │   Bucket    │
        │  └─────────────┘
        │         │
        │         ▼
        │    Copy to R2
        │         │
        └────┬────┘
             │
             ▼
     Return to Client
```

## Important Notes

1. **Egress costs**: You pay source provider egress fees only for objects not yet in R2
2. **Latency**: First request for uncached objects has higher latency (fetching from source)
3. **Credentials**: Source credentials are stored securely but never returned in API responses
4. **Consistency**: Objects are copied as-is; metadata is preserved
5. **Overwrite**: Objects already in R2 are not overwritten by source objects

## Migration Best Practices

1. **Pre-warm** frequently accessed objects by requesting them before cutover
2. **Monitor** R2 usage metrics to track migration progress
3. **Validate** critical objects after migration
4. **Disable** Sippy only after confirming all necessary data is in R2
5. **Update** application endpoints gradually during migration

## Related

- [Bucket Management](buckets.md) - Create and manage buckets
- [R2 Object Storage](../../r2/index.md) - Upload and download objects
