# R2 Bucket Locks

Configure object retention policies to prevent deletion and modification of objects in R2 buckets. Bucket locks are essential for compliance requirements like WORM (Write Once Read Many) storage.

## Overview

```csharp
public class RetentionService(ICloudflareApiClient cf)
{
    public async Task SetRetentionAsync(string bucket, int days)
    {
        var policy = new BucketLockPolicy([
            new BucketLockRule(
                Id: "retention-rule",
                Enabled: true,
                Condition: BucketLockCondition.ForDays(days)
            )
        ]);

        await cf.Accounts.Buckets.SetLockAsync(bucket, policy);
    }
}
```

> [!NOTE]
> **Jurisdictional Buckets:** If your bucket was created with a jurisdiction (e.g., `R2Jurisdiction.EuropeanUnion`), you must pass the jurisdiction parameter to all bucket lock operations. See [Working with Jurisdictional Buckets](buckets.md#working-with-jurisdictional-buckets).
>
> ```csharp
> await cf.Accounts.Buckets.SetLockAsync("my-eu-bucket", policy, R2Jurisdiction.EuropeanUnion);
> ```

## Getting Lock Policy

Retrieve the current lock rules for a bucket:

```csharp
var policy = await cf.Accounts.Buckets.GetLockAsync("my-bucket");

foreach (var rule in policy.Rules)
{
    Console.WriteLine($"Rule: {rule.Id}");
    Console.WriteLine($"  Enabled: {rule.Enabled}");
    Console.WriteLine($"  Prefix: {rule.Prefix ?? "(all objects)"}");
    Console.WriteLine($"  Type: {rule.Condition?.Type}");
}
```

## Setting Lock Policy

### Age-Based Retention (Days)

Lock objects for a specific number of days after creation:

```csharp
await cf.Accounts.Buckets.SetLockAsync("my-bucket", new BucketLockPolicy([
    new BucketLockRule(
        Id: "30-day-retention",
        Enabled: true,
        Condition: BucketLockCondition.ForDays(30)
    )
]));
```

### Age-Based Retention (Seconds)

For precise control, specify retention in seconds:

```csharp
await cf.Accounts.Buckets.SetLockAsync("my-bucket", new BucketLockPolicy([
    new BucketLockRule(
        Id: "1-hour-retention",
        Enabled: true,
        Condition: BucketLockCondition.ForSeconds(3600)
    )
]));
```

### Date-Based Retention

Lock objects until a specific date:

```csharp
await cf.Accounts.Buckets.SetLockAsync("my-bucket", new BucketLockPolicy([
    new BucketLockRule(
        Id: "retain-until-2026",
        Enabled: true,
        Condition: BucketLockCondition.UntilDate(new DateTime(2026, 1, 1))
    )
]));
```

### Indefinite Retention

Lock objects permanently (cannot be deleted):

```csharp
await cf.Accounts.Buckets.SetLockAsync("my-bucket", new BucketLockPolicy([
    new BucketLockRule(
        Id: "permanent-lock",
        Enabled: true,
        Condition: BucketLockCondition.Indefinitely()
    )
]));
```

> [!CAUTION]
> Indefinite locks cannot be removed. Objects will be retained forever. Use with extreme caution.

### Prefix-Scoped Rules

Apply retention to objects with a specific prefix:

```csharp
await cf.Accounts.Buckets.SetLockAsync("my-bucket", new BucketLockPolicy([
    new BucketLockRule(
        Id: "archive-retention",
        Enabled: true,
        Prefix: "archive/",
        Condition: BucketLockCondition.ForDays(365)
    ),
    new BucketLockRule(
        Id: "logs-retention",
        Enabled: true,
        Prefix: "logs/",
        Condition: BucketLockCondition.ForDays(90)
    )
]));
```

### Multiple Rules

Configure different retention policies for different object prefixes:

```csharp
await cf.Accounts.Buckets.SetLockAsync("compliance-bucket", new BucketLockPolicy([
    // Financial records: 7 years
    new BucketLockRule(
        Id: "financial-records",
        Enabled: true,
        Prefix: "financial/",
        Condition: BucketLockCondition.ForDays(2555)  // ~7 years
    ),
    // Legal documents: indefinite
    new BucketLockRule(
        Id: "legal-documents",
        Enabled: true,
        Prefix: "legal/",
        Condition: BucketLockCondition.Indefinitely()
    ),
    // General data: 1 year
    new BucketLockRule(
        Id: "general-retention",
        Enabled: true,
        Condition: BucketLockCondition.ForDays(365)
    )
]));
```

## Deleting Lock Policy

Remove all lock rules from a bucket:

```csharp
await cf.Accounts.Buckets.DeleteLockAsync("my-bucket");
```

> [!NOTE]
> Deleting the lock policy does not affect objects that are currently within their retention period. Those objects remain locked until their retention expires.

## Models Reference

### BucketLockPolicy

| Property | Type | Description |
|----------|------|-------------|
| `Rules` | `IReadOnlyList<BucketLockRule>` | List of lock rules (max 1000) |

### BucketLockRule

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string?` | Optional rule identifier |
| `Enabled` | `bool` | Whether the rule is active (default: true) |
| `Prefix` | `string?` | Object key prefix filter (optional) |
| `Condition` | `BucketLockCondition?` | Retention condition |

### BucketLockCondition

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `BucketLockConditionType` | Type of condition (Age, Date, Indefinite) |
| `MaxAgeSeconds` | `int?` | Retention duration in seconds (for Age type) |
| `Date` | `DateTime?` | Retention end date (for Date type) |

### BucketLockCondition Factory Methods

| Method | Description |
|--------|-------------|
| `ForDays(int days)` | Create age-based condition in days |
| `ForSeconds(int seconds)` | Create age-based condition in seconds |
| `UntilDate(DateTime date)` | Create date-based condition |
| `Indefinitely()` | Create permanent lock condition |

### BucketLockConditionType

| Value | Description |
|-------|-------------|
| `Age` | Retention based on object age |
| `Date` | Retention until specific date |
| `Indefinite` | Permanent retention |

## Common Patterns

### Compliance Configuration

```csharp
public async Task ConfigureComplianceAsync(
    string bucket,
    int retentionDays,
    string? prefix = null)
{
    var rules = new List<BucketLockRule>
    {
        new BucketLockRule(
            Id: $"compliance-{retentionDays}d",
            Enabled: true,
            Prefix: prefix,
            Condition: BucketLockCondition.ForDays(retentionDays)
        )
    };

    await cf.Accounts.Buckets.SetLockAsync(bucket, new BucketLockPolicy(rules));
}
```

### Check Lock Status

```csharp
public async Task<bool> IsLockedAsync(string bucket)
{
    var policy = await cf.Accounts.Buckets.GetLockAsync(bucket);
    return policy.Rules.Any(r => r.Enabled);
}
```

### Extend Retention

```csharp
public async Task ExtendRetentionAsync(string bucket, int additionalDays)
{
    var policy = await cf.Accounts.Buckets.GetLockAsync(bucket);

    var updatedRules = policy.Rules.Select(rule =>
    {
        if (rule.Condition?.Type == BucketLockConditionType.Age && rule.Condition.MaxAgeSeconds.HasValue)
        {
            var newSeconds = rule.Condition.MaxAgeSeconds.Value + (additionalDays * 86400);
            return rule with { Condition = BucketLockCondition.ForSeconds(newSeconds) };
        }
        return rule;
    }).ToList();

    await cf.Accounts.Buckets.SetLockAsync(bucket, new BucketLockPolicy(updatedRules));
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Workers R2 Storage | Account | Write |

## Important Notes

1. **Precedence**: If multiple rules apply to the same object, the strictest (longest) retention takes precedence
2. **Maximum rules**: Up to 1000 lock rules per bucket
3. **Immutable objects**: Locked objects cannot be deleted or overwritten until retention expires
4. **Versioning**: Lock rules apply to individual object versions
5. **Billing**: Locked objects continue to incur storage costs during retention

## Compliance Use Cases

| Requirement | Recommended Configuration |
|-------------|---------------------------|
| HIPAA | 6-year retention for medical records |
| SOX | 7-year retention for financial data |
| GDPR | Date-based retention with deletion |
| Legal Hold | Indefinite retention |

## Related

- [Bucket Management](buckets.md) - Create and manage buckets
- [Lifecycle Policies](lifecycle.md) - Automatic object management
