# User API Tokens

Manage your personal API tokens. User tokens are owned by you and can access resources across all your account memberships.

## Overview

Access user token management through `cf.ApiTokens`:

```csharp
public class UserTokenService(ICloudflareApiClient cf)
{
    public async Task<CreateApiTokenResult> CreatePersonalTokenAsync(string name)
    {
        return await cf.ApiTokens.CreateUserTokenAsync(
            new CreateApiTokenRequest(
                Name: name,
                Policies: new[]
                {
                    new CreateTokenPolicyRequest(
                        Effect: "allow",
                        PermissionGroups: new[] { new TokenPermissionGroupReference(groupId) },
                        Resources: new Dictionary<string, string>
                        {
                            ["com.cloudflare.api.user"] = "*"
                        }
                    )
                }
            ));
    }
}
```

> [!WARNING]
> The token secret is only returned when created. Store it securely - it cannot be retrieved again.

## Listing Tokens

### With Pagination

```csharp
var result = await cf.ApiTokens.ListUserTokensAsync(
    new ListApiTokensFilters(Page: 1, PerPage: 50));

foreach (var token in result.Items)
{
    Console.WriteLine($"{token.Name}: {token.Status}");
    Console.WriteLine($"  Last used: {token.LastUsedOn}");
    Console.WriteLine($"  Expires: {token.ExpiresOn}");
}
```

### List All Tokens

```csharp
await foreach (var token in cf.ApiTokens.ListAllUserTokensAsync())
{
    Console.WriteLine($"{token.Name} [{token.Status}]");
}
```

## Getting Token Details

```csharp
var token = await cf.ApiTokens.GetUserTokenAsync(tokenId);

Console.WriteLine($"Name: {token.Name}");
Console.WriteLine($"Status: {token.Status}");
Console.WriteLine($"Created: {token.IssuedOn}");
Console.WriteLine($"Expires: {token.ExpiresOn}");
Console.WriteLine($"Last Used: {token.LastUsedOn}");
```

## Creating Tokens

### Basic Token

```csharp
var result = await cf.ApiTokens.CreateUserTokenAsync(
    new CreateApiTokenRequest(
        Name: "My Personal Token",
        Policies: new[]
        {
            new CreateTokenPolicyRequest(
                Effect: "allow",
                PermissionGroups: new[]
                {
                    new TokenPermissionGroupReference(permissionGroupId)
                },
                Resources: new Dictionary<string, string>
                {
                    ["com.cloudflare.api.user"] = "*"
                }
            )
        }
    ));

// IMPORTANT: Store this securely!
Console.WriteLine($"Token: {result.Value}");
```

### Token with Expiration

```csharp
var result = await cf.ApiTokens.CreateUserTokenAsync(
    new CreateApiTokenRequest(
        Name: "Temporary Token",
        Policies: new[]
        {
            new CreateTokenPolicyRequest(
                Effect: "allow",
                PermissionGroups: new[] { new TokenPermissionGroupReference(groupId) },
                Resources: new Dictionary<string, string>
                {
                    ["com.cloudflare.api.user"] = "*"
                }
            )
        },
        ExpiresOn: DateTime.UtcNow.AddYears(1)
    ));
```

## Updating Tokens

```csharp
// Disable a token
var updated = await cf.ApiTokens.UpdateUserTokenAsync(tokenId,
    new UpdateApiTokenRequest(
        Name: "Disabled Token",
        Policies: existingPolicies,
        Status: TokenStatus.Disabled
    ));
```

## Deleting Tokens

```csharp
await cf.ApiTokens.DeleteUserTokenAsync(tokenId);
```

## Verifying Tokens

Check if the current token is valid:

```csharp
var result = await cf.ApiTokens.VerifyUserTokenAsync();

Console.WriteLine($"Token ID: {result.Id}");
Console.WriteLine($"Status: {result.Status}");

if (result.ExpiresOn.HasValue)
{
    var remaining = result.ExpiresOn.Value - DateTime.UtcNow;
    Console.WriteLine($"Expires in: {remaining.Days} days");
}
```

## Rolling (Rotating) Tokens

Generate a new secret for an existing token:

```csharp
var newValue = await cf.ApiTokens.RollUserTokenAsync(tokenId);

// Store the new value securely
Console.WriteLine($"New token value: {newValue}");
```

> [!WARNING]
> Rolling a token immediately invalidates the old value.

## Permission Groups

### List Available Permission Groups

```csharp
var result = await cf.ApiTokens.GetUserPermissionGroupsAsync();

foreach (var group in result.Items)
{
    Console.WriteLine($"{group.Name} ({group.Id})");
}
```

### List All Permission Groups

```csharp
await foreach (var group in cf.ApiTokens.GetAllUserPermissionGroupsAsync())
{
    Console.WriteLine($"{group.Name}: {group.Id}");

    foreach (var scope in group.Scopes)
    {
        Console.WriteLine($"  - {scope}");
    }
}
```

## User vs Account Tokens

| Feature | User Tokens | Account Tokens |
|---------|-------------|----------------|
| Ownership | Personal | Account |
| Scope | All your memberships | Single account |
| Management | `cf.ApiTokens.ListUserTokensAsync` | `cf.ApiTokens.ListAccountTokensAsync` |
| Best for | Personal automation | Shared team access |

## Models Reference

### ApiToken

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Token identifier |
| `Name` | `string` | Token name |
| `Status` | `TokenStatus` | Token status |
| `Policies` | `IReadOnlyList<TokenPolicy>` | Access policies |
| `IssuedOn` | `DateTime?` | Creation timestamp |
| `ExpiresOn` | `DateTime?` | Expiration timestamp |
| `NotBefore` | `DateTime?` | Validity start |
| `LastUsedOn` | `DateTime?` | Last usage |

### TokenStatus (Extensible Enum)

<xref:Cloudflare.NET.ApiTokens.Models.TokenStatus> represents the current state of an API token:

| Known Value | API Value | Description |
|-------------|-----------|-------------|
| `Active` | `active` | Token is active and can be used for API requests |
| `Disabled` | `disabled` | Token has been manually disabled |
| `Expired` | `expired` | Token has passed its expiration date |

```csharp
using Cloudflare.NET.ApiTokens.Models;

// Check token status
if (token.Status == TokenStatus.Active)
{
    Console.WriteLine("Token is ready to use");
}

// Extensible for future status values
TokenStatus customStatus = "pending";
```

## Common Patterns

### Find Expiring Tokens

```csharp
public async Task AlertExpiringTokensAsync(int daysWarning = 30)
{
    var threshold = DateTime.UtcNow.AddDays(daysWarning);

    await foreach (var token in cf.ApiTokens.ListAllUserTokensAsync())
    {
        if (token.ExpiresOn.HasValue && token.ExpiresOn.Value < threshold)
        {
            var remaining = token.ExpiresOn.Value - DateTime.UtcNow;
            Console.WriteLine($"[WARNING] {token.Name} expires in {remaining.Days} days");
        }
    }
}
```

### Rotate All Active Tokens

```csharp
public async Task<Dictionary<string, string>> RotateAllTokensAsync()
{
    var newTokens = new Dictionary<string, string>();

    await foreach (var token in cf.ApiTokens.ListAllUserTokensAsync())
    {
        if (token.Status == TokenStatus.Active)
        {
            var newValue = await cf.ApiTokens.RollUserTokenAsync(token.Id);
            newTokens[token.Name] = newValue;
            Console.WriteLine($"Rotated: {token.Name}");
        }
    }

    return newTokens;
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| API Tokens | User | Read (for listing/get/verify) |
| API Tokens | User | Write (for create/update/delete/roll) |

## Related

- [Account API Tokens](../accounts/api-tokens.md) - Account-scoped tokens
- [User Profile](profile.md) - Your profile settings
