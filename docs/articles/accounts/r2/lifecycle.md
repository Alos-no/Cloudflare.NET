# R2 Lifecycle Policies

Configure lifecycle policies to automatically manage object retention, deletion, and storage class transitions in R2 buckets.

## Overview

```csharp
public class LifecycleService(ICloudflareApiClient cf)
{
    public async Task SetRetentionPolicyAsync(string bucket, int days)
    {
        await cf.Accounts.SetBucketLifecycleAsync(bucket, new BucketLifecyclePolicy([
            new LifecycleRule(
                Id: "auto-delete",
                DeleteObjectsTransition: new DeleteObjectsTransition(
                    Condition: LifecycleCondition.AfterDays(days)
                )
            )
        ]));
    }
}
```

## Setting Lifecycle Policy

### Auto-Delete After Age

Delete objects after a specified number of days:

```csharp
await cf.Accounts.SetBucketLifecycleAsync("my-bucket", new BucketLifecyclePolicy([
    new LifecycleRule(
        Id: "delete-after-30-days",
        Enabled: true,
        DeleteObjectsTransition: new DeleteObjectsTransition(
            Condition: LifecycleCondition.AfterDays(30)
        )
    )
]));
```

### Delete on Specific Date

Delete objects on a specific date:

```csharp
await cf.Accounts.SetBucketLifecycleAsync("my-bucket", new BucketLifecyclePolicy([
    new LifecycleRule(
        Id: "delete-on-date",
        DeleteObjectsTransition: new DeleteObjectsTransition(
            Condition: LifecycleCondition.OnDate(new DateTime(2025, 12, 31))
        )
    )
]));
```

### Transition to Infrequent Access

Move objects to cheaper storage after a period:

```csharp
await cf.Accounts.SetBucketLifecycleAsync("my-bucket", new BucketLifecyclePolicy([
    new LifecycleRule(
        Id: "move-to-ia",
        StorageClassTransitions: [
            new StorageClassTransition(
                Condition: LifecycleCondition.AfterDays(30),
                StorageClass: R2StorageClass.InfrequentAccess
            )
        ]
    )
]));
```

### Abort Incomplete Multipart Uploads

Clean up failed multipart uploads:

```csharp
await cf.Accounts.SetBucketLifecycleAsync("my-bucket", new BucketLifecyclePolicy([
    new LifecycleRule(
        Id: "cleanup-multipart",
        AbortMultipartUploadsTransition: new AbortMultipartUploadsTransition(
            Condition: LifecycleCondition.AfterDays(7)
        )
    )
]));
```

### Apply to Prefix Only

Apply rules to objects with a specific prefix:

```csharp
await cf.Accounts.SetBucketLifecycleAsync("my-bucket", new BucketLifecyclePolicy([
    new LifecycleRule(
        Id: "temp-files-cleanup",
        Conditions: new LifecycleRuleConditions(Prefix: "temp/"),
        DeleteObjectsTransition: new DeleteObjectsTransition(
            Condition: LifecycleCondition.AfterDays(1)
        )
    )
]));
```

### Complete Lifecycle Configuration

```csharp
await cf.Accounts.SetBucketLifecycleAsync("my-bucket", new BucketLifecyclePolicy([
    // Move to IA after 30 days
    new LifecycleRule(
        Id: "archive-old-objects",
        StorageClassTransitions: [
            new StorageClassTransition(
                Condition: LifecycleCondition.AfterDays(30),
                StorageClass: R2StorageClass.InfrequentAccess
            )
        ]
    ),
    // Delete after 365 days
    new LifecycleRule(
        Id: "delete-old-objects",
        DeleteObjectsTransition: new DeleteObjectsTransition(
            Condition: LifecycleCondition.AfterDays(365)
        )
    ),
    // Cleanup incomplete uploads
    new LifecycleRule(
        Id: "cleanup-multipart",
        AbortMultipartUploadsTransition: new AbortMultipartUploadsTransition(
            Condition: LifecycleCondition.AfterDays(7)
        )
    ),
    // Delete temp files quickly
    new LifecycleRule(
        Id: "temp-cleanup",
        Conditions: new LifecycleRuleConditions(Prefix: "temp/"),
        DeleteObjectsTransition: new DeleteObjectsTransition(
            Condition: LifecycleCondition.AfterDays(1)
        )
    )
]));
```

## Getting Lifecycle Policy

```csharp
var policy = await cf.Accounts.GetBucketLifecycleAsync("my-bucket");

foreach (var rule in policy.Rules)
{
    Console.WriteLine($"Rule: {rule.Id}");
    Console.WriteLine($"  Enabled: {rule.Enabled}");

    if (rule.DeleteObjectsTransition is { } del)
    {
        Console.WriteLine($"  Delete after: {del.Condition.MaxAge / 86400} days");
    }
}
```

## Deleting Lifecycle Policy

```csharp
await cf.Accounts.DeleteBucketLifecycleAsync("my-bucket");
```

## Models Reference

### LifecycleRule

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string?` | Rule identifier |
| `Enabled` | `bool` | Whether rule is active (default: true) |
| `Conditions` | `LifecycleRuleConditions?` | Object filtering conditions |
| `DeleteObjectsTransition` | `DeleteObjectsTransition?` | Deletion trigger |
| `AbortMultipartUploadsTransition` | `AbortMultipartUploadsTransition?` | Multipart cleanup trigger |
| `StorageClassTransitions` | `IReadOnlyList<StorageClassTransition>?` | Storage class changes |

### LifecycleCondition

| Method | Description |
|--------|-------------|
| `AfterDays(int days)` | Trigger after N days |
| `AfterSeconds(int seconds)` | Trigger after N seconds |
| `OnDate(DateTime date)` | Trigger on specific date |

### LifecycleConditionType

| Value | Description |
|-------|-------------|
| `Age` | Based on object age |
| `Date` | Based on specific date |

### R2StorageClass

| Constant | Description |
|----------|-------------|
| `Standard` | Frequently accessed data |
| `InfrequentAccess` | Lower cost for infrequent access |

## Common Patterns

### Log Retention

```csharp
public async Task SetLogRetentionAsync(string bucket, int retentionDays)
{
    await cf.Accounts.SetBucketLifecycleAsync(bucket, new BucketLifecyclePolicy([
        new LifecycleRule(
            Id: "log-retention",
            Conditions: new LifecycleRuleConditions(Prefix: "logs/"),
            StorageClassTransitions: [
                new StorageClassTransition(
                    Condition: LifecycleCondition.AfterDays(7),
                    StorageClass: R2StorageClass.InfrequentAccess
                )
            ],
            DeleteObjectsTransition: new DeleteObjectsTransition(
                Condition: LifecycleCondition.AfterDays(retentionDays)
            )
        )
    ]));
}
```

### Backup Lifecycle

```csharp
public async Task SetBackupPolicyAsync(string bucket)
{
    await cf.Accounts.SetBucketLifecycleAsync(bucket, new BucketLifecyclePolicy([
        // Daily backups: keep 30 days
        new LifecycleRule(
            Id: "daily-backups",
            Conditions: new LifecycleRuleConditions(Prefix: "daily/"),
            DeleteObjectsTransition: new DeleteObjectsTransition(
                Condition: LifecycleCondition.AfterDays(30)
            )
        ),
        // Weekly backups: move to IA after 30 days, keep 180 days
        new LifecycleRule(
            Id: "weekly-backups-archive",
            Conditions: new LifecycleRuleConditions(Prefix: "weekly/"),
            StorageClassTransitions: [
                new StorageClassTransition(
                    Condition: LifecycleCondition.AfterDays(30),
                    StorageClass: R2StorageClass.InfrequentAccess
                )
            ]
        ),
        new LifecycleRule(
            Id: "weekly-backups-delete",
            Conditions: new LifecycleRuleConditions(Prefix: "weekly/"),
            DeleteObjectsTransition: new DeleteObjectsTransition(
                Condition: LifecycleCondition.AfterDays(180)
            )
        )
    ]));
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Workers R2 Storage | Account | Write |

## Important Notes

1. **Processing delay**: Rules are processed within 24 hours of being set
2. **Maximum rules**: Up to 1000 lifecycle rules per bucket
3. **Age calculation**: Based on object creation time
4. **Prefix matching**: Case-sensitive, matches key prefixes

## Related

- [Bucket Management](buckets.md) - Create and manage buckets
- [R2 Deleting Objects](../../r2/deletes.md) - Manual object deletion
