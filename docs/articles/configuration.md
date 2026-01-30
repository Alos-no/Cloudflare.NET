# Configuration

This guide covers all configuration options available in the Cloudflare.NET SDK.

## Core API Client Options

The `CloudflareApiOptions` class provides the following configuration:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ApiToken` | `string` | *Required* | Your Cloudflare API token |
| `AccountId` | `string` | *Required* | Your Cloudflare account ID |
| `ApiBaseUrl` | `string` | `https://api.cloudflare.com/client/v4` | Base URL for the Cloudflare API |
| `DefaultTimeout` | `TimeSpan` | 30 seconds | Default timeout for API requests |
| `RateLimiting` | `RateLimitingOptions` | See below | Rate limiting configuration |

### Rate Limiting Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `IsEnabled` | `bool` | `true` | Enable client-side rate limiting |
| `MaxRetries` | `int` | `3` | Maximum retry attempts for failed requests |
| `PermitLimit` | `int` | `25` | Maximum concurrent requests |
| `QueueLimit` | `int` | `10` | Maximum queued requests when at limit |

### Configuration Example

```json
{
  "Cloudflare": {
    "ApiToken": "your-cloudflare-api-token",
    "AccountId": "your-cloudflare-account-id",
    "ApiBaseUrl": "https://api.cloudflare.com/client/v4",
    "DefaultTimeout": "00:00:30",
    "RateLimiting": {
      "IsEnabled": true,
      "MaxRetries": 3,
      "PermitLimit": 25,
      "QueueLimit": 10
    }
  }
}
```

## R2 Client Options

The `R2Settings` class provides configuration for the S3-compatible R2 client:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AccessKeyId` | `string` | *Required* | R2 Access Key ID |
| `SecretAccessKey` | `string` | *Required* | R2 Secret Access Key |
| `Jurisdiction` | `R2Jurisdiction` | `Default` | Target jurisdiction (determines S3 endpoint) |
| `EndpointUrl` | `string` | *Auto-computed* | R2 endpoint URL template (overrides `Jurisdiction` if set) |
| `Region` | `string` | `auto` | AWS region (always "auto" for R2) |

> [!NOTE]
> The R2 client uses separate credentials from the REST API. You need to create R2 API tokens in the Cloudflare dashboard under R2 > Manage R2 API Tokens.

### Jurisdiction to Endpoint Mapping

| Jurisdiction | JSON Value | S3 Endpoint |
|--------------|------------|-------------|
| Default | `"default"` | `https://{account_id}.r2.cloudflarestorage.com` |
| European Union | `"eu"` | `https://{account_id}.eu.r2.cloudflarestorage.com` |
| FedRAMP | `"fedramp"` | `https://{account_id}.fedramp.r2.cloudflarestorage.com` |

> [!TIP]
> When `Jurisdiction` is set and `EndpointUrl` is not specified, the endpoint is automatically computed from the jurisdiction. If you explicitly set `EndpointUrl`, it takes precedence over `Jurisdiction`.

### R2 Configuration Example

```json
{
  "Cloudflare": {
    "AccountId": "your-cloudflare-account-id"
  },
  "R2": {
    "AccessKeyId": "your-r2-access-key-id",
    "SecretAccessKey": "your-r2-secret-access-key"
  }
}
```

### R2 with Jurisdiction

```json
{
  "Cloudflare": {
    "AccountId": "your-cloudflare-account-id"
  },
  "R2": {
    "AccessKeyId": "your-r2-access-key-id",
    "SecretAccessKey": "your-r2-secret-access-key",
    "Jurisdiction": "eu"
  }
}
```

### Named R2 Clients (Multi-Account)

For multi-account R2 scenarios, register named clients with separate configurations:

**JSON Configuration:**

```json
{
  "Cloudflare": {
    "AccountId": "primary-account-id"
  },
  "Cloudflare:backup": {
    "AccountId": "backup-account-id"
  },
  "R2": {
    "AccessKeyId": "primary-r2-key",
    "SecretAccessKey": "primary-r2-secret"
  },
  "R2:backup": {
    "AccessKeyId": "backup-r2-key",
    "SecretAccessKey": "backup-r2-secret",
    "Jurisdiction": "eu"
  }
}
```

```csharp
// Program.cs - Register named clients from configuration
builder.Services.AddCloudflareR2Client(builder.Configuration);              // Default client
builder.Services.AddCloudflareR2Client("backup", builder.Configuration);    // Named "backup" client
```

**Code-Based Configuration:**

```csharp
// Program.cs - Register with inline configuration
builder.Services.AddCloudflareR2Client(options =>
{
    options.AccessKeyId = "primary-key";
    options.SecretAccessKey = "primary-secret";
});

builder.Services.AddCloudflareR2Client("eu-storage", options =>
{
    options.AccessKeyId = "eu-key";
    options.SecretAccessKey = "eu-secret";
    options.Jurisdiction = R2Jurisdiction.EuropeanUnion;
});
```

**Usage with IR2ClientFactory:**

```csharp
public class StorageService(IR2ClientFactory factory)
{
    public async Task ReplicateAsync(string bucket, string key, Stream data)
    {
        // Upload to primary account (default client)
        var primaryClient = factory.GetClient(R2Jurisdiction.Default);
        await primaryClient.UploadAsync(bucket, key, data);

        // Upload to backup account (named client)
        data.Position = 0;
        var backupClient = factory.GetClient("backup");
        await backupClient.UploadAsync("backup-bucket", key, data);
    }

    // Override jurisdiction for a named client
    public async Task UploadToBackupEuAsync(string key, Stream data)
    {
        var client = factory.GetClient("backup", R2Jurisdiction.EuropeanUnion);
        await client.UploadAsync("eu-bucket", key, data);
    }
}
```

> [!TIP]
> The same R2 credentials work across all jurisdictions within an account—only the S3 endpoint differs. Use `factory.GetClient(name, jurisdiction)` to override the configured jurisdiction at runtime.

## Resilience Pipeline

The SDK includes a built-in resilience pipeline powered by [Polly](https://github.com/App-vNext/Polly):

1. **Rate Limiter** - Client-side concurrency control
2. **Total Timeout** - 60 seconds for entire operation including retries
3. **Retry** - Exponential backoff with jitter; honors `Retry-After` header
4. **Circuit Breaker** - Stops requests after consecutive failures
5. **Attempt Timeout** - Per-request timeout from `DefaultTimeout`

### Customizing Resilience

The resilience pipeline is configured automatically when using `AddCloudflareApiClient()`. The default settings are designed for most use cases, but you can adjust the rate limiting options to match your Cloudflare plan limits.

## Named Clients

For multi-account scenarios where configurations are known at startup, use named clients:

```csharp
// appsettings.json
{
  "Cloudflare:production": {
    "ApiToken": "prod-token",
    "AccountId": "prod-account-id"
  },
  "Cloudflare:staging": {
    "ApiToken": "staging-token",
    "AccountId": "staging-account-id"
  }
}

// Program.cs
builder.Services.AddCloudflareApiClient("production", builder.Configuration);
builder.Services.AddCloudflareApiClient("staging", builder.Configuration);
```

## Dynamic Clients

For scenarios where client configurations are not known at startup (e.g., desktop applications where users add accounts at runtime), use dynamic client creation:

```csharp
public class AccountManager(ICloudflareApiClientFactory factory)
{
    public async Task<AccountInfo> ValidateAndGetAccountInfoAsync(string apiToken, string accountId)
    {
        var options = new CloudflareApiOptions
        {
            ApiToken = apiToken,
            AccountId = accountId,
            DefaultTimeout = TimeSpan.FromSeconds(15),
            RateLimiting = new RateLimitingOptions
            {
                IsEnabled = true,
                PermitLimit = 5,      // Conservative for user-provided credentials
                MaxRetries = 2
            }
        };

        // Create and use a dynamic client
        using var client = factory.CreateClient(options);

        var user = await client.User.GetUserAsync();
        return new AccountInfo(user.Email, accountId);
    }
}
```

### Dynamic Client Characteristics

| Aspect | Named Clients | Dynamic Clients |
|--------|---------------|-----------------|
| Configuration | At startup via DI | At runtime via `CloudflareApiOptions` |
| Lifecycle | Managed by DI container | Must be disposed by caller |
| HttpClient | Shared via `IHttpClientFactory` | Owned by the client instance |
| Resilience Pipeline | Shared per named client | Isolated per instance |
| Use Case | Known accounts, server apps | User-provided accounts, desktop apps |

### Disposal Requirements

Dynamic clients implement `IDisposable` and **must** be disposed when no longer needed:

```csharp
// Option 1: using statement (recommended)
using var client = factory.CreateClient(options);
await client.Zones.ListZonesAsync();
// Client automatically disposed at end of scope

// Option 2: using declaration in async methods
using var client = factory.CreateClient(options);
var zones = await client.Zones.ListZonesAsync();
// ... more operations
// Client disposed when method returns

// Option 3: Manual disposal (for longer-lived clients)
var client = factory.CreateClient(options);
try
{
    // Use client for multiple operations...
}
finally
{
    client.Dispose();
}
```

> [!WARNING]
> Failing to dispose dynamic clients will leak `HttpClient` instances and their underlying socket connections. Always use a `using` statement or explicitly call `Dispose()`.

## Environment Variables

Configuration can also be provided via environment variables using the `__` (double underscore) convention:

```bash
export Cloudflare__ApiToken="your-api-token"
export Cloudflare__AccountId="your-account-id"
export R2__AccessKeyId="your-r2-access-key"
export R2__SecretAccessKey="your-r2-secret"
```

## User Secrets (Development)

For local development, use .NET User Secrets:

```bash
dotnet user-secrets init
dotnet user-secrets set "Cloudflare:ApiToken" "your-api-token"
dotnet user-secrets set "Cloudflare:AccountId" "your-account-id"
```
