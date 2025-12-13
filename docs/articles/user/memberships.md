# User Memberships

View and manage your account memberships. Memberships represent the accounts you have access to.

## Overview

Access memberships through `cf.User`:

```csharp
public class MembershipService(ICloudflareApiClient cf)
{
    public async Task<IReadOnlyList<Membership>> GetMyAccountsAsync()
    {
        var memberships = new List<Membership>();

        await foreach (var membership in cf.User.ListAllMembershipsAsync())
        {
            memberships.Add(membership);
        }

        return memberships;
    }
}
```

## Listing Memberships

### With Pagination

```csharp
var result = await cf.User.ListMembershipsAsync(
    new ListMembershipsFilters(
        Status: MemberStatus.Accepted,
        Page: 1,
        PerPage: 50
    ));

foreach (var membership in result.Items)
{
    Console.WriteLine($"Account: {membership.Account.Name}");
    Console.WriteLine($"  Status: {membership.Status}");
    Console.WriteLine($"  Roles: {string.Join(", ", membership.Roles.Select(r => r.Name))}");
}
```

### List All Memberships

```csharp
await foreach (var membership in cf.User.ListAllMembershipsAsync())
{
    Console.WriteLine($"{membership.Account.Name} [{membership.Status}]");
}
```

### Filter by Status

```csharp
// Get pending invitations
var pending = await cf.User.ListMembershipsAsync(
    new ListMembershipsFilters(Status: MemberStatus.Pending));

foreach (var membership in pending.Items)
{
    Console.WriteLine($"Invitation from: {membership.Account.Name}");
}
```

## Getting Membership Details

```csharp
var membership = await cf.User.GetMembershipAsync(membershipId);

Console.WriteLine($"Account: {membership.Account.Name}");
Console.WriteLine($"Account ID: {membership.Account.Id}");
Console.WriteLine($"Status: {membership.Status}");
Console.WriteLine($"API Access: {membership.ApiAccessEnabled}");

Console.WriteLine("Roles:");
foreach (var role in membership.Roles)
{
    Console.WriteLine($"  - {role.Name}");
}
```

## Accepting/Rejecting Invitations

> [!NOTE]
> **Preview:** This operation has limited test coverage.

### Accept an Invitation

```csharp
var result = await cf.User.UpdateMembershipAsync(membershipId,
    new UpdateMembershipRequest(MemberStatus.Accepted));

Console.WriteLine($"Accepted invitation to {result.Account.Name}");
```

### Reject an Invitation

```csharp
var result = await cf.User.UpdateMembershipAsync(membershipId,
    new UpdateMembershipRequest(MemberStatus.Rejected));

Console.WriteLine($"Rejected invitation from {result.Account.Name}");
```

> [!WARNING]
> Once an invitation is accepted or rejected, its status cannot be changed.

## Leaving an Account

> [!NOTE]
> **Preview:** This operation has limited test coverage.

Remove yourself from an account:

```csharp
await cf.User.DeleteMembershipAsync(membershipId);
Console.WriteLine("Left the account");
```

> [!WARNING]
> Leaving an account removes your access immediately. You must be re-invited to regain access.

## Models Reference

### Membership

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Membership identifier |
| `Account` | `MembershipAccount` | Account information |
| `Status` | `MemberStatus` | Membership status |
| `Roles` | `IReadOnlyList<AccountRole>` | Assigned roles |
| `ApiAccessEnabled` | `bool?` | Whether API access is enabled |
| `Permissions` | `IReadOnlyList<string>?` | Granted permissions |

### MembershipAccount

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Account identifier |
| `Name` | `string` | Account name |

### MemberStatus (Extensible Enum)

| Known Value | Description |
|-------------|-------------|
| `Pending` | Invitation awaiting response |
| `Accepted` | Active membership |
| `Rejected` | Invitation was rejected |

### ListMembershipsFilters

| Property | Type | Description |
|----------|------|-------------|
| `Status` | `MemberStatus?` | Filter by status |
| `AccountName` | `string?` | Filter by account name |
| `Page` | `int?` | Page number (1-based) |
| `PerPage` | `int?` | Results per page |
| `Order` | `string?` | Field to order by |
| `Direction` | `ListOrderDirection?` | Sort direction |

## Common Patterns

### Find Account by Name

```csharp
public async Task<Membership?> FindAccountByNameAsync(string accountName)
{
    await foreach (var membership in cf.User.ListAllMembershipsAsync())
    {
        if (membership.Account.Name.Contains(accountName, StringComparison.OrdinalIgnoreCase))
        {
            return membership;
        }
    }

    return null;
}
```

### Accept All Pending Invitations

```csharp
public async Task AcceptAllPendingAsync()
{
    var filters = new ListMembershipsFilters(Status: MemberStatus.Pending);

    await foreach (var membership in cf.User.ListAllMembershipsAsync(filters))
    {
        await cf.User.UpdateMembershipAsync(membership.Id,
            new UpdateMembershipRequest(MemberStatus.Accepted));

        Console.WriteLine($"Accepted: {membership.Account.Name}");
    }
}
```

### List Accounts with Specific Permission

```csharp
public async Task ListAccountsWithDnsAccessAsync()
{
    Console.WriteLine("Accounts with DNS access:");

    await foreach (var membership in cf.User.ListAllMembershipsAsync())
    {
        if (membership.Status != MemberStatus.Accepted)
            continue;

        var hasDns = membership.Roles.Any(r =>
            r.Permissions?.DnsRecords?.Write == true);

        if (hasDns)
        {
            Console.WriteLine($"  - {membership.Account.Name}");
        }
    }
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Memberships | User | Read (for listing/get) |
| Memberships | User | Write (for accept/reject/leave) |

## Related

- [User Invitations](invitations.md) - Alternative invitation management
- [Account Members](../accounts/members.md) - Account-side member management
- [Account Roles](../accounts/roles.md) - View available roles
