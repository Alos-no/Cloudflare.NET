# Analytics API

The `Cloudflare.NET.Analytics` package provides a typed GraphQL client for querying Cloudflare's Analytics API.

## Installation

```bash
dotnet add package Cloudflare.NET.Analytics
```

## Quick Start

### Registration

```csharp
// Program.cs
builder.Services.AddCloudflareApiClient(builder.Configuration);
builder.Services.AddCloudflareAnalytics();
```

### Basic Usage

Inject <xref:Cloudflare.NET.Analytics.IAnalyticsApi> into your services:

```csharp
public class AnalyticsService(IAnalyticsApi analytics)
{
    public async Task<ZoneTrafficData> GetTrafficAsync(string zoneTag)
    {
        var request = new GraphQLRequest
        {
            Query = """
                query GetZoneTraffic($zoneTag: String!, $since: String!, $until: String!) {
                    viewer {
                        zones(filter: { zoneTag: $zoneTag }) {
                            httpRequests1dGroups(
                                filter: { date_geq: $since, date_lt: $until }
                                limit: 30
                                orderBy: [date_ASC]
                            ) {
                                dimensions { date }
                                sum {
                                    requests
                                    bytes
                                    cachedRequests
                                    cachedBytes
                                }
                            }
                        }
                    }
                }
                """,
            Variables = new
            {
                zoneTag = zoneTag,
                since = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"),
                until = DateTime.UtcNow.ToString("yyyy-MM-dd")
            }
        };

        return await analytics.SendQueryAsync<ZoneTrafficData>(request);
    }
}
```

## API Overview

The <xref:Cloudflare.NET.Analytics.IAnalyticsApi> provides a single method for executing GraphQL queries:

```csharp
public interface IAnalyticsApi
{
    Task<TResponse> SendQueryAsync<TResponse>(
        GraphQLRequest request,
        CancellationToken cancellationToken = default);
}
```

## GraphQL Request

Create requests using the `GraphQL.GraphQLRequest` class:

```csharp
var request = new GraphQLRequest
{
    Query = "query { ... }",      // GraphQL query string
    Variables = new { ... },       // Query variables
    OperationName = "MyQuery"      // Optional operation name
};
```

## Response Types

Define strongly-typed response classes matching your query structure:

```csharp
public record ZoneTrafficData(Viewer Viewer);

public record Viewer(IReadOnlyList<Zone> Zones);

public record Zone(IReadOnlyList<HttpRequestsGroup> HttpRequests1dGroups);

public record HttpRequestsGroup(
    Dimensions Dimensions,
    RequestSum Sum
);

public record Dimensions(string Date);

public record RequestSum(
    long Requests,
    long Bytes,
    long CachedRequests,
    long CachedBytes
);
```

## Error Handling

```csharp
try
{
    var result = await analytics.SendQueryAsync<MyResponse>(request);
}
catch (InvalidOperationException ex)
{
    // GraphQL errors or null response
    Console.WriteLine($"Query failed: {ex.Message}");
}
```

## Available Datasets

Cloudflare Analytics provides various datasets:

| Dataset | Granularity | Description |
|---------|-------------|-------------|
| `httpRequests1dGroups` | Daily | HTTP request metrics |
| `httpRequests1hGroups` | Hourly | Detailed HTTP metrics |
| `httpRequests1mGroups` | Minute | Real-time HTTP metrics |
| `firewallEventsAdaptiveGroups` | Adaptive | WAF event data |
| `workersAnalyticsEngineAdaptiveGroups` | Adaptive | Workers analytics |
| `r2StorageAdaptiveGroups` | Adaptive | R2 storage metrics |

## Features

| Feature | Description |
|---------|-------------|
| **Strongly-typed** | Define response types for compile-time safety |
| **Variables** | Support for parameterized queries |
| **Authentication** | Automatic token handling via shared API client |
| **Error handling** | Exceptions for GraphQL errors |

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Analytics | Zone or Account | Read |

## Limitations

> [!NOTE]
> The `Cloudflare.NET.Analytics` package cannot be strong-named because its dependency (`GraphQL.Client`) is not strong-named.

## Related

- [GraphQL Queries](graphql.md) - Query examples and patterns
- [Cloudflare GraphQL API Docs](https://developers.cloudflare.com/analytics/graphql-api/)
