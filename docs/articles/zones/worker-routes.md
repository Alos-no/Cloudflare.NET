# Worker Routes

Manage Worker routes that map URL patterns to Worker scripts within a zone.

## Overview

Access Worker routes through `cf.Workers`:

```csharp
public class WorkerRouteService(ICloudflareApiClient cf)
{
    public async Task<WorkerRoute> CreateRouteAsync(
        string zoneId,
        string pattern,
        string scriptName)
    {
        return await cf.Workers.CreateRouteAsync(zoneId,
            new CreateWorkerRouteRequest(
                Pattern: pattern,
                Script: scriptName
            ));
    }
}
```

## Listing Routes

```csharp
var routes = await cf.Workers.ListRoutesAsync(zoneId);

foreach (var route in routes)
{
    Console.WriteLine($"{route.Pattern} -> {route.Script ?? "(disabled)"}");
}
```

## Getting Route Details

```csharp
var route = await cf.Workers.GetRouteAsync(zoneId, routeId);

Console.WriteLine($"ID: {route.Id}");
Console.WriteLine($"Pattern: {route.Pattern}");
Console.WriteLine($"Script: {route.Script ?? "None (route disabled)"}");
```

## Creating Routes

### Route with Worker Script

```csharp
var route = await cf.Workers.CreateRouteAsync(zoneId,
    new CreateWorkerRouteRequest(
        Pattern: "api.example.com/*",
        Script: "api-handler"
    ));

Console.WriteLine($"Created route: {route.Id}");
```

### Disabled Route (No Script)

Create a route that doesn't invoke any Worker (useful for excluding paths):

```csharp
var route = await cf.Workers.CreateRouteAsync(zoneId,
    new CreateWorkerRouteRequest(
        Pattern: "api.example.com/health",
        Script: null  // No Worker invoked
    ));
```

## Route Patterns

Worker route patterns support wildcards:

| Pattern | Matches |
|---------|---------|
| `example.com/*` | All paths on example.com |
| `*.example.com/*` | All subdomains and paths |
| `example.com/api/*` | Only /api/ paths |
| `example.com/v1/users/*` | Specific API paths |

> [!NOTE]
> More specific routes take precedence over less specific ones.

## Updating Routes

```csharp
var updated = await cf.Workers.UpdateRouteAsync(zoneId, routeId,
    new UpdateWorkerRouteRequest(
        Pattern: "api.example.com/v2/*",
        Script: "api-handler-v2"
    ));
```

### Change Script

```csharp
var updated = await cf.Workers.UpdateRouteAsync(zoneId, routeId,
    new UpdateWorkerRouteRequest(
        Pattern: existingRoute.Pattern,
        Script: "new-worker-script"
    ));
```

### Disable Route

Remove the script to disable the route:

```csharp
var updated = await cf.Workers.UpdateRouteAsync(zoneId, routeId,
    new UpdateWorkerRouteRequest(
        Pattern: existingRoute.Pattern,
        Script: null
    ));
```

## Deleting Routes

```csharp
await cf.Workers.DeleteRouteAsync(zoneId, routeId);
Console.WriteLine($"Deleted route: {routeId}");
```

## Models Reference

### WorkerRoute

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Route identifier |
| `Pattern` | `string` | URL pattern to match |
| `Script` | `string?` | Worker script name (null = disabled) |

### CreateWorkerRouteRequest

| Property | Type | Description |
|----------|------|-------------|
| `Pattern` | `string` | URL pattern to match |
| `Script` | `string?` | Worker script name |

### UpdateWorkerRouteRequest

| Property | Type | Description |
|----------|------|-------------|
| `Pattern` | `string` | New URL pattern |
| `Script` | `string?` | New Worker script name |

## Common Patterns

### Find Route by Pattern

```csharp
public async Task<WorkerRoute?> FindRouteByPatternAsync(
    string zoneId,
    string pattern)
{
    var routes = await cf.Workers.ListRoutesAsync(zoneId);

    return routes.FirstOrDefault(r =>
        r.Pattern.Equals(pattern, StringComparison.OrdinalIgnoreCase));
}
```

### Update or Create Route

```csharp
public async Task<WorkerRoute> UpsertRouteAsync(
    string zoneId,
    string pattern,
    string script)
{
    var existing = await FindRouteByPatternAsync(zoneId, pattern);

    if (existing is not null)
    {
        return await cf.Workers.UpdateRouteAsync(zoneId, existing.Id,
            new UpdateWorkerRouteRequest(Pattern: pattern, Script: script));
    }

    return await cf.Workers.CreateRouteAsync(zoneId,
        new CreateWorkerRouteRequest(Pattern: pattern, Script: script));
}
```

### Deploy Routes for API Versions

```csharp
public async Task DeployApiVersionAsync(
    string zoneId,
    string version,
    string scriptName)
{
    var patterns = new[]
    {
        $"api.example.com/{version}/*",
        $"*.api.example.com/{version}/*"
    };

    foreach (var pattern in patterns)
    {
        await cf.Workers.CreateRouteAsync(zoneId,
            new CreateWorkerRouteRequest(Pattern: pattern, Script: scriptName));

        Console.WriteLine($"Created route: {pattern} -> {scriptName}");
    }
}
```

### List Routes by Script

```csharp
public async Task<IReadOnlyList<WorkerRoute>> GetRoutesByScriptAsync(
    string zoneId,
    string scriptName)
{
    var routes = await cf.Workers.ListRoutesAsync(zoneId);

    return routes
        .Where(r => r.Script?.Equals(scriptName, StringComparison.OrdinalIgnoreCase) == true)
        .ToList();
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Workers Routes | Zone | Read (for listing/get) |
| Workers Routes | Zone | Write (for create/update/delete) |

## Related

- [Zone Management](zone-management.md) - Manage zones
- [DNS Records](dns-records.md) - Manage DNS
