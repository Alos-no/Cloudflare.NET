# Accounts API

The Accounts API provides access to account-level resources in Cloudflare, including R2 bucket management and account-wide security rules.

## Overview

Access the Accounts API through the `ICloudflareApiClient.Accounts` property:

```csharp
public class AccountService(ICloudflareApiClient cf)
{
    public async Task ListBucketsAsync()
    {
        await foreach (var bucket in cf.Accounts.ListAllR2BucketsAsync())
        {
            Console.WriteLine($"Bucket: {bucket.Name}");
        }
    }
}
```

## Available APIs

The Accounts API is organized into the following areas:

| API | Property/Method | Description |
|-----|-----------------|-------------|
| **R2 Buckets** | Direct methods | Create, list, delete buckets |
| **R2 Custom Domains** | Direct methods | Attach/detach custom domains |
| **R2 CORS** | Direct methods | Configure CORS policies |
| **R2 Lifecycle** | Direct methods | Configure object lifecycle rules |
| **Access Rules** | `cf.Accounts.AccessRules` | Account-level IP access control |
| **Rulesets** | `cf.Accounts.Rulesets` | Account-level WAF rules |

## R2 Bucket Management

### Create a Bucket

```csharp
var bucket = await cf.Accounts.CreateR2BucketAsync("my-bucket");
Console.WriteLine($"Created: {bucket.Name} in {bucket.Location}");
```

### List Buckets

```csharp
await foreach (var bucket in cf.Accounts.ListAllR2BucketsAsync())
{
    Console.WriteLine($"{bucket.Name}: {bucket.StorageClass}");
}
```

### Delete a Bucket

```csharp
// Bucket must be empty first
await cf.Accounts.DeleteR2BucketAsync("my-bucket");
```

## Quick Links

### R2 Storage
- [Bucket Management](r2/buckets.md) - Create, list, and delete buckets
- [Custom Domains](r2/custom-domains.md) - Attach custom hostnames to buckets
- [CORS Configuration](r2/cors.md) - Configure cross-origin access
- [Lifecycle Policies](r2/lifecycle.md) - Automatic object expiration and transitions

### Security
- [Access Rules](security/access-rules.md) - Account-level IP firewall rules
- [WAF Rulesets](security/rulesets.md) - Account-level WAF custom rules

## Required Permissions

| Feature | Permission | Level |
|---------|------------|-------|
| R2 Buckets | Workers R2 Storage | Account: Read/Write |
| Access Rules | Account Firewall Access Rules | Account: Read/Write |
| Rulesets | Account Rulesets | Account: Read/Write |

## Account vs Zone Level

Many security features are available at both account and zone levels:

| Feature | Account Level | Zone Level |
|---------|---------------|------------|
| Access Rules | Applies to all zones | Applies to specific zone |
| Rulesets | Shared across zones | Zone-specific |
| Scope | Broader | Targeted |

Use account-level rules for policies that should apply across all zones. Use zone-level rules for zone-specific policies.
