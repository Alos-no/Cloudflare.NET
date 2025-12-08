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
| `EndpointUrl` | `string` | `https://{0}.r2.cloudflarestorage.com` | R2 endpoint URL template |
| `Region` | `string` | `auto` | AWS region (always "auto" for R2) |

> [!NOTE]
> The R2 client uses separate credentials from the REST API. You need to create R2 API tokens in the Cloudflare dashboard under R2 > Manage R2 API Tokens.

### R2 Configuration Example

```json
{
  "Cloudflare": {
    "AccountId": "your-cloudflare-account-id"
  },
  "R2": {
    "AccessKeyId": "your-r2-access-key-id",
    "SecretAccessKey": "your-r2-secret-access-key",
    "EndpointUrl": "https://{0}.r2.cloudflarestorage.com",
    "Region": "auto"
  }
}
```

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

For multi-account scenarios, use named clients with separate configurations:

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
