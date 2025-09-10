# Cloudflare.NET.Analytics

This package is an extension for `Cloudflare.NET` that provides a client for interacting with the Cloudflare Analytics GraphQL API. It simplifies sending GraphQL queries and deserializing the results into strongly-typed C# records.

## 1. Installation

Install the package from NuGet:

```bash
dotnet add package Cloudflare.NET.Analytics
```

This package has a dependency on `Cloudflare.NET`, which will be installed automatically.

## 2. API Token Permissions

To use the Analytics GraphQL API, your Cloudflare API Token needs the following permission:

*   **Account Analytics**: `Read`

## 3. Usage

### 3.1 Configuration

In your `appsettings.json`, ensure your core Cloudflare settings are configured. The Analytics client uses the same `ApiToken` for authentication.

```json
{
  "Cloudflare": {
    "ApiToken": "your-cloudflare-api-token",
    "AccountId": "your-cloudflare-account-id"
    // The GraphQlApiUrl is optional and defaults to the standard endpoint.
    // "GraphQlApiUrl": "https://api.cloudflare.com/client/v4/graphql"
  }
}
```

### 3.2 Register Services

In your application's service configuration (e.g., `Program.cs`), register both the core Cloudflare client and the Analytics client. The Analytics client depends on the core client for authentication details.

```csharp
using Cloudflare.NET.Core;
using Cloudflare.NET.Analytics;

var builder = WebApplication.CreateBuilder(args);

// 1. Register the core Cloudflare client (required for authentication)
builder.Services.AddCloudflareApiClient(builder.Configuration);

// 2. Register the Analytics client
builder.Services.AddCloudflareAnalytics();

// ... rest of your service configuration
```

### 3.3 Example: Querying R2 Analytics

Inject `IAnalyticsApi` into your services. You can use it to send any valid GraphQL query to the Cloudflare API. The SDK provides response models that match the structure of the R2 analytics datasets.

```csharp
using Cloudflare.NET.Analytics;
using Cloudflare.NET.Analytics.Models;
using Cloudflare.NET.Core;
using GraphQL;
using Microsoft.Extensions.Options;

public class MyAnalyticsService(IAnalyticsApi analyticsApi, IOptions<CloudflareApiOptions> options)
{
    public async Task<long> GetTotalR2ObjectCount()
    {
        var accountId = options.Value.AccountId;
        var request = new GraphQLRequest
        {
            Query = @"
                query GetR2Storage($accountTag: String!, $startTime: Time!, $endTime: Time!) {
                  viewer {
                    accounts(filter: { accountTag: $accountTag }) {
                      r2StorageAdaptiveGroups(
                        limit: 1,
                        filter: { datetime_geq: $startTime, datetime_leq: $endTime },
                        orderBy: [datetime_DESC]
                      ) {
                        max { objectCount }
                      }
                    }
                  }
                }",
            Variables = new
            {
                accountTag = accountId,
                startTime = DateTime.UtcNow.AddDays(-1),
                endTime = DateTime.UtcNow
            }
        };

        // Use the GraphQLResponse and nested types from the SDK
        var result = await analyticsApi.SendQueryAsync<GraphQLResponse>(request);

        // Safely navigate the response structure
        return result.Viewer.Accounts
            .FirstOrDefault()?
            .Storage.FirstOrDefault()?
            .Max?.ObjectCount ?? 0;
    }
}
```