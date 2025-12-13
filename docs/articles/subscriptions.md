# Subscriptions

Manage Cloudflare subscriptions for accounts, users, and zones. Subscriptions control billing plans and add-ons for Cloudflare services.

## Overview

Access subscriptions through `cf.Subscriptions`:

```csharp
public class SubscriptionService(ICloudflareApiClient cf)
{
    public async Task<IReadOnlyList<Subscription>> GetAccountSubscriptionsAsync(
        string accountId)
    {
        return await cf.Subscriptions.ListAccountSubscriptionsAsync(accountId);
    }
}
```

> [!WARNING]
> Creating or updating subscriptions may incur billing charges. Use test/sandbox accounts for development.

## Account Subscriptions

### Listing Subscriptions

```csharp
var subscriptions = await cf.Subscriptions.ListAccountSubscriptionsAsync(accountId);

foreach (var sub in subscriptions)
{
    Console.WriteLine($"{sub.RatePlan?.PublicName}: {sub.State}");
    Console.WriteLine($"  Price: {sub.Price} {sub.Currency}");
    Console.WriteLine($"  Frequency: {sub.Frequency}");
}
```

### Creating Subscriptions

> [!NOTE]
> **Preview:** This operation has limited test coverage.

```csharp
var subscription = await cf.Subscriptions.CreateAccountSubscriptionAsync(accountId,
    new CreateAccountSubscriptionRequest(
        RatePlan: new RatePlanReference("enterprise_plan_id"),
        Frequency: SubscriptionFrequency.Monthly
    ));

Console.WriteLine($"Created subscription: {subscription.Id}");
```

### Updating Subscriptions

> [!NOTE]
> **Preview:** This operation has limited test coverage.

```csharp
var updated = await cf.Subscriptions.UpdateAccountSubscriptionAsync(
    accountId,
    subscriptionId,
    new UpdateAccountSubscriptionRequest(
        Frequency: SubscriptionFrequency.Yearly
    ));

Console.WriteLine($"Updated to yearly billing");
```

### Deleting Subscriptions

> [!NOTE]
> **Preview:** This operation has limited test coverage.

> [!WARNING]
> Deleting a subscription cancels the associated plan. This may be irreversible.

```csharp
await cf.Subscriptions.DeleteAccountSubscriptionAsync(accountId, subscriptionId);
Console.WriteLine("Subscription cancelled");
```

## User Subscriptions

### Listing Subscriptions

```csharp
var subscriptions = await cf.Subscriptions.ListUserSubscriptionsAsync();

foreach (var sub in subscriptions)
{
    Console.WriteLine($"{sub.RatePlan?.PublicName}: {sub.State}");
}
```

### Updating Subscriptions

> [!NOTE]
> **Preview:** This operation has limited test coverage.

```csharp
var updated = await cf.Subscriptions.UpdateUserSubscriptionAsync(subscriptionId,
    new UpdateUserSubscriptionRequest(
        Frequency: SubscriptionFrequency.Yearly
    ));
```

### Deleting Subscriptions

> [!NOTE]
> **Preview:** This operation has limited test coverage.

```csharp
var result = await cf.Subscriptions.DeleteUserSubscriptionAsync(subscriptionId);
Console.WriteLine($"Deleted subscription: {result.SubscriptionId}");
```

## Zone Subscriptions

### Getting Zone Subscription

```csharp
var subscription = await cf.Subscriptions.GetZoneSubscriptionAsync(zoneId);

Console.WriteLine($"Current plan: {subscription.RatePlan?.PublicName}");
Console.WriteLine($"State: {subscription.State}");
Console.WriteLine($"Price: {subscription.Price} {subscription.Currency}");
```

### Listing Available Plans

```csharp
var plans = await cf.Subscriptions.ListAvailableRatePlansAsync(zoneId);

foreach (var plan in plans)
{
    Console.WriteLine($"{plan.Name}:");
    Console.WriteLine($"  ID: {plan.Id}");
    Console.WriteLine($"  {plan.Currency} {plan.Frequency}");
}
```

### Upgrading Zone Plan

> [!NOTE]
> **Preview:** This operation has limited test coverage.

```csharp
var subscription = await cf.Subscriptions.CreateZoneSubscriptionAsync(zoneId,
    new CreateZoneSubscriptionRequest(
        RatePlan: new RatePlanReference("pro"),
        Frequency: SubscriptionFrequency.Monthly
    ));

Console.WriteLine($"Upgraded to: {subscription.RatePlan?.PublicName}");
```

### Changing Zone Plan

> [!NOTE]
> **Preview:** This operation has limited test coverage.

```csharp
var updated = await cf.Subscriptions.UpdateZoneSubscriptionAsync(zoneId,
    new UpdateZoneSubscriptionRequest(
        RatePlan: new RatePlanReference("business")
    ));

Console.WriteLine($"Changed to: {updated.RatePlan?.PublicName}");
```

## Externally Managed Subscriptions

Some subscriptions are managed outside of Cloudflare (e.g., through partners) and cannot be modified via API.

```csharp
var subscription = await cf.Subscriptions.GetZoneSubscriptionAsync(zoneId);

if (subscription.RatePlan?.ExternallyManaged == true)
{
    Console.WriteLine("This subscription is externally managed");
    Console.WriteLine("Contact your partner to make changes");
}
```

## Models Reference

### Subscription

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Subscription identifier |
| `RatePlan` | `RatePlan?` | Associated rate plan |
| `State` | `SubscriptionState` | Current state |
| `Price` | `decimal?` | Current price |
| `Currency` | `string?` | Currency code |
| `Frequency` | `SubscriptionFrequency?` | Billing frequency |
| `ComponentValues` | `IReadOnlyList<ComponentValue>?` | Add-on values |
| `CurrentPeriodStart` | `DateTime?` | Billing period start |
| `CurrentPeriodEnd` | `DateTime?` | Billing period end |

### SubscriptionState (Extensible Enum)

| Known Value | Description |
|-------------|-------------|
| `Trial` | Trial period |
| `Provisioned` | Active subscription |
| `Paid` | Paid and active |
| `AwaitingPayment` | Payment pending |
| `Cancelled` | Subscription cancelled |
| `Failed` | Payment failed |
| `Expired` | Subscription expired |

### SubscriptionFrequency (Extensible Enum)

| Known Value | Description |
|-------------|-------------|
| `Weekly` | Weekly billing |
| `Monthly` | Monthly billing |
| `Quarterly` | Quarterly billing |
| `Yearly` | Annual billing |

### RatePlan

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Plan identifier |
| `Name` | `string?` | Internal name |
| `PublicName` | `string?` | Display name |
| `Currency` | `string?` | Currency code |
| `Frequency` | `string?` | Billing frequency |
| `ExternallyManaged` | `bool` | If managed by partner |

### ZoneRatePlan

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Plan identifier |
| `Name` | `string` | Plan name |
| `Currency` | `string?` | Currency code |
| `Frequency` | `string?` | Billing frequency |

## Common Patterns

### Upgrade Zone to Pro

```csharp
public async Task UpgradeToProAsync(string zoneId)
{
    var plans = await cf.Subscriptions.ListAvailableRatePlansAsync(zoneId);
    var proPlan = plans.FirstOrDefault(p =>
        p.Name.Contains("pro", StringComparison.OrdinalIgnoreCase));

    if (proPlan is null)
    {
        Console.WriteLine("Pro plan not available");
        return;
    }

    await cf.Subscriptions.CreateZoneSubscriptionAsync(zoneId,
        new CreateZoneSubscriptionRequest(
            RatePlan: new RatePlanReference(proPlan.Id),
            Frequency: SubscriptionFrequency.Monthly
        ));

    Console.WriteLine($"Upgraded to Pro plan");
}
```

### List All Active Subscriptions

```csharp
public async Task ListAllActiveAsync(string accountId)
{
    Console.WriteLine("=== Account Subscriptions ===");

    var accountSubs = await cf.Subscriptions.ListAccountSubscriptionsAsync(accountId);

    foreach (var sub in accountSubs.Where(s => s.State == SubscriptionState.Provisioned))
    {
        Console.WriteLine($"  {sub.RatePlan?.PublicName}: {sub.Price} {sub.Currency}");
    }

    Console.WriteLine();
    Console.WriteLine("=== User Subscriptions ===");

    var userSubs = await cf.Subscriptions.ListUserSubscriptionsAsync();

    foreach (var sub in userSubs.Where(s => s.State == SubscriptionState.Provisioned))
    {
        Console.WriteLine($"  {sub.RatePlan?.PublicName}: {sub.Price} {sub.Currency}");
    }
}
```

### Check Zone Plan Level

```csharp
public async Task<string> GetZonePlanLevelAsync(string zoneId)
{
    var subscription = await cf.Subscriptions.GetZoneSubscriptionAsync(zoneId);

    return subscription.RatePlan?.PublicName ?? "Unknown";
}
```

### Switch to Annual Billing

```csharp
public async Task SwitchToAnnualAsync(string accountId)
{
    var subscriptions = await cf.Subscriptions.ListAccountSubscriptionsAsync(accountId);

    foreach (var sub in subscriptions)
    {
        if (sub.Frequency == SubscriptionFrequency.Monthly &&
            sub.State == SubscriptionState.Provisioned)
        {
            try
            {
                await cf.Subscriptions.UpdateAccountSubscriptionAsync(
                    accountId,
                    sub.Id,
                    new UpdateAccountSubscriptionRequest(
                        Frequency: SubscriptionFrequency.Yearly
                    ));

                Console.WriteLine($"Switched to annual: {sub.RatePlan?.PublicName}");
            }
            catch (CloudflareApiException ex)
            {
                Console.WriteLine($"Cannot update {sub.RatePlan?.PublicName}: {ex.Message}");
            }
        }
    }
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Billing | Account/Zone | Read (for listing/get) |
| Billing | Account/Zone | Write (for create/update/delete) |

## Related

- [Account Management](accounts/account-management.md) - Manage accounts
- [Zone Management](zones/zone-management.md) - Manage zones
- [User Profile](user/profile.md) - User settings
