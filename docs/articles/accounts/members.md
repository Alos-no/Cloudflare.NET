# Account Members

Manage account members and their access permissions. Members are users who have been granted access to an account's resources.

## Overview

Access member management through `cf.Members`:

```csharp
public class MemberService(ICloudflareApiClient cf)
{
    public async Task<AccountMember> InviteMemberAsync(
        string accountId,
        string email,
        string roleId)
    {
        return await cf.Members.CreateAccountMemberAsync(accountId,
            new CreateAccountMemberRequest(
                Email: email,
                Roles: new[] { roleId }
            ));
    }
}
```

## Listing Members

### With Pagination

```csharp
var result = await cf.Members.ListAccountMembersAsync(accountId,
    new ListAccountMembersFilters(
        Status: MemberStatus.Accepted,
        Page: 1,
        PerPage: 50
    ));

foreach (var member in result.Items)
{
    Console.WriteLine($"{member.User.Email}: {member.Status}");
    Console.WriteLine($"  Roles: {string.Join(", ", member.Roles.Select(r => r.Name))}");
}
```

### List All Members

```csharp
await foreach (var member in cf.Members.ListAllAccountMembersAsync(accountId))
{
    Console.WriteLine($"{member.User.Email} [{member.Status}]");
}
```

### Filter by Status

```csharp
// List pending invitations
var pending = await cf.Members.ListAccountMembersAsync(accountId,
    new ListAccountMembersFilters(Status: MemberStatus.Pending));

// List active members
var active = await cf.Members.ListAccountMembersAsync(accountId,
    new ListAccountMembersFilters(Status: MemberStatus.Accepted));
```

## Getting Member Details

```csharp
var member = await cf.Members.GetAccountMemberAsync(accountId, memberId);

Console.WriteLine($"Email: {member.User.Email}");
Console.WriteLine($"Status: {member.Status}");
Console.WriteLine($"Roles:");

foreach (var role in member.Roles)
{
    Console.WriteLine($"  - {role.Name}: {role.Description}");
}
```

## Inviting Members

> [!NOTE]
> **Preview:** This operation has limited test coverage.

### Basic Invitation

```csharp
var member = await cf.Members.CreateAccountMemberAsync(accountId,
    new CreateAccountMemberRequest(
        Email: "developer@example.com",
        Roles: new[] { dnsAdminRoleId }
    ));

Console.WriteLine($"Invited {member.User.Email}, status: {member.Status}");
```

### Invitation with Multiple Roles

```csharp
var member = await cf.Members.CreateAccountMemberAsync(accountId,
    new CreateAccountMemberRequest(
        Email: "admin@example.com",
        Roles: new[] { adminRoleId, billingRoleId }
    ));
```

> [!NOTE]
> An invitation email is sent to the specified address. The user must accept the invitation to become an active member.

## Updating Members

> [!NOTE]
> **Preview:** This operation has limited test coverage.

Update a member's roles:

```csharp
var updated = await cf.Members.UpdateAccountMemberAsync(accountId, memberId,
    new UpdateAccountMemberRequest(
        Roles: new[] { newRoleId, additionalRoleId }
    ));

Console.WriteLine($"Updated roles for {updated.User.Email}");
```

> [!NOTE]
> Only roles can be updated. The member's email and status cannot be changed through this API.

## Removing Members

> [!NOTE]
> **Preview:** This operation has limited test coverage.

```csharp
var result = await cf.Members.DeleteAccountMemberAsync(accountId, memberId);
Console.WriteLine($"Removed member: {result.Id}");
```

> [!WARNING]
> Removing a member immediately revokes their access. They must be re-invited to regain access.

> [!NOTE]
> You cannot remove yourself from an account.

## Models Reference

### AccountMember

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Membership identifier |
| `User` | `MemberUser` | User information |
| `Status` | `MemberStatus` | Membership status |
| `Roles` | `IReadOnlyList<AccountRole>` | Assigned roles |

### MemberUser

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | User identifier |
| `Email` | `string` | User's email address |
| `FirstName` | `string?` | User's first name |
| `LastName` | `string?` | User's last name |
| `TwoFactorAuthenticationEnabled` | `bool` | Whether 2FA is enabled |

### MemberStatus (Extensible Enum)

| Known Value | Description |
|-------------|-------------|
| `Pending` | Invitation sent, awaiting acceptance |
| `Accepted` | User has accepted and is an active member |
| `Rejected` | User rejected the invitation |

### ListAccountMembersFilters

| Property | Type | Description |
|----------|------|-------------|
| `Status` | `MemberStatus?` | Filter by status |
| `Page` | `int?` | Page number (1-based) |
| `PerPage` | `int?` | Results per page |
| `Order` | `string?` | Field to order by |
| `Direction` | `ListOrderDirection?` | Sort direction |

## Common Patterns

### Find Member by Email

```csharp
public async Task<AccountMember?> FindMemberByEmailAsync(
    string accountId,
    string email)
{
    await foreach (var member in cf.Members.ListAllAccountMembersAsync(accountId))
    {
        if (member.User.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
        {
            return member;
        }
    }

    return null;
}
```

### Invite or Update Member

```csharp
public async Task<AccountMember> EnsureMemberHasRolesAsync(
    string accountId,
    string email,
    IReadOnlyList<string> roleIds)
{
    var existing = await FindMemberByEmailAsync(accountId, email);

    if (existing is not null)
    {
        return await cf.Members.UpdateAccountMemberAsync(accountId, existing.Id,
            new UpdateAccountMemberRequest(Roles: roleIds));
    }

    return await cf.Members.CreateAccountMemberAsync(accountId,
        new CreateAccountMemberRequest(Email: email, Roles: roleIds));
}
```

### Audit Member Roles

```csharp
public async Task AuditMemberRolesAsync(string accountId)
{
    Console.WriteLine("=== Member Role Audit ===");

    await foreach (var member in cf.Members.ListAllAccountMembersAsync(accountId))
    {
        Console.WriteLine($"\n{member.User.Email} ({member.Status}):");

        foreach (var role in member.Roles)
        {
            Console.WriteLine($"  - {role.Name}");
        }

        if (member.User.TwoFactorAuthenticationEnabled)
        {
            Console.WriteLine("  [2FA Enabled]");
        }
        else
        {
            Console.WriteLine("  [WARNING: 2FA Not Enabled]");
        }
    }
}
```

### Remove Inactive Members

```csharp
public async Task RemoveRejectedInvitationsAsync(string accountId)
{
    var filters = new ListAccountMembersFilters(Status: MemberStatus.Rejected);

    await foreach (var member in cf.Members.ListAllAccountMembersAsync(accountId, filters))
    {
        await cf.Members.DeleteAccountMemberAsync(accountId, member.Id);
        Console.WriteLine($"Removed rejected invitation for: {member.User.Email}");
    }
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Account Members | Account | Read (for listing/get) |
| Account Members | Account | Write (for invite/update/remove) |

## Related

- [Account Roles](roles.md) - View available roles
- [User Memberships](../user/memberships.md) - User's view of memberships
- [Account Management](account-management.md) - Manage accounts
