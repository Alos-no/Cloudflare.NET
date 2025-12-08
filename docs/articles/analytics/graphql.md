# GraphQL Queries

Examples and patterns for querying Cloudflare Analytics with GraphQL.

## Query Structure

Cloudflare Analytics queries follow this structure:

```graphql
query {
    viewer {
        zones(filter: { zoneTag: "your-zone-id" }) {
            # Zone-level analytics
        }
        accounts(filter: { accountTag: "your-account-id" }) {
            # Account-level analytics
        }
    }
}
```

## Zone Analytics

### HTTP Requests (Daily)

```csharp
public async Task<DailyTraffic> GetDailyTrafficAsync(string zoneTag, int days = 30)
{
    var request = new GraphQLRequest
    {
        Query = """
            query DailyTraffic($zoneTag: String!, $since: String!, $until: String!) {
                viewer {
                    zones(filter: { zoneTag: $zoneTag }) {
                        httpRequests1dGroups(
                            filter: { date_geq: $since, date_lt: $until }
                            limit: 100
                            orderBy: [date_ASC]
                        ) {
                            dimensions { date }
                            sum {
                                requests
                                bytes
                                cachedRequests
                                cachedBytes
                                pageViews
                            }
                            uniq {
                                uniques
                            }
                        }
                    }
                }
            }
            """,
        Variables = new
        {
            zoneTag,
            since = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd"),
            until = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd")
        }
    };

    return await analytics.SendQueryAsync<DailyTraffic>(request);
}
```

### HTTP Requests (Hourly)

```csharp
public async Task<HourlyTraffic> GetHourlyTrafficAsync(string zoneTag, int hours = 24)
{
    var since = DateTime.UtcNow.AddHours(-hours);

    var request = new GraphQLRequest
    {
        Query = """
            query HourlyTraffic($zoneTag: String!, $since: DateTime!, $until: DateTime!) {
                viewer {
                    zones(filter: { zoneTag: $zoneTag }) {
                        httpRequests1hGroups(
                            filter: { datetime_geq: $since, datetime_lt: $until }
                            limit: 100
                            orderBy: [datetime_ASC]
                        ) {
                            dimensions { datetime }
                            sum {
                                requests
                                bytes
                            }
                        }
                    }
                }
            }
            """,
        Variables = new
        {
            zoneTag,
            since = since.ToString("O"),
            until = DateTime.UtcNow.ToString("O")
        }
    };

    return await analytics.SendQueryAsync<HourlyTraffic>(request);
}
```

### Requests by Country

```csharp
public async Task<CountryTraffic> GetTrafficByCountryAsync(string zoneTag, int days = 7)
{
    var request = new GraphQLRequest
    {
        Query = """
            query TrafficByCountry($zoneTag: String!, $since: String!, $until: String!) {
                viewer {
                    zones(filter: { zoneTag: $zoneTag }) {
                        httpRequests1dGroups(
                            filter: { date_geq: $since, date_lt: $until }
                            limit: 100
                            orderBy: [sum_requests_DESC]
                        ) {
                            dimensions { clientCountryName }
                            sum { requests bytes }
                        }
                    }
                }
            }
            """,
        Variables = new
        {
            zoneTag,
            since = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd"),
            until = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd")
        }
    };

    return await analytics.SendQueryAsync<CountryTraffic>(request);
}
```

### Cache Performance

```csharp
public async Task<CacheStats> GetCacheStatsAsync(string zoneTag, int days = 30)
{
    var request = new GraphQLRequest
    {
        Query = """
            query CacheStats($zoneTag: String!, $since: String!, $until: String!) {
                viewer {
                    zones(filter: { zoneTag: $zoneTag }) {
                        httpRequests1dGroups(
                            filter: { date_geq: $since, date_lt: $until }
                            limit: 1
                        ) {
                            sum {
                                requests
                                cachedRequests
                                bytes
                                cachedBytes
                            }
                        }
                    }
                }
            }
            """,
        Variables = new
        {
            zoneTag,
            since = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd"),
            until = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd")
        }
    };

    return await analytics.SendQueryAsync<CacheStats>(request);
}
```

## Security Analytics

### Firewall Events

```csharp
public async Task<FirewallEvents> GetFirewallEventsAsync(string zoneTag, int hours = 24)
{
    var since = DateTime.UtcNow.AddHours(-hours);

    var request = new GraphQLRequest
    {
        Query = """
            query FirewallEvents($zoneTag: String!, $since: DateTime!, $until: DateTime!) {
                viewer {
                    zones(filter: { zoneTag: $zoneTag }) {
                        firewallEventsAdaptiveGroups(
                            filter: { datetime_geq: $since, datetime_lt: $until }
                            limit: 100
                            orderBy: [count_DESC]
                        ) {
                            dimensions {
                                action
                                clientCountryName
                                clientASNDescription
                            }
                            count
                        }
                    }
                }
            }
            """,
        Variables = new
        {
            zoneTag,
            since = since.ToString("O"),
            until = DateTime.UtcNow.ToString("O")
        }
    };

    return await analytics.SendQueryAsync<FirewallEvents>(request);
}
```

### Threats Blocked

```csharp
public async Task<ThreatsBlocked> GetThreatsBlockedAsync(string zoneTag, int days = 7)
{
    var request = new GraphQLRequest
    {
        Query = """
            query ThreatsBlocked($zoneTag: String!, $since: String!, $until: String!) {
                viewer {
                    zones(filter: { zoneTag: $zoneTag }) {
                        httpRequests1dGroups(
                            filter: { date_geq: $since, date_lt: $until }
                            limit: 100
                            orderBy: [date_ASC]
                        ) {
                            dimensions { date }
                            sum { threats }
                        }
                    }
                }
            }
            """,
        Variables = new
        {
            zoneTag,
            since = DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd"),
            until = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd")
        }
    };

    return await analytics.SendQueryAsync<ThreatsBlocked>(request);
}
```

## Response Models

### Basic Traffic Response

```csharp
public record DailyTraffic(Viewer Viewer);

public record Viewer(IReadOnlyList<Zone> Zones);

public record Zone(IReadOnlyList<HttpRequestsGroup> HttpRequests1dGroups);

public record HttpRequestsGroup(
    DateDimensions Dimensions,
    TrafficSum Sum,
    UniquesCount? Uniq
);

public record DateDimensions(string Date);

public record TrafficSum(
    long Requests,
    long Bytes,
    long CachedRequests,
    long CachedBytes,
    long? PageViews,
    long? Threats
);

public record UniquesCount(long Uniques);
```

### Country Traffic Response

```csharp
public record CountryTraffic(Viewer Viewer);

public record CountryGroup(
    CountryDimensions Dimensions,
    RequestSum Sum
);

public record CountryDimensions(string ClientCountryName);

public record RequestSum(long Requests, long Bytes);
```

### Firewall Response

```csharp
public record FirewallEvents(Viewer Viewer);

public record FirewallGroup(
    FirewallDimensions Dimensions,
    long Count
);

public record FirewallDimensions(
    string Action,
    string ClientCountryName,
    string? ClientASNDescription
);
```

## Common Patterns

### Dashboard Service

```csharp
public class DashboardService(IAnalyticsApi analytics)
{
    public async Task<DashboardData> GetDashboardAsync(string zoneTag)
    {
        var request = new GraphQLRequest
        {
            Query = """
                query Dashboard($zoneTag: String!, $since: String!, $until: String!) {
                    viewer {
                        zones(filter: { zoneTag: $zoneTag }) {
                            # Traffic overview
                            trafficOverview: httpRequests1dGroups(
                                filter: { date_geq: $since, date_lt: $until }
                                limit: 1
                            ) {
                                sum { requests bytes cachedRequests cachedBytes }
                                uniq { uniques }
                            }
                            # Daily trend
                            dailyTrend: httpRequests1dGroups(
                                filter: { date_geq: $since, date_lt: $until }
                                limit: 30
                                orderBy: [date_ASC]
                            ) {
                                dimensions { date }
                                sum { requests }
                            }
                            # Top countries
                            topCountries: httpRequests1dGroups(
                                filter: { date_geq: $since, date_lt: $until }
                                limit: 10
                                orderBy: [sum_requests_DESC]
                            ) {
                                dimensions { clientCountryName }
                                sum { requests }
                            }
                        }
                    }
                }
                """,
            Variables = new
            {
                zoneTag,
                since = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd"),
                until = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd")
            }
        };

        return await analytics.SendQueryAsync<DashboardData>(request);
    }
}
```

### Caching Results

```csharp
public class CachedAnalyticsService(IAnalyticsApi analytics, IMemoryCache cache)
{
    public async Task<DailyTraffic> GetCachedTrafficAsync(string zoneTag)
    {
        var cacheKey = $"traffic:{zoneTag}";

        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            return await GetDailyTrafficAsync(zoneTag);
        })!;
    }

    private async Task<DailyTraffic> GetDailyTrafficAsync(string zoneTag)
    {
        // Implementation from above...
    }
}
```

## Filter Reference

### Date Filters

| Filter | Type | Example |
|--------|------|---------|
| `date` | String | `"2024-01-15"` |
| `date_geq` | String | Greater than or equal |
| `date_gt` | String | Greater than |
| `date_leq` | String | Less than or equal |
| `date_lt` | String | Less than |

### DateTime Filters

| Filter | Type | Example |
|--------|------|---------|
| `datetime_geq` | DateTime | `"2024-01-15T00:00:00Z"` |
| `datetime_lt` | DateTime | Less than |

### Common Filters

| Filter | Description |
|--------|-------------|
| `clientCountryName` | Two-letter country code |
| `clientASNDescription` | ASN name |
| `action` | Firewall action taken |

## Order By Options

| Field | Description |
|-------|-------------|
| `date_ASC` / `date_DESC` | Order by date |
| `datetime_ASC` / `datetime_DESC` | Order by datetime |
| `sum_requests_ASC` / `sum_requests_DESC` | Order by request count |
| `sum_bytes_ASC` / `sum_bytes_DESC` | Order by bytes |
| `count_ASC` / `count_DESC` | Order by count |

## Related

- [Analytics Overview](index.md) - Package introduction
- [Cloudflare GraphQL Schema](https://developers.cloudflare.com/analytics/graphql-api/getting-started/)
