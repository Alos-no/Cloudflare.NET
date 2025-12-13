# User Profile

View and edit the authenticated user's profile information.

## Overview

Access user profile through `cf.User`:

```csharp
public class ProfileService(ICloudflareApiClient cf)
{
    public async Task<User> GetProfileAsync()
    {
        return await cf.User.GetUserAsync();
    }
}
```

## Getting Your Profile

```csharp
var user = await cf.User.GetUserAsync();

Console.WriteLine($"Hello, {user.FirstName}!");
Console.WriteLine($"Email: {user.Email}");
Console.WriteLine($"2FA Enabled: {user.TwoFactorAuthenticationEnabled}");
Console.WriteLine($"Country: {user.Country}");
Console.WriteLine($"Suspended: {user.Suspended}");
```

## Editing Your Profile

### Update Name

```csharp
var updated = await cf.User.EditUserAsync(
    new EditUserRequest(
        FirstName: "John",
        LastName: "Doe"
    ));

Console.WriteLine($"Updated name: {updated.FirstName} {updated.LastName}");
```

### Update Contact Information

```csharp
var updated = await cf.User.EditUserAsync(
    new EditUserRequest(
        FirstName: "John",
        LastName: "Doe",
        Country: "US",
        Telephone: "+1-555-555-5555",
        Zipcode: "12345"
    ));
```

> [!NOTE]
> Only the following fields can be edited: FirstName, LastName, Country, Telephone, and Zipcode. Email, 2FA settings, and suspension status are managed through separate Cloudflare flows.

## Models Reference

### User

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | User identifier |
| `Email` | `string` | Email address |
| `FirstName` | `string?` | First name |
| `LastName` | `string?` | Last name |
| `Username` | `string?` | Username |
| `Telephone` | `string?` | Phone number |
| `Country` | `string?` | Country code |
| `Zipcode` | `string?` | Postal/ZIP code |
| `TwoFactorAuthenticationEnabled` | `bool` | Whether 2FA is enabled |
| `Suspended` | `bool` | Whether the account is suspended |
| `CreatedOn` | `DateTime` | Account creation date |
| `ModifiedOn` | `DateTime` | Last modification date |

### EditUserRequest

| Property | Type | Description |
|----------|------|-------------|
| `FirstName` | `string?` | New first name |
| `LastName` | `string?` | New last name |
| `Country` | `string?` | New country code |
| `Telephone` | `string?` | New phone number |
| `Zipcode` | `string?` | New postal/ZIP code |

## Common Patterns

### Display Profile Summary

```csharp
public async Task DisplayProfileAsync()
{
    var user = await cf.User.GetUserAsync();

    Console.WriteLine("=== Your Profile ===");
    Console.WriteLine($"Name: {user.FirstName} {user.LastName}");
    Console.WriteLine($"Email: {user.Email}");
    Console.WriteLine($"Created: {user.CreatedOn}");
    Console.WriteLine();

    Console.WriteLine("Security:");
    Console.WriteLine($"  2FA: {(user.TwoFactorAuthenticationEnabled ? "Enabled" : "Not Enabled")}");

    if (user.Suspended)
    {
        Console.WriteLine("  [WARNING: Account Suspended]");
    }
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| User Details | User | Read (for get) |
| User Details | User | Write (for edit) |

## Related

- [User Memberships](memberships.md) - Manage account memberships
- [User Invitations](invitations.md) - Accept/reject invitations
- [User API Tokens](api-tokens.md) - Manage personal tokens
