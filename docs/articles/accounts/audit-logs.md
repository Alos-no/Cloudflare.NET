# Account Audit Logs

View audit logs for account activity. Audit logs provide a record of all actions taken on account resources over the past 30 days.

## Overview

Access audit logs through `cf.AuditLogs`:

```csharp
public class AuditService(ICloudflareApiClient cf)
{
    public async Task<IReadOnlyList<AuditLog>> GetRecentLogsAsync(string accountId)
    {
        var result = await cf.AuditLogs.GetAccountAuditLogsAsync(accountId,
            new ListAuditLogsFilters(Limit: 100));

        return result.Items;
    }
}
```

> [!NOTE]
> Audit logs are only available for the past **30 days**. This is a beta API.

## Listing Audit Logs

### With Pagination

```csharp
var result = await cf.AuditLogs.GetAccountAuditLogsAsync(accountId,
    new ListAuditLogsFilters(
        Since: DateTime.UtcNow.AddDays(-7),
        Limit: 100
    ));

foreach (var log in result.Items)
{
    Console.WriteLine($"{log.Action.Time}: {log.Action.Type}");
    Console.WriteLine($"  Actor: {log.Actor.Email}");
    Console.WriteLine($"  Result: {log.Action.Result}");
}

// Check for more pages
if (result.CursorInfo?.Cursor is not null)
{
    var nextPage = await cf.AuditLogs.GetAccountAuditLogsAsync(accountId,
        new ListAuditLogsFilters(Cursor: result.CursorInfo.Cursor));
}
```

### List All Logs

```csharp
var filters = new ListAuditLogsFilters(
    Since: DateTime.UtcNow.AddDays(-7)
);

await foreach (var log in cf.AuditLogs.GetAllAccountAuditLogsAsync(accountId, filters))
{
    Console.WriteLine($"{log.Action.Time}: {log.Action.Type} by {log.Actor.Email}");
}
```

## Filtering Logs

### By Time Range

```csharp
var filters = new ListAuditLogsFilters(
    Since: DateTime.UtcNow.AddDays(-7),   // Start time
    Before: DateTime.UtcNow.AddDays(-1)   // End time
);

var logs = await cf.AuditLogs.GetAccountAuditLogsAsync(accountId, filters);
```

### By Actor

```csharp
var filters = new ListAuditLogsFilters(
    ActorEmail: "admin@example.com"
);

await foreach (var log in cf.AuditLogs.GetAllAccountAuditLogsAsync(accountId, filters))
{
    Console.WriteLine($"{log.Action.Type}: {log.Action.Description}");
}
```

### By Action Type

```csharp
var filters = new ListAuditLogsFilters(
    ActionType: "zone_create"
);

var logs = await cf.AuditLogs.GetAccountAuditLogsAsync(accountId, filters);
```

### By Result (Success/Failure)

```csharp
// Get failed actions only
var filters = new ListAuditLogsFilters(
    ActionResults: new[] { "failure" }
);

await foreach (var log in cf.AuditLogs.GetAllAccountAuditLogsAsync(accountId, filters))
{
    Console.WriteLine($"Failed: {log.Action.Type} - {log.Action.Description}");
}
```

### By Resource

```csharp
var filters = new ListAuditLogsFilters(
    ResourceId: zoneId  // Filter by specific resource
);

var logs = await cf.AuditLogs.GetAccountAuditLogsAsync(accountId, filters);
```

## Models Reference

### AuditLog

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique log identifier |
| `Action` | `AuditAction` | Details about the action taken |
| `Actor` | `AuditActor` | Who performed the action |
| `Owner` | `AuditOwner` | Account that owns the resource |
| `Resource` | `AuditResource` | The resource affected |
| `When` | `DateTime` | When the action occurred |

### AuditAction

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `string` | Action type (e.g., "zone_create", "dns_record_delete") |
| `Result` | `string` | Result ("success" or "failure") |
| `Time` | `DateTime` | When the action was performed |
| `Description` | `string?` | Human-readable description |

### AuditActor

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Actor identifier |
| `Email` | `string?` | Actor's email address |
| `Type` | `string` | Actor type (e.g., "user", "token") |
| `Ip` | `string?` | IP address of the actor |

### ListAuditLogsFilters

| Property | Type | Description |
|----------|------|-------------|
| `Since` | `DateTime?` | Start of time range |
| `Before` | `DateTime?` | End of time range |
| `ActorEmail` | `string?` | Filter by actor email |
| `ActorIp` | `string?` | Filter by actor IP |
| `ActionType` | `string?` | Filter by action type |
| `ActionResults` | `string[]?` | Filter by result ("success", "failure") |
| `ResourceId` | `string?` | Filter by resource ID |
| `ResourceType` | `string?` | Filter by resource type |
| `Limit` | `int?` | Maximum results per page |
| `Cursor` | `string?` | Cursor for pagination |
| `Direction` | `string?` | Sort direction ("asc" or "desc") |

## Common Patterns

### Security Audit Report

```csharp
public async Task GenerateSecurityReportAsync(string accountId)
{
    var filters = new ListAuditLogsFilters(
        Since: DateTime.UtcNow.AddDays(-30),
        ActionResults: new[] { "failure" }
    );

    Console.WriteLine("=== Security Audit Report ===");
    Console.WriteLine($"Period: Last 30 days");
    Console.WriteLine();

    var failedByActor = new Dictionary<string, int>();

    await foreach (var log in cf.AuditLogs.GetAllAccountAuditLogsAsync(accountId, filters))
    {
        var email = log.Actor.Email ?? "unknown";
        failedByActor.TryGetValue(email, out var count);
        failedByActor[email] = count + 1;
    }

    Console.WriteLine("Failed Actions by Actor:");
    foreach (var (email, count) in failedByActor.OrderByDescending(x => x.Value))
    {
        Console.WriteLine($"  {email}: {count} failures");
    }
}
```

### Monitor Specific Action Types

```csharp
public async Task MonitorZoneChangesAsync(string accountId, TimeSpan lookback)
{
    var filters = new ListAuditLogsFilters(
        Since: DateTime.UtcNow - lookback
    );

    var zoneActions = new[] { "zone_create", "zone_delete", "zone_update" };

    await foreach (var log in cf.AuditLogs.GetAllAccountAuditLogsAsync(accountId, filters))
    {
        if (zoneActions.Contains(log.Action.Type))
        {
            Console.WriteLine($"[{log.Action.Time}] {log.Action.Type}");
            Console.WriteLine($"  By: {log.Actor.Email} from {log.Actor.Ip}");
            Console.WriteLine($"  Resource: {log.Resource.Id}");
        }
    }
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Audit Logs | Account | Read |

## Related

- [User Audit Logs](../user/audit-logs.md) - View user-specific activity
- [Account Management](account-management.md) - Manage accounts
- [Account Members](members.md) - Manage account members
