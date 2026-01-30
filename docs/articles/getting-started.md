# Getting Started

This guide will help you get up and running with the Cloudflare.NET SDK.

## Prerequisites

- .NET 8.0, .NET 9.0, or .NET 10.0 SDK
- A Cloudflare account with an API token

## Installation

Install the packages you need from NuGet:

### [.NET CLI](#tab/dotnet-cli)

```bash
# Core REST API Client (Required)
dotnet add package Cloudflare.NET.Api

# R2 S3-Compatible Client (Optional)
dotnet add package Cloudflare.NET.R2

# Analytics GraphQL Client (Optional)
dotnet add package Cloudflare.NET.Analytics
```

### [Package Manager](#tab/package-manager)

```powershell
# Core REST API Client (Required)
Install-Package Cloudflare.NET.Api

# R2 S3-Compatible Client (Optional)
Install-Package Cloudflare.NET.R2

# Analytics GraphQL Client (Optional)
Install-Package Cloudflare.NET.Analytics
```

---

## Configuration

The SDK supports two configuration approaches:

1. **Dependency Injection** (recommended for most applications) - Register clients at startup with `AddCloudflareApiClient()`
2. **Dynamic Creation** - Create clients at runtime when configurations aren't known at startup (see [Dynamic Clients](#option-2-dynamic-clients-runtime-creation))

### Dependency Injection

Register the clients in your `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register from IConfiguration (binds to "Cloudflare" and "R2" sections)
builder.Services.AddCloudflareApiClient(builder.Configuration);
builder.Services.AddCloudflareR2Client(builder.Configuration);
builder.Services.AddCloudflareAnalytics();

var app = builder.Build();
```

Alternatively, configure options programmatically:

```csharp
builder.Services.AddCloudflareApiClient(options =>
{
    options.ApiToken = "your-api-token";
    options.AccountId = "your-account-id";
    options.DefaultTimeout = TimeSpan.FromSeconds(30);
    options.RateLimiting.IsEnabled = true;
    options.RateLimiting.EnableProactiveThrottling = true;  // Delay requests when quota is low
    options.RateLimiting.QuotaLowThreshold = 0.1;           // Throttle at 10% remaining
});
```

### appsettings.json

Configure your Cloudflare credentials in `appsettings.json`:

```json
{
  "Cloudflare": {
    "ApiToken": "your-cloudflare-api-token",
    "AccountId": "your-cloudflare-account-id",
    "DefaultTimeout": "00:00:30",
    "RateLimiting": {
      "IsEnabled": true,
      "MaxRetries": 3,
      "PermitLimit": 25,
      "QueueLimit": 10,
      "EnableProactiveThrottling": true,
      "QuotaLowThreshold": 0.1
    }
  },
  "R2": {
    "AccessKeyId": "your-r2-access-key-id",
    "SecretAccessKey": "your-r2-secret-access-key"
  }
}
```

> [!TIP]
> **Proactive Throttling**: When `EnableProactiveThrottling` is enabled (default: `true`), the SDK automatically delays requests when the remaining API quota falls below `QuotaLowThreshold` (default: 10%). This prevents 429 errors before they occur by reading rate limit headers from Cloudflare's responses.

> [!NOTE]
> Never commit API tokens or secrets to source control. Use [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for development and environment variables or a managed Key Vault for production.

## Basic Usage

Inject <xref:Cloudflare.NET.ICloudflareApiClient> into your services:

```csharp
public class DnsService(ICloudflareApiClient cf)
{
    public async Task<DnsRecord?> FindRecordAsync(string zoneId, string hostname)
    {
        return await cf.Zones.FindDnsRecordByNameAsync(zoneId, hostname);
    }

    public async Task CreateCnameAsync(string zoneId, string name, string target)
    {
        await cf.Zones.CreateCnameRecordAsync(zoneId, name, target, proxied: true);
    }

    public async Task PurgeCacheAsync(string zoneId, IEnumerable<string> urls)
    {
        await cf.Zones.PurgeCacheAsync(zoneId, new PurgeCacheRequest
        {
            Files = urls.ToList()
        });
    }
}
```

## Multi-Account Support

For applications managing multiple Cloudflare accounts, you have two options:

### Option 1: Named Clients (Pre-Registered)

Use named clients when account configurations are known at startup:

```csharp
// Register named clients
builder.Services.AddCloudflareApiClient("production", options =>
{
    options.ApiToken = "prod-token";
    options.AccountId = "prod-account-id";
});

builder.Services.AddCloudflareApiClient("staging", options =>
{
    options.ApiToken = "staging-token";
    options.AccountId = "staging-account-id";
});
```

#### Using the Factory

```csharp
public class MultiAccountService(ICloudflareApiClientFactory apiFactory)
{
    public async Task ManageProductionAsync()
    {
        var prodClient = apiFactory.CreateClient("production");
        // Use the production client...
    }
}
```

#### Using Keyed Services (.NET 8+)

```csharp
public class MyService(
    [FromKeyedServices("production")] ICloudflareApiClient prodClient,
    [FromKeyedServices("staging")] ICloudflareApiClient stagingClient)
{
    // Both clients are injected directly
}
```

### Option 2: Dynamic Clients (Runtime Creation)

Use dynamic clients when account configurations are not known at startup, such as when users can add Cloudflare accounts through a UI:

```csharp
public class UserAccountService(ICloudflareApiClientFactory factory)
{
    public async Task<IReadOnlyList<Zone>> GetUserZonesAsync(UserCloudflareCredentials credentials)
    {
        var options = new CloudflareApiOptions
        {
            ApiToken = credentials.ApiToken,
            AccountId = credentials.AccountId,
            RateLimiting = new RateLimitingOptions
            {
                IsEnabled = true,
                PermitLimit = 10  // Conservative limit for user accounts
            }
        };

        // Dynamic clients must be disposed when done
        using var client = factory.CreateClient(options);

        var zones = new List<Zone>();
        await foreach (var zone in client.Zones.ListAllZonesAsync())
        {
            zones.Add(zone);
        }
        return zones;
    }
}
```

> [!IMPORTANT]
> Dynamic clients manage their own `HttpClient` and resilience pipeline. Always dispose them when finished using a `using` statement or by calling `Dispose()`. Each dynamic client has isolated state (rate limiter, circuit breaker) and does not share resources with other clients.

## Error Handling

The SDK throws <xref:Cloudflare.NET.Core.Exceptions.CloudflareApiException> when the API returns an error:

```csharp
try
{
    await cf.Zones.CreateCnameRecordAsync(zoneId, name, target);
}
catch (CloudflareApiException ex)
{
    // API returned success=false
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"[{error.Code}] {error.Message}");
    }
}
catch (HttpRequestException ex)
{
    // Network or HTTP error
    Console.WriteLine($"HTTP Error: {ex.Message}");
}
```

## Next Steps

- [Configuration](configuration.md) - Advanced configuration options
- [Zones API](zones/index.md) - Manage DNS, cache, and custom hostnames
- [Accounts API](accounts/index.md) - Manage R2 buckets and account-level security
- [R2 Object Storage](r2/index.md) - Upload, download, and manage objects
- [Analytics](analytics/index.md) - Query traffic and security metrics
- [API Reference](../api/index.md) - Complete API documentation
