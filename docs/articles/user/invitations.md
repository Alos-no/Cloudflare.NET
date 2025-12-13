# User Invitations

View and respond to account invitations sent to you. This is an alternative to the Memberships API for handling invitations.

## Overview

Access invitations through `cf.User`:

```csharp
public class InvitationService(ICloudflareApiClient cf)
{
    public async Task<IReadOnlyList<UserInvitation>> GetPendingInvitationsAsync()
    {
        return await cf.User.ListInvitationsAsync();
    }
}
```

## Listing Invitations

> [!NOTE]
> **Preview:** This operation has limited test coverage.

```csharp
var invitations = await cf.User.ListInvitationsAsync();

foreach (var invite in invitations)
{
    Console.WriteLine($"Invitation to: {invite.OrganizationName}");
    Console.WriteLine($"  Status: {invite.Status}");
    Console.WriteLine($"  Expires: {invite.ExpiresOn}");

    if (invite.Roles is not null)
    {
        Console.WriteLine($"  Roles: {string.Join(", ", invite.Roles.Select(r => r.Name))}");
    }
}
```

## Getting Invitation Details

> [!NOTE]
> **Preview:** This operation has limited test coverage.

```csharp
var invitation = await cf.User.GetInvitationAsync(invitationId);

Console.WriteLine($"Organization: {invitation.OrganizationName}");
Console.WriteLine($"Status: {invitation.Status}");
Console.WriteLine($"Invited: {invitation.InvitedOn}");
Console.WriteLine($"Expires: {invitation.ExpiresOn}");

if (invitation.Roles is not null)
{
    Console.WriteLine("Roles offered:");
    foreach (var role in invitation.Roles)
    {
        Console.WriteLine($"  - {role.Name}");
    }
}
```

## Accepting Invitations

> [!NOTE]
> **Preview:** This operation has limited test coverage.

```csharp
var result = await cf.User.RespondToInvitationAsync(invitationId,
    new RespondToInvitationRequest(MemberStatus.Accepted));

Console.WriteLine($"Invitation {result.Status}!");
```

> [!NOTE]
> Upon acceptance, you become a member of the account with the roles specified in the invitation.

## Rejecting Invitations

> [!NOTE]
> **Preview:** This operation has limited test coverage.

```csharp
var result = await cf.User.RespondToInvitationAsync(invitationId,
    new RespondToInvitationRequest(MemberStatus.Rejected));

Console.WriteLine($"Invitation rejected");
```

> [!WARNING]
> Once an invitation is accepted or rejected, its status cannot be changed. The account admin may send a new invitation.

## Invitations vs Memberships

Both APIs can be used to accept/reject invitations:

| Feature | Invitations API | Memberships API |
|---------|-----------------|-----------------|
| View pending | `ListInvitationsAsync` | Filter by `Status: Pending` |
| Accept | `RespondToInvitationAsync` | `UpdateMembershipAsync` |
| Reject | `RespondToInvitationAsync` | `UpdateMembershipAsync` |
| Leave account | N/A | `DeleteMembershipAsync` |

Use whichever API fits your workflow. The Memberships API provides more features including leaving accounts and viewing active memberships.

## Models Reference

### UserInvitation

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Invitation identifier |
| `OrganizationId` | `string` | Account/organization identifier |
| `OrganizationName` | `string` | Account/organization name |
| `InvitedMemberEmail` | `string` | Email the invitation was sent to |
| `Status` | `InvitationStatus` | Current status |
| `Roles` | `IReadOnlyList<AccountRole>?` | Roles offered |
| `InvitedBy` | `string?` | Who sent the invitation |
| `InvitedOn` | `DateTime` | When the invitation was sent |
| `ExpiresOn` | `DateTime?` | When the invitation expires |

### InvitationStatus (Extensible Enum)

| Known Value | Description |
|-------------|-------------|
| `Pending` | Awaiting response |
| `Accepted` | Invitation accepted |
| `Rejected` | Invitation rejected |
| `Expired` | Invitation expired |

### RespondToInvitationRequest

| Property | Type | Description |
|----------|------|-------------|
| `Status` | `MemberStatus` | New status (Accepted or Rejected) |

## Common Patterns

### Accept All Invitations from Trusted Organization

```csharp
public async Task AcceptFromOrganizationAsync(string orgName)
{
    var invitations = await cf.User.ListInvitationsAsync();

    foreach (var invite in invitations)
    {
        if (invite.OrganizationName.Contains(orgName, StringComparison.OrdinalIgnoreCase) &&
            invite.Status == InvitationStatus.Pending)
        {
            await cf.User.RespondToInvitationAsync(invite.Id,
                new RespondToInvitationRequest(MemberStatus.Accepted));

            Console.WriteLine($"Accepted: {invite.OrganizationName}");
        }
    }
}
```

### Review Invitations Interactively

```csharp
public async Task ReviewInvitationsAsync()
{
    var invitations = await cf.User.ListInvitationsAsync();
    var pending = invitations.Where(i => i.Status == InvitationStatus.Pending);

    foreach (var invite in pending)
    {
        Console.WriteLine($"\nInvitation from: {invite.OrganizationName}");
        Console.WriteLine($"Invited by: {invite.InvitedBy}");
        Console.WriteLine($"Expires: {invite.ExpiresOn}");

        if (invite.Roles is not null)
        {
            Console.WriteLine($"Roles: {string.Join(", ", invite.Roles.Select(r => r.Name))}");
        }

        Console.Write("Accept? (y/n): ");
        var response = Console.ReadLine()?.ToLower();

        var status = response == "y" ? MemberStatus.Accepted : MemberStatus.Rejected;

        await cf.User.RespondToInvitationAsync(invite.Id,
            new RespondToInvitationRequest(status));

        Console.WriteLine($"Invitation {status}");
    }
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| User Invites | User | Read (for listing/get) |
| User Invites | User | Write (for accept/reject) |

## Related

- [User Memberships](memberships.md) - Alternative membership management
- [Account Members](../accounts/members.md) - Send invitations
