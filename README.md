# Cloudflare.NET SDK

[![.NET Build and Test](https://github.com/Alos-no/Cloudflare.NET/actions/workflows/CI.yml/badge.svg)](https://github.com/Alos-no/Cloudflare.NET/actions/workflows/CI.yml)
[![License: Apache-2.0](https://img.shields.io/badge/license-Apache--2.0-blue)](https://github.com/Alos-no/Cloudflare.NET/blob/main/LICENSE.txt)

[![NuGet (Cloudflare.NET.Api)](https://img.shields.io/nuget/v/Cloudflare.NET.Api?label=Cloudflare.NET.Api)](https://www.nuget.org/packages/Cloudflare.NET.Api/)
[![NuGet (Cloudflare.NET.R2)](https://img.shields.io/nuget/v/Cloudflare.NET.R2?label=Cloudflare.NET.R2)](https://www.nuget.org/packages/Cloudflare.NET.R2/)
[![NuGet (Cloudflare.NET.Analytics)](https://img.shields.io/nuget/v/Cloudflare.NET.Analytics?label=Cloudflare.NET.Analytics)](https://www.nuget.org/packages/Cloudflare.NET.Analytics/)


This is an unofficial .NET SDK for the Cloudflare API. Its primary goal is to provide a strongly-typed, testable, and maintainable library for interacting with various Cloudflare services. The core SDK is lean and focuses on the REST API, while optional functionality, like the Analytics GraphQL API, is provided in a separate extension package.

## Supported Frameworks

| Package | .NET 8 | .NET 9 | .NET 10 | Strong Named |
|---------|:------:|:------:|:-------:|:------------:|
| **Cloudflare.NET.Api** | ✅ | ✅ | ✅ | ✅ |
| **Cloudflare.NET.R2** | ✅ | ✅ | ✅ | ✅ |
| **Cloudflare.NET.Analytics** | ✅ | ✅ | ✅ | ❌* |

> \* `Cloudflare.NET.Analytics` cannot be strong-named because its dependency (`GraphQL.Client`) is not strong-named.

## 1. Installation

The SDK is split into multiple packages. Install the one(s) you need from NuGet.

**Core REST API Client (Required):**
```powershell
Install-Package Cloudflare.NET.Api
```

**R2 S3-Compatible Client (Optional):**
```powershell
Install-Package Cloudflare.NET.R2
```

**Analytics GraphQL Client (Optional):**
```powershell
Install-Package Cloudflare.NET.Analytics
```

---

## 2. Quick Start

### 2.1 Configuration

First, configure your secrets in `appsettings.json` or through user secrets / environment variables.

```json
{
  "Cloudflare": {
    "ApiToken": "your-cloudflare-api-token",
    "AccountId": "your-cloudflare-account-id",
    // Optional
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

### 2.2 Dependency Injection

Register the client(s) in your `Program.cs` or `Startup.cs`.

**Option A: Configuration-based (from `appsettings.json`)**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Register from IConfiguration (binds to "Cloudflare" and "R2" sections)
builder.Services.AddCloudflareApiClient(builder.Configuration);
builder.Services.AddCloudflareR2Client(builder.Configuration);
builder.Services.AddCloudflareAnalytics();
```

**Option B: Code-based (programmatic configuration)**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure options directly in code
builder.Services.AddCloudflareApiClient(options =>
{
    options.ApiToken = "your-api-token";
    options.AccountId = "your-account-id";
    options.DefaultTimeout = TimeSpan.FromSeconds(30);
    options.RateLimiting.IsEnabled = true;
    options.RateLimiting.MaxRetries = 3;
});

builder.Services.AddCloudflareR2Client(options =>
{
    options.AccessKeyId = "your-r2-access-key";
    options.SecretAccessKey = "your-r2-secret";
    // EndpointUrl defaults to "https://{0}.r2.cloudflarestorage.com"
});

builder.Services.AddCloudflareAnalytics();
```

### 2.3 Named Clients (Multi-Account Support)

For applications managing multiple Cloudflare accounts, use **named clients**. Each named client has its own configuration and can be retrieved via a factory or keyed services.

**Registration:**
```csharp
// Register multiple named clients
builder.Services.AddCloudflareApiClient("production", options =>
{
    options.ApiToken = "prod-token";
    options.AccountId = "prod-account-id";
});
builder.Services.AddCloudflareR2Client("production", options =>
{
    options.AccessKeyId = "prod-r2-key";
    options.SecretAccessKey = "prod-r2-secret";
});
builder.Services.AddCloudflareAnalytics("production");

builder.Services.AddCloudflareApiClient("staging", options =>
{
    options.ApiToken = "staging-token";
    options.AccountId = "staging-account-id";
});
builder.Services.AddCloudflareR2Client("staging", options =>
{
    options.AccessKeyId = "staging-r2-key";
    options.SecretAccessKey = "staging-r2-secret";
});
builder.Services.AddCloudflareAnalytics("staging");
```

**Usage via Factory:**
```csharp
public class MultiAccountService(
    ICloudflareApiClientFactory apiFactory,
    IR2ClientFactory r2Factory,
    IAnalyticsApiFactory analyticsFactory)
{
    public async Task ManageProductionAsync()
    {
        var prodApi = apiFactory.CreateClient("production");
        var prodR2 = r2Factory.CreateClient("production");
        var prodAnalytics = analyticsFactory.CreateClient("production");
        
        // Use clients...
    }
}
```

**Usage via Keyed Services (.NET 8+):**
```csharp
public class MyService(
    [FromKeyedServices("production")] ICloudflareApiClient prodClient,
    [FromKeyedServices("staging")] ICloudflareApiClient stagingClient)
{
    // Both clients are injected directly
}
```

**Configuration-based Named Clients:**

You can also configure named clients via `appsettings.json`:
```json
{
  "Cloudflare:production": {
    "ApiToken": "prod-token",
    "AccountId": "prod-account-id"
  },
  "Cloudflare:staging": {
    "ApiToken": "staging-token",
    "AccountId": "staging-account-id"
  },
  "R2:production": {
    "AccessKeyId": "prod-r2-key",
    "SecretAccessKey": "prod-r2-secret"
  }
}
```
```csharp
builder.Services.AddCloudflareApiClient("production", builder.Configuration);
builder.Services.AddCloudflareApiClient("staging", builder.Configuration);
builder.Services.AddCloudflareR2Client("production", builder.Configuration);
```

### 2.4 Usage Example

Inject the client interfaces and use them in your services.

```csharp
public class MyCloudflareService(ICloudflareApiClient cf, IAnalyticsApi analytics)
{
    // Example: Using the REST API client to manage DNS
    public async Task<DnsRecord?> FindDnsRecordAsync(string zoneId, string hostname)
    {
        // Use the strongly-typed Zones API
        return await cf.Zones.FindDnsRecordByNameAsync(zoneId, hostname);
    }

    // Example: Using the GraphQL client to get R2 analytics
    public async Task<long> GetTotalR2ObjectCountAsync(string accountId)
    {
        var request = new GraphQLRequest
        {
            Query = @"
                query GetStorage($account: String!) {
                    viewer {
                        accounts(filter: { accountTag: $account }) {
                            r2StorageAdaptiveGroups(limit: 1) {
                                max { objectCount }
                            }
                        }
                    }
                }",
            Variables = new { account = accountId }
        };

        // Send the query and get back a strongly-typed response model
        var response = await analytics.SendQueryAsync<GraphQLResponse>(request);
        
        return response.Accounts.FirstOrDefault()?
            .Storage.FirstOrDefault()?
            .Max?.ObjectCount ?? 0;
    }
}
```
---

## 3. API Coverage & Roadmap

### 3.1 Implemented API Resources

| API Family | Endpoint Group | Status | Purpose |
| :--- | :--- | :--- | :--- |
| **Accounts** | R2 Buckets | ✅ **Implemented** | `POST /accounts/{id}/r2/buckets`, `DELETE .../{name}` |
| **Accounts** | R2 Custom Domains | ✅ **Implemented** | `POST .../domains/custom`, `PUT .../domains/managed` |
| **Accounts** | IP Access Rules | ✅ **Implemented** | `GET, POST, PATCH, DELETE /accounts/{id}/firewall/access_rules/rules` |
| **Accounts** | Rulesets (WAF) | ✅ **Implemented** | `GET, POST, PUT, DELETE /accounts/{id}/rulesets`, including phase entrypoints and rule management. |
| **Zones** | DNS Records | ✅ **Implemented** | `GET, POST, DELETE /zones/{id}/dns_records` |
| **Zones** | DNS Records (Bulk) | ✅ **Implemented** | `POST .../import`, `GET .../export` |
| **Zones** | Cache Purge | ✅ **Implemented** | `POST /zones/{id}/purge_cache` |
| **Zones** | Zone Details | ✅ **Implemented** | `GET /zones/{id}` |
| **Zones** | IP Access Rules | ✅ **Implemented** | `GET, POST, PATCH, DELETE /zones/{id}/firewall/access_rules/rules` |
| **Zones** | Zone Lockdown | ✅ **Implemented** | `GET, POST, PUT, DELETE /zones/{id}/firewall/lockdowns` |
| **Zones** | User-Agent Rules | ✅ **Implemented** | `GET, POST, PUT, DELETE /zones/{id}/firewall/ua_rules` |
| **Zones** | Rulesets (WAF) | ✅ **Implemented** | `GET, POST, PUT, DELETE /zones/{id}/rulesets`, including phase entrypoints and rule management. |
| **Zones** | Custom Hostnames (SaaS) | ✅ **Implemented** | `GET, POST, PATCH, DELETE /zones/{id}/custom_hostnames`, including fallback origin management. |
| **Analytics** | GraphQL API | ✅ **Implemented** | **(Extension Package)** Provides a generic GraphQL client. |
| **R2 Client** | S3-Compatible API | ✅ **Implemented** | **(Extension Package)** Provides a high-level client for object storage operations. |

### 3.2 Planned API Resources (Roadmap)

The following API surfaces are planned for implementation to support advanced use cases.

| API Family / Endpoint Group | Use Case | Notable Paths |
| :--- | :--- | :--- |
| **SSL & Certificates (mTLS)** | Securing endpoints with Mutual TLS (mTLS). | `GET /zones/{zoneId}/client_certificates` |
| **R2 Object Metadata** | Manage advanced, Cloudflare-specific object metadata. | `GET /accounts/{...}/r2/buckets/{...}/objects` |
| **User & Tokens** | Automated API token management and permission auditing. | `GET /user/permissions`, `GET /user/tokens` |

---

## 4. API Token Permissions

To adhere to the principle of least privilege, create a Cloudflare API token with only the scopes your application requires.

**Note**: The R2 S3-compatible client (`Cloudflare.NET.R2`) does **not** use an API token; it requires separate R2 credentials (Access Key ID and Secret). The permissions below apply to the REST and GraphQL APIs.

| Permission Group (UI Label) | Scope | Level | Typical Uses |
| :--- | :--- | :--- | :--- |
| **Workers R2 Storage** | Account | **Write** | Create/delete R2 buckets, manage domains (`Cloudflare.NET.Api`). |
| **Workers R2 Storage** | Account | **Read** | List buckets and read configurations (`Cloudflare.NET.Api`). |
| **Account Firewall Access Rules** | Account | **Write** | Programmatically manage IP Access Rules at the account level. |
| **Account Firewall Access Rules** | Account | **Read** | Audit and list existing firewall rules at the account level. |
| **Firewall Services** | Zone | **Write** | Programmatically manage IP Access Rules at the zone level. |
| **Account Rulesets** | Account | **Write** | Deploy and manage WAF custom rulesets at the account level. |
| **Account Rulesets** | Account | **Read** | List and audit WAF custom rulesets at the account level. |
| **Zone WAF** | Zone | **Write** | Deploy and manage WAF custom rulesets at the zone level. |
| **Zone WAF** | Zone | **Read** | List and audit WAF custom rulesets at the zone level. |
| **DNS** | Zone | **Write** | Automate DNS changes, including bulk import/export. |
| **DNS** | Zone | **Read** | List, scan, and verify DNS state before migrations. |
| **Cache Purge** | Zone | **Purge** | Purge the cache via the API. |
| **SSL and Certificates** | Zone | **Write** | Automate client certificate lifecycle management. Cloudflare SaaS. |
| **SSL and Certificates** | Zone | **Read** | Monitor and report on client certificate status. Cloudflare SaaS. |
| **Account Analytics** | Account | **Read** | Query R2 usage and other datasets via GraphQL. |
| **User API-tokens** | User | **Read** | Build applications that can inspect their own token permissions. |
