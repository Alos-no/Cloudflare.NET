# Account Management

Manage Cloudflare accounts through the SDK. This includes listing, creating, updating, and deleting accounts.

## Overview

Access account management through `cf.Accounts`:

```csharp
public class AccountService(ICloudflareApiClient cf)
{
    public async Task<Account> GetAccountAsync(string accountId)
    {
        return await cf.Accounts.GetAccountDetailsAsync(accountId);
    }
}
```

## Listing Accounts

### List with Pagination

```csharp
var page = await cf.Accounts.ListAccountsAsync(new ListAccountsFilters
{
    Page = 1,
    PerPage = 20
});

foreach (var account in page.Items)
{
    Console.WriteLine($"{account.Name} ({account.Id})");
    Console.WriteLine($"  Type: {account.Type}");
    Console.WriteLine($"  Created: {account.CreatedOn}");
}
```

### List All Accounts

```csharp
await foreach (var account in cf.Accounts.ListAllAccountsAsync())
{
    Console.WriteLine($"{account.Name}: {account.Type}");
}
```

### Filtering by Name

```csharp
var filters = new ListAccountsFilters(Name: "production");

await foreach (var account in cf.Accounts.ListAllAccountsAsync(filters))
{
    Console.WriteLine($"Found: {account.Name}");
}
```

## Getting Account Details

```csharp
var account = await cf.Accounts.GetAccountDetailsAsync(accountId);

Console.WriteLine($"Account: {account.Name}");
Console.WriteLine($"Type: {account.Type}");
Console.WriteLine($"Created: {account.CreatedOn}");

if (account.Settings is not null)
{
    Console.WriteLine($"2FA Enforced: {account.Settings.EnforceTwofactor}");
    Console.WriteLine($"Abuse Email: {account.Settings.AbuseContactEmail}");
}

if (account.ManagedBy is not null)
{
    Console.WriteLine($"Parent Org: {account.ManagedBy.ParentOrgName}");
}
```

## Creating Accounts

> [!NOTE]
> **Preview:** This operation has limited test coverage.

> [!NOTE]
> Account creation is only available to tenant administrators. Standard users should create accounts through the Cloudflare dashboard.

```csharp
var account = await cf.Accounts.CreateAccountAsync(
    new CreateAccountRequest(Name: "My New Account"));

Console.WriteLine($"Created account: {account.Id}");
```

## Updating Accounts

### Update Name

```csharp
var updated = await cf.Accounts.UpdateAccountAsync(accountId,
    new UpdateAccountRequest(Name: "New Account Name"));
```

### Update Name and Settings

```csharp
var updated = await cf.Accounts.UpdateAccountAsync(accountId,
    new UpdateAccountRequest(
        Name: "Production Account",
        Settings: new AccountSettings(
            AbuseContactEmail: "abuse@example.com",
            EnforceTwofactor: true
        )
    ));
```

## Deleting Accounts

> [!NOTE]
> **Preview:** This operation has limited test coverage.

> [!WARNING]
> Deleting an account is permanent and removes all zones, Workers, R2 buckets, and other resources.

```csharp
var result = await cf.Accounts.DeleteAccountAsync(accountId);
Console.WriteLine($"Deleted account: {result.Id}");
```

## Models Reference

### Account

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Account identifier (32 hex characters) |
| `Name` | `string` | Account name |
| `Type` | `AccountType` | Account type (standard, enterprise, etc.) |
| `CreatedOn` | `DateTime` | Creation timestamp |
| `ManagedBy` | `AccountManagedBy?` | Parent organization info (if managed) |
| `Settings` | `AccountSettings?` | Account settings |

### AccountType (Extensible Enum)

| Known Value | Description |
|-------------|-------------|
| `Standard` | Standard Cloudflare account |
| `Enterprise` | Enterprise account with advanced features |

### AccountSettings

| Property | Type | Description |
|----------|------|-------------|
| `AbuseContactEmail` | `string?` | Email for abuse reports |
| `EnforceTwofactor` | `bool` | Whether 2FA is required for all members |

### ListAccountsFilters

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string?` | Filter by name (partial match) |
| `Page` | `int?` | Page number (1-based) |
| `PerPage` | `int?` | Results per page (5-50) |
| `Direction` | `ListOrderDirection?` | Sort direction |

## Common Patterns

### Find Account by Name

```csharp
public async Task<Account?> FindAccountByNameAsync(string name)
{
    await foreach (var account in cf.Accounts.ListAllAccountsAsync(
        new ListAccountsFilters(Name: name)))
    {
        if (account.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            return account;
        }
    }

    return null;
}
```

### Enforce 2FA on All Accounts

```csharp
public async Task EnforceTwoFactorAsync()
{
    await foreach (var account in cf.Accounts.ListAllAccountsAsync())
    {
        if (account.Settings?.EnforceTwofactor != true)
        {
            await cf.Accounts.UpdateAccountAsync(account.Id,
                new UpdateAccountRequest(
                    Name: account.Name,
                    Settings: new AccountSettings(EnforceTwofactor: true)
                ));

            Console.WriteLine($"Enabled 2FA for: {account.Name}");
        }
    }
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Account Settings | Account | Read (for listing/get) |
| Account Settings | Account | Write (for create/update/delete) |

## Related

- [Account Members](members.md) - Manage account members
- [Account Roles](roles.md) - View available roles
- [Account Audit Logs](audit-logs.md) - View account activity
- [Account API Tokens](api-tokens.md) - Manage API tokens
