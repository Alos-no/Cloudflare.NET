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

The SDK provides <xref:Cloudflare.NET.Roles.RoleConstants> for type-safe role name lookups. This eliminates magic strings and provides IntelliSense support:

```csharp
using Cloudflare.NET.Roles;

public class RoleHelper(ICloudflareApiClient cf)
{
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

    public async Task<AccountRole?> GetAdministratorRoleAsync(string accountId)
    {
        // Use RoleConstants for type-safe role lookup
        return await FindRoleByNameAsync(accountId, RoleConstants.Administrator);
    }

    public async Task<AccountRole?> GetDnsRoleAsync(string accountId)
    {
        return await FindRoleByNameAsync(accountId, RoleConstants.Dns);
    }

    public async Task<AccountRole?> GetBillingRoleAsync(string accountId)
    {
        return await FindRoleByNameAsync(accountId, RoleConstants.Billing);
    }
}
```

> [!TIP]
> Always use <xref:Cloudflare.NET.Roles.RoleConstants> instead of hardcoded strings to avoid typos and benefit from compile-time checking. See the [complete role reference](#annex-role-constants-reference) for all available constants.

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

The most frequently used roles include:

| Constant | Role Name | Description |
|----------|-----------|-------------|
| `Administrator` | Administrator | Full administrative access |
| `AdministratorReadOnly` | Administrator Read Only | View-only access to all resources |
| `Dns` | DNS | DNS management |
| `Firewall` | Firewall | Firewall management |
| `Billing` | Billing | Billing management |
| `AuditLogsViewer` | Audit Logs Viewer | View audit logs |

For the complete list of 60+ role constants organized by category, see the [Annex](#annex-role-constants-reference).

## Models Reference

### AccountRole

Represents a Cloudflare account role.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Role identifier |
| `Name` | `string` | Role name (matches <xref:Cloudflare.NET.Roles.RoleConstants> values) |
| `Description` | `string` | Role description |
| `Permissions` | `RolePermissions` | Detailed permission flags |

### RolePermissions

Contains nested permission groups for granular access control.

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

Represents read/write permission flags.

| Property | Type | Description |
|----------|------|-------------|
| `Read` | `bool?` | Read access granted |
| `Write` | `bool?` | Write access granted |

### ListAccountRolesFilters

Filter parameters for listing account roles.

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

// Usage with RoleConstants
var adminRole = await FindRoleByNameAsync(accountId, RoleConstants.Administrator);
var r2Role = await FindRoleByNameAsync(accountId, RoleConstants.CloudflareR2Admin);
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

### Get Roles by Category

```csharp
public async Task<IReadOnlyList<AccountRole>> GetZeroTrustRolesAsync(string accountId)
{
    var zeroTrustRoleNames = new[]
    {
        RoleConstants.CloudflareAccess,
        RoleConstants.CloudflareZeroTrust,
        RoleConstants.CloudflareZeroTrustReadOnly,
        RoleConstants.CloudflareZeroTrustReporting,
        RoleConstants.CloudflareGateway,
        RoleConstants.CloudflareDex,
        RoleConstants.CloudflareCasb
    };

    var roles = new List<AccountRole>();

    await foreach (var role in cf.Roles.ListAllAccountRolesAsync(accountId))
    {
        if (zeroTrustRoleNames.Contains(role.Name, StringComparer.OrdinalIgnoreCase))
        {
            roles.Add(role);
        }
    }

    return roles;
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

---

## Annex: Role Constants Reference

The following tables list all role constants available in <xref:Cloudflare.NET.Roles.RoleConstants>. Role availability varies by account type and subscription plan.

### Core Administrative Roles

| Constant | Role Name | Description |
|----------|-----------|-------------|
| `SuperAdministrator` | Super Administrator - All Privileges | Full administrative access to all account features and settings |
| `Administrator` | Administrator | Administrative access to the entire account |
| `AdministratorReadOnly` | Administrator Read Only | View all settings but cannot make changes |
| `MinimalAccountAccess` | Minimal Account Access | Most restricted role with minimal permissions |

### Analytics and Monitoring Roles

| Constant | Role Name | Description |
|----------|-----------|-------------|
| `Analytics` | Analytics | Access to analytics data |
| `AuditLogsViewer` | Audit Logs Viewer | View audit logs |
| `LogShare` | Log Share | Full log sharing access |
| `LogShareReader` | Log Share Reader | Read-only log sharing access |

### DNS and Domain Roles

| Constant | Role Name | Description |
|----------|-----------|-------------|
| `Dns` | DNS | DNS management |
| `ZoneVersioning` | Zone Versioning (Account-Wide) | Zone versioning management |
| `ZoneVersioningRead` | Zone Versioning Read (Account-Wide) | Read-only zone versioning access |

### Security Roles

| Constant | Role Name | Description |
|----------|-----------|-------------|
| `Firewall` | Firewall | Firewall management |
| `Waf` | WAF | Web Application Firewall management |
| `BotManagement` | Bot Management (Account-wide) | Bot management configuration |
| `PageShield` | Page Shield | Page Shield management |
| `PageShieldRead` | Page Shield Read | Read-only Page Shield access |
| `TrustAndSafety` | Trust and Safety | Trust and safety management |

### Zero Trust and Access Roles

| Constant | Role Name | Description |
|----------|-----------|-------------|
| `CloudflareAccess` | Cloudflare Access | Cloudflare Access management |
| `CloudflareZeroTrust` | Cloudflare Zero Trust | Full Zero Trust access |
| `CloudflareZeroTrustReadOnly` | Cloudflare Zero Trust Read Only | Read-only Zero Trust access |
| `CloudflareZeroTrustReporting` | Cloudflare Zero Trust Reporting | Zero Trust reporting access |
| `CloudflareZeroTrustPii` | Cloudflare Zero Trust PII | Zero Trust PII access |
| `CloudflareZeroTrustDnsLocationsWrite` | Cloudflare Zero Trust DNS Locations Write | DNS locations write access |
| `CloudflareGateway` | Cloudflare Gateway | Gateway management |
| `CloudflareDex` | Cloudflare DEX | Digital Experience management |
| `CloudflareCasb` | Cloudflare CASB | CASB management |
| `CloudflareCasbRead` | Cloudflare CASB Read | Read-only CASB access |

### API and Developer Roles

| Constant | Role Name | Description |
|----------|-----------|-------------|
| `ApiGateway` | API Gateway | API Gateway management |
| `ApiGatewayRead` | API Gateway Read | Read-only API Gateway access |
| `WorkersPlatformAdmin` | Workers Platform Admin | Workers platform administration |
| `WorkersPlatformReadOnly` | Workers Platform (Read-only) | Read-only Workers platform access |
| `HyperdriveAdmin` | Hyperdrive Admin | Hyperdrive administration |
| `HyperdriveRead` | Hyperdrive Read | Read-only Hyperdrive access |
| `VectorizeAdmin` | Vectorize Admin | Vectorize administration |
| `VectorizeReadOnly` | Vectorize Read only | Read-only Vectorize access |

### Storage Roles

| Constant | Role Name | Description |
|----------|-----------|-------------|
| `CloudflareR2Admin` | Cloudflare R2 Admin | R2 storage administration |
| `CloudflareR2Read` | Cloudflare R2 Read | Read-only R2 access |
| `CloudflareStream` | Cloudflare Stream | Stream video management |
| `CloudflareImages` | Cloudflare Images | Images management |

### Performance and Caching Roles

| Constant | Role Name | Description |
|----------|-----------|-------------|
| `CachePurge` | Cache Purge | Cache purge access |
| `LoadBalancer` | Load Balancer | Load balancer management |
| `SslTlsCachingPerformance` | SSL/TLS Caching Performance Page Rules and Customization | SSL/TLS, caching, performance, and customization |
| `WaitingRoomAdmin` | Waiting Room Admin | Waiting room administration |
| `WaitingRoomRead` | Waiting Room Read | Read-only waiting room access |

### Email Security Roles

| Constant | Role Name | Description |
|----------|-----------|-------------|
| `EmailConfigurationAdmin` | Email Configuration Admin | Email configuration administration |
| `EmailIntegrationAdmin` | Email Integration Admin | Email integration administration |
| `EmailSecurityAnalyst` | Email security Analyst | Email security analysis |
| `EmailSecurityReadOnly` | Email security Read Only | Read-only email security access |
| `EmailSecurityReporting` | Email security Reporting | Email security reporting |
| `EmailSecurityPolicyAdmin` | Email security Policy Admin | Email security policy administration |

### Network and Magic Roles

| Constant | Role Name | Description |
|----------|-----------|-------------|
| `NetworkServicesWrite` | Network Services Write (Magic) | Network services write access |
| `NetworkServicesRead` | Network Services Read (Magic) | Network services read access |
| `MagicNetworkMonitoring` | Magic Network Monitoring | Magic network monitoring |
| `MagicNetworkMonitoringAdmin` | Magic Network Monitoring Admin | Magic network monitoring administration |
| `MagicNetworkMonitoringReadOnly` | Magic Network Monitoring Read-Only | Read-only magic network monitoring |

### Security Center Roles

| Constant | Role Name | Description |
|----------|-----------|-------------|
| `SecurityCenterBrandProtection` | Security Center Brand Protection | Brand protection management |
| `SecurityCenterCloudforceOneAdmin` | Security Center Cloudforce One Admin | Cloudforce One administration |
| `SecurityCenterCloudforceOneRead` | Security Center Cloudforce One Read | Read-only Cloudforce One access |

### Secrets and Connectivity Roles

| Constant | Role Name | Description |
|----------|-----------|-------------|
| `SecretsStoreAdmin` | Secrets Store Admin | Secrets store administration |
| `SecretsStoreDeployer` | Secrets Store Deployer | Secrets store deployment |
| `SecretsStoreReporter` | Secrets Store Reporter | Secrets store reporting |
| `ConnectivityDirectoryRead` | Connectivity Directory Read | Connectivity directory read access |
| `ConnectivityDirectoryBind` | Connectivity Directory Bind | Connectivity directory bind access |
| `ConnectivityDirectoryAdmin` | Connectivity Directory Admin | Connectivity directory administration |

### Other Roles

| Constant | Role Name | Description |
|----------|-----------|-------------|
| `Billing` | Billing | Billing management |
| `Turnstile` | Turnstile | Turnstile management |
| `TurnstileRead` | Turnstile Read | Read-only Turnstile access |
| `ZarazAdmin` | Zaraz Admin | Zaraz administration |
| `ZarazEdit` | Zaraz Edit | Zaraz editing |
| `ZarazRead` | Zaraz Read | Read-only Zaraz access |
