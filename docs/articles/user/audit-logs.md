# User Audit Logs

View audit logs for actions taken by you across all your accounts. Audit logs are retained for 30 days.

## Overview

Access user audit logs through `cf.AuditLogs`:

```csharp
public class UserAuditService(ICloudflareApiClient cf)
{
    public async Task<IReadOnlyList<AuditLog>> GetMyRecentActivityAsync()
    {
        var result = await cf.AuditLogs.ListUserAuditLogsAsync(
            new ListAuditLogsFilters(Limit: 100));

        return result.Items;
    }
}
```

> [!NOTE]
> User audit logs show actions **you** performed, not actions performed on your resources.

## Listing Audit Logs

### With Pagination

```csharp
var result = await cf.AuditLogs.ListUserAuditLogsAsync(
    new ListAuditLogsFilters(
        Since: DateTime.UtcNow.AddDays(-7),
        Limit: 100
    ));

foreach (var log in result.Items)
{
    Console.WriteLine($"{log.Action.Time}: {log.Action.Type}");
    Console.WriteLine($"  Result: {log.Action.Result}");
    Console.WriteLine($"  Resource: {log.Resource.Type}");
}

// Check for more pages
if (result.CursorInfo?.Cursor is not null)
{
    var nextPage = await cf.AuditLogs.ListUserAuditLogsAsync(
        new ListAuditLogsFilters(Cursor: result.CursorInfo.Cursor));
}
```

### List All Logs

```csharp
var filters = new ListAuditLogsFilters(
    Since: DateTime.UtcNow.AddDays(-7)
);

await foreach (var log in cf.AuditLogs.ListAllUserAuditLogsAsync(filters))
{
    Console.WriteLine($"{log.Action.Time}: {log.Action.Type}");
}
```

## Filtering Logs

### By Time Range

```csharp
var filters = new ListAuditLogsFilters(
    Since: DateTime.UtcNow.AddDays(-7),
    Before: DateTime.UtcNow.AddDays(-1)
);

var logs = await cf.AuditLogs.ListUserAuditLogsAsync(filters);
```

### By Action Type

```csharp
var filters = new ListAuditLogsFilters(
    ActionType: "dns_record_create"
);

await foreach (var log in cf.AuditLogs.ListAllUserAuditLogsAsync(filters))
{
    Console.WriteLine($"Created DNS record: {log.Resource.Id}");
}
```

### By Result (Success/Failure)

```csharp
// Find your failed actions
var filters = new ListAuditLogsFilters(
    ActionResults: new[] { "failure" }
);

await foreach (var log in cf.AuditLogs.ListAllUserAuditLogsAsync(filters))
{
    Console.WriteLine($"Failed: {log.Action.Type}");
    Console.WriteLine($"  Time: {log.Action.Time}");
    Console.WriteLine($"  Description: {log.Action.Description}");
}
```

## User vs Account Audit Logs

| Feature | User Audit Logs | Account Audit Logs |
|---------|-----------------|-------------------|
| Scope | Your actions only | All account activity |
| Cross-account | Yes, all your accounts | Single account |
| Access | `cf.AuditLogs.ListUserAuditLogsAsync` | `cf.AuditLogs.GetAccountAuditLogsAsync` |
| Best for | Personal activity review | Security monitoring |

## Models Reference

### AuditLog

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique log identifier |
| `Action` | `AuditAction` | Action details |
| `Actor` | `AuditActor` | Who performed (you) |
| `Owner` | `AuditOwner` | Account owner |
| `Resource` | `AuditResource` | Affected resource |
| `When` | `DateTime` | When it occurred |

### AuditAction

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `string` | Action type |
| `Result` | `string` | "success" or "failure" |
| `Time` | `DateTime` | Timestamp |
| `Description` | `string?` | Human-readable description |

### ListAuditLogsFilters

| Property | Type | Description |
|----------|------|-------------|
| `Since` | `DateTime?` | Start time |
| `Before` | `DateTime?` | End time |
| `ActionType` | `string?` | Filter by action type |
| `ActionResults` | `string[]?` | Filter by result |
| `ResourceId` | `string?` | Filter by resource |
| `ResourceType` | `string?` | Filter by resource type |
| `Limit` | `int?` | Max results per page |
| `Cursor` | `string?` | Pagination cursor |

## Common Patterns

### Daily Activity Summary

```csharp
public async Task DailyActivitySummaryAsync()
{
    var filters = new ListAuditLogsFilters(
        Since: DateTime.UtcNow.Date  // Start of today
    );

    var actionCounts = new Dictionary<string, int>();
    var failureCount = 0;

    await foreach (var log in cf.AuditLogs.ListAllUserAuditLogsAsync(filters))
    {
        actionCounts.TryGetValue(log.Action.Type, out var count);
        actionCounts[log.Action.Type] = count + 1;

        if (log.Action.Result == "failure")
            failureCount++;
    }

    Console.WriteLine("=== Today's Activity ===");
    Console.WriteLine($"Total failures: {failureCount}");
    Console.WriteLine();
    Console.WriteLine("Actions by type:");

    foreach (var (action, count) in actionCounts.OrderByDescending(x => x.Value))
    {
        Console.WriteLine($"  {action}: {count}");
    }
}
```

### Find Actions on Specific Resource

```csharp
public async Task<IReadOnlyList<AuditLog>> GetResourceHistoryAsync(string resourceId)
{
    var filters = new ListAuditLogsFilters(
        ResourceId: resourceId,
        Since: DateTime.UtcNow.AddDays(-30)
    );

    var logs = new List<AuditLog>();

    await foreach (var log in cf.AuditLogs.ListAllUserAuditLogsAsync(filters))
    {
        logs.Add(log);
    }

    return logs;
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Audit Logs | User | Read |

## Related

- [Account Audit Logs](../accounts/audit-logs.md) - Full account activity
- [User Profile](profile.md) - Your profile settings
