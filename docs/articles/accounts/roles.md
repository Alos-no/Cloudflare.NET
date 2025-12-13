# Account Roles

View predefined roles available for account members. Roles define sets of permissions that can be assigned to members.

## Overview

Access roles through `cf.Roles`:

```csharp
public class RoleService(ICloudflareApiClient cf)
{
    public async Task<IReadOnlyList<AccountRole>> GetAllRolesAsync(string accountId)
    {
        var roles = new List<AccountRole>();

        await foreach (var role in cf.Roles.ListAllAccountRolesAsync(accountId))
        {
            roles.Add(role);
        }

        return roles;
    }
}
```

> [!NOTE]
> Roles are predefined by Cloudflare and cannot be created, modified, or deleted via the API. Available roles depend on your account type and subscription plan.

## Using Role Constants

The SDK provides `RoleConstants` for type-safe role name lookups:

```csharp
using Cloudflare.NET.Roles;

// Find Administrator role by constant
var adminRole = await FindRoleByNameAsync(accountId, RoleConstants.Administrator);

// Find DNS role
var dnsRole = await FindRoleByNameAsync(accountId, RoleConstants.Dns);

// Find billing role
var billingRole = await FindRoleByNameAsync(accountId, RoleConstants.Billing);
```

## Listing Roles

### With Pagination

```csharp
var result = await cf.Roles.ListAccountRolesAsync(accountId,
    new ListAccountRolesFilters(Page: 1, PerPage: 50));

foreach (var role in result.Items)
{
    Console.WriteLine($"{role.Name}: {role.Description}");
}
```

### List All Roles

```csharp
await foreach (var role in cf.Roles.ListAllAccountRolesAsync(accountId))
{
    Console.WriteLine($"{role.Id}: {role.Name}");

    // Check specific permissions
    if (role.Permissions.Dns?.Write == true)
    {
        Console.WriteLine("  - Can modify DNS");
    }
}
```

## Getting Role Details

```csharp
var role = await cf.Roles.GetAccountRoleAsync(accountId, roleId);

Console.WriteLine($"Role: {role.Name}");
Console.WriteLine($"Description: {role.Description}");
Console.WriteLine();
Console.WriteLine("Permissions:");

// Check DNS permissions
if (role.Permissions.DnsRecords is { Read: true, Write: true })
{
    Console.WriteLine("  - Full DNS access");
}
else if (role.Permissions.DnsRecords?.Read == true)
{
    Console.WriteLine("  - DNS read-only");
}

// Check Zone permissions
if (role.Permissions.Zone?.Write == true)
{
    Console.WriteLine("  - Can modify zones");
}
```

## Common Roles

Available roles depend on account type and plan. Common roles include:

| Role | Description |
|------|-------------|
| Administrator | Full access to all account resources |
| Administrator Read Only | View-only access to all resources |
| DNS Administrator | Full access to DNS records |
| Firewall Administrator | Manage firewall rules and security |
| Audit Log Viewer | View audit logs |
| Billing | Manage billing and subscriptions |

## Models Reference

### AccountRole

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Role identifier |
| `Name` | `string` | Role name |
| `Description` | `string` | Role description |
| `Permissions` | `RolePermissions` | Detailed permission flags |

### RolePermissions

The permissions object contains nested permission groups:

| Property | Type | Description |
|----------|------|-------------|
| `Zone` | `PermissionFlags?` | Zone-level permissions |
| `ZoneSettings` | `PermissionFlags?` | Zone settings permissions |
| `Dns` | `PermissionFlags?` | DNS permissions |
| `DnsRecords` | `PermissionFlags?` | DNS record permissions |
| `Firewall` | `PermissionFlags?` | Firewall permissions |
| `Waf` | `PermissionFlags?` | WAF permissions |
| `Billing` | `PermissionFlags?` | Billing permissions |
| `AuditLogs` | `PermissionFlags?` | Audit log permissions |

### PermissionFlags

| Property | Type | Description |
|----------|------|-------------|
| `Read` | `bool?` | Read access |
| `Write` | `bool?` | Write access |

### ListAccountRolesFilters

| Property | Type | Description |
|----------|------|-------------|
| `Page` | `int?` | Page number (1-based) |
| `PerPage` | `int?` | Results per page |

## Common Patterns

### Find Role by Name

```csharp
public async Task<AccountRole?> FindRoleByNameAsync(
    string accountId,
    string roleName)
{
    await foreach (var role in cf.Roles.ListAllAccountRolesAsync(accountId))
    {
        if (role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase))
        {
            return role;
        }
    }

    return null;
}

// Usage
var adminRole = await FindRoleByNameAsync(accountId, "Administrator");
```

### Find Roles with Specific Permission

```csharp
public async Task<IReadOnlyList<AccountRole>> FindRolesWithDnsWriteAsync(
    string accountId)
{
    var matching = new List<AccountRole>();

    await foreach (var role in cf.Roles.ListAllAccountRolesAsync(accountId))
    {
        if (role.Permissions.DnsRecords?.Write == true ||
            role.Permissions.Dns?.Write == true)
        {
            matching.Add(role);
        }
    }

    return matching;
}
```

### Display Role Matrix

```csharp
public async Task DisplayRoleMatrixAsync(string accountId)
{
    Console.WriteLine("Role Permission Matrix");
    Console.WriteLine("=====================");
    Console.WriteLine();
    Console.WriteLine($"{"Role",-30} {"DNS",-8} {"Zone",-8} {"Firewall",-10} {"Billing",-8}");
    Console.WriteLine(new string('-', 70));

    await foreach (var role in cf.Roles.ListAllAccountRolesAsync(accountId))
    {
        var dns = role.Permissions.DnsRecords?.Write == true ? "Write" :
                  role.Permissions.DnsRecords?.Read == true ? "Read" : "-";

        var zone = role.Permissions.Zone?.Write == true ? "Write" :
                   role.Permissions.Zone?.Read == true ? "Read" : "-";

        var fw = role.Permissions.Firewall?.Write == true ? "Write" :
                 role.Permissions.Firewall?.Read == true ? "Read" : "-";

        var billing = role.Permissions.Billing?.Write == true ? "Write" :
                      role.Permissions.Billing?.Read == true ? "Read" : "-";

        Console.WriteLine($"{role.Name,-30} {dns,-8} {zone,-8} {fw,-10} {billing,-8}");
    }
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Account Members | Account | Read |

## Related

- [Account Members](members.md) - Assign roles to members
- [User Memberships](../user/memberships.md) - User's view of roles
- [Account Management](account-management.md) - Manage accounts
