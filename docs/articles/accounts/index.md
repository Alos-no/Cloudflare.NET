# Accounts API

The Accounts API provides access to account-level resources in Cloudflare, including account management, members, roles, tokens, and R2 storage.

## Overview

Access the Accounts API through `ICloudflareApiClient`:

```csharp
public class AccountService(ICloudflareApiClient cf)
{
    public async Task GetAccountInfoAsync(string accountId)
    {
        var account = await cf.Accounts.GetAccountDetailsAsync(accountId);
        Console.WriteLine($"Account: {account.Name}, Type: {account.Type}");
    }
}
```

## Available APIs

| API | Property | Description |
|-----|----------|-------------|
| [Account Management](account-management.md) | `cf.Accounts` | Create, list, edit, delete accounts |
| [Members](members.md) | `cf.Members` | Invite and manage account members |
| [Roles](roles.md) | `cf.Roles` | View available permission roles |
| [API Tokens](api-tokens.md) | `cf.ApiTokens` | Manage account-scoped API tokens |
| [Audit Logs](audit-logs.md) | `cf.AuditLogs` | View account activity logs |
| [Turnstile](turnstile.md) | `cf.Turnstile` | Manage Turnstile widgets |
| [R2 Buckets](r2/buckets.md) | `cf.Accounts` | Create and manage R2 buckets |
| [Access Rules](security/access-rules.md) | `cf.Accounts.AccessRules` | Account-level IP access control |
| [Rulesets](security/rulesets.md) | `cf.Accounts.Rulesets` | Account-level WAF rules |

## Account Management

### List Accounts

```csharp
await foreach (var account in cf.Accounts.ListAllAccountsAsync())
{
    Console.WriteLine($"{account.Name}: {account.Type}");
}
```

### Get Account Details

```csharp
var account = await cf.Accounts.GetAccountDetailsAsync(accountId);
Console.WriteLine($"Name: {account.Name}");
Console.WriteLine($"2FA Required: {account.Settings?.EnforceTwofactor}");
```

## Quick Links

### Account Operations
- [Account Management](account-management.md) - CRUD operations for accounts
- [Members](members.md) - Invite and manage team members
- [Roles](roles.md) - View available permission roles

### Authentication & Security
- [API Tokens](api-tokens.md) - Create and manage API tokens
- [Audit Logs](audit-logs.md) - View security and activity logs
- [Turnstile](turnstile.md) - Bot protection widgets

### R2 Storage
- [Bucket Management](r2/buckets.md) - Create, list, and delete buckets
- [Custom Domains](r2/custom-domains.md) - Attach custom hostnames to buckets
- [CORS Configuration](r2/cors.md) - Configure cross-origin access
- [Lifecycle Policies](r2/lifecycle.md) - Automatic object expiration

### Security
- [Access Rules](security/access-rules.md) - Account-level IP firewall rules
- [WAF Rulesets](security/rulesets.md) - Account-level WAF custom rules

## Required Permissions

| Feature | Permission | Level |
|---------|------------|-------|
| Account Management | Account Settings | Read/Write |
| Members | Account Members | Read/Write |
| Roles | Account Members | Read |
| API Tokens | API Tokens | Read/Write |
| Audit Logs | Audit Logs | Read |
| Turnstile | Turnstile | Read/Write |
| R2 Buckets | Workers R2 Storage | Read/Write |
| Access Rules | Account Firewall Access Rules | Read/Write |
| Rulesets | Account Rulesets | Read/Write |

## Account vs Zone Level

Many security features are available at both account and zone levels:

| Feature | Account Level | Zone Level |
|---------|---------------|------------|
| Access Rules | Applies to all zones | Applies to specific zone |
| Rulesets | Shared across zones | Zone-specific |
| API Tokens | Access multiple zones | Zone-scoped access |
| Audit Logs | All account activity | (use account logs) |

Use account-level features for policies that should apply across all zones. Use zone-level features for zone-specific policies.

## Related

- [Zone API](../zones/index.md) - Zone-level operations
- [User API](../user/index.md) - User profile and memberships
- [Subscriptions](../subscriptions.md) - Account subscription management
