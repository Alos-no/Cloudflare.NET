# Account API Tokens

Manage API tokens for Cloudflare accounts. API tokens provide fine-grained access control for API operations.

## Overview

Access API token management through `cf.ApiTokens`:

```csharp
public class TokenService(ICloudflareApiClient cf)
{
    public async Task<CreateApiTokenResult> CreateTokenAsync(
        string accountId,
        string name,
        string permissionGroupId)
    {
        return await cf.ApiTokens.CreateAccountTokenAsync(accountId,
            new CreateApiTokenRequest(
                Name: name,
                Policies: new[]
                {
                    new CreateTokenPolicyRequest(
                        Effect: "allow",
                        PermissionGroups: new[] { new TokenPermissionGroupReference(permissionGroupId) },
                        Resources: new Dictionary<string, string>
                        {
                            ["com.cloudflare.api.account.*"] = "*"
                        }
                    )
                }
            ));
    }
}
```

> [!WARNING]
> The token secret is only returned when the token is created. Store it securely - it cannot be retrieved again.

## Listing Tokens

### With Pagination

```csharp
var result = await cf.ApiTokens.ListAccountTokensAsync(accountId,
    new ListApiTokensFilters(Page: 1, PerPage: 50));

foreach (var token in result.Items)
{
    Console.WriteLine($"{token.Name}: {token.Status}");
    Console.WriteLine($"  Expires: {token.ExpiresOn}");
}
```

### List All Tokens

```csharp
await foreach (var token in cf.ApiTokens.ListAllAccountTokensAsync(accountId))
{
    Console.WriteLine($"{token.Name} [{token.Status}]");
}
```

## Getting Token Details

```csharp
var token = await cf.ApiTokens.GetAccountTokenAsync(accountId, tokenId);

Console.WriteLine($"Name: {token.Name}");
Console.WriteLine($"Status: {token.Status}");
Console.WriteLine($"Created: {token.IssuedOn}");
Console.WriteLine($"Expires: {token.ExpiresOn}");
Console.WriteLine($"Last Used: {token.LastUsedOn}");
```

## Creating Tokens

### Basic Token

```csharp
var result = await cf.ApiTokens.CreateAccountTokenAsync(accountId,
    new CreateApiTokenRequest(
        Name: "CI/CD Token",
        Policies: new[]
        {
            new CreateTokenPolicyRequest(
                Effect: "allow",
                PermissionGroups: new[]
                {
                    new TokenPermissionGroupReference(dnsWriteGroupId)
                },
                Resources: new Dictionary<string, string>
                {
                    ["com.cloudflare.api.account.*"] = "*"
                }
            )
        }
    ));

// IMPORTANT: Store this securely!
Console.WriteLine($"Token: {result.Value}");
Console.WriteLine($"Token ID: {result.Id}");
```

### Token with Expiration

```csharp
var result = await cf.ApiTokens.CreateAccountTokenAsync(accountId,
    new CreateApiTokenRequest(
        Name: "Temporary Access",
        Policies: new[]
        {
            new CreateTokenPolicyRequest(
                Effect: "allow",
                PermissionGroups: new[] { new TokenPermissionGroupReference(groupId) },
                Resources: new Dictionary<string, string>
                {
                    ["com.cloudflare.api.account.*"] = "*"
                }
            )
        },
        ExpiresOn: DateTime.UtcNow.AddDays(30)
    ));
```

### Token with IP Restrictions

```csharp
var result = await cf.ApiTokens.CreateAccountTokenAsync(accountId,
    new CreateApiTokenRequest(
        Name: "Office Only Token",
        Policies: new[]
        {
            new CreateTokenPolicyRequest(
                Effect: "allow",
                PermissionGroups: new[] { new TokenPermissionGroupReference(groupId) },
                Resources: new Dictionary<string, string>
                {
                    ["com.cloudflare.api.account.*"] = "*"
                }
            )
        },
        Condition: new TokenCondition(
            RequestIp: new IpCondition(
                In: new[] { "192.0.2.0/24", "198.51.100.0/24" }
            )
        )
    ));
```

### Token with Time Restrictions

```csharp
var result = await cf.ApiTokens.CreateAccountTokenAsync(accountId,
    new CreateApiTokenRequest(
        Name: "Scheduled Token",
        Policies: new[]
        {
            new CreateTokenPolicyRequest(
                Effect: "allow",
                PermissionGroups: new[] { new TokenPermissionGroupReference(groupId) },
                Resources: new Dictionary<string, string>
                {
                    ["com.cloudflare.api.account.*"] = "*"
                }
            )
        },
        NotBefore: DateTime.UtcNow.AddDays(1),   // Starts tomorrow
        ExpiresOn: DateTime.UtcNow.AddDays(8)    // Expires in 8 days
    ));
```

## Updating Tokens

```csharp
// Disable a token
var updated = await cf.ApiTokens.UpdateAccountTokenAsync(accountId, tokenId,
    new UpdateApiTokenRequest(
        Name: "Disabled Token",
        Policies: existingPolicies,
        Status: TokenStatus.Disabled
    ));
```

## Deleting Tokens

```csharp
await cf.ApiTokens.DeleteAccountTokenAsync(accountId, tokenId);
```

## Verifying Tokens

Check if the current token is valid:

```csharp
var result = await cf.ApiTokens.VerifyAccountTokenAsync(accountId);

Console.WriteLine($"Token ID: {result.Id}");
Console.WriteLine($"Status: {result.Status}");

if (result.ExpiresOn.HasValue)
{
    Console.WriteLine($"Expires: {result.ExpiresOn}");
}
```

## Rolling (Rotating) Tokens

Generate a new secret for an existing token:

```csharp
// The old token value becomes invalid immediately
var newValue = await cf.ApiTokens.RollAccountTokenAsync(accountId, tokenId);

// Store the new value securely
Console.WriteLine($"New token value: {newValue}");
```

> [!WARNING]
> Rolling a token immediately invalidates the old value. All clients using the old value must be updated.

## Permission Groups

### List Available Permission Groups

```csharp
var result = await cf.ApiTokens.GetAccountPermissionGroupsAsync(accountId);

foreach (var group in result.Items)
{
    Console.WriteLine($"{group.Name} ({group.Id})");
    Console.WriteLine($"  Scopes: {string.Join(", ", group.Scopes)}");
}
```

### List All Permission Groups

```csharp
await foreach (var group in cf.ApiTokens.GetAllAccountPermissionGroupsAsync(accountId))
{
    Console.WriteLine($"{group.Name}: {group.Id}");
}
```

## Models Reference

### ApiToken

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Token identifier |
| `Name` | `string` | Token name |
| `Status` | `TokenStatus` | Token status (active, disabled, expired) |
| `Policies` | `IReadOnlyList<TokenPolicy>` | Access policies |
| `IssuedOn` | `DateTime?` | When the token was created |
| `ExpiresOn` | `DateTime?` | When the token expires |
| `NotBefore` | `DateTime?` | When the token becomes valid |
| `LastUsedOn` | `DateTime?` | Last usage timestamp |
| `Condition` | `TokenCondition?` | IP/time restrictions |

### TokenStatus (Extensible Enum)

| Known Value | Description |
|-------------|-------------|
| `Active` | Token is active and can be used |
| `Disabled` | Token is disabled |
| `Expired` | Token has expired |

### CreateApiTokenResult

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Token identifier |
| `Value` | `string` | Token secret (only returned on creation) |

### PermissionGroup

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Permission group identifier |
| `Name` | `string` | Permission group name |
| `Scopes` | `IReadOnlyList<string>` | Scopes included in the group |

## Common Patterns

### Create Read-Only Zone Token

```csharp
public async Task<string> CreateZoneReadTokenAsync(string accountId, string zoneId)
{
    // Find the DNS Read permission group
    PermissionGroup? dnsRead = null;

    await foreach (var group in cf.ApiTokens.GetAllAccountPermissionGroupsAsync(accountId))
    {
        if (group.Name.Contains("DNS") && group.Scopes.Any(s => s.Contains("read")))
        {
            dnsRead = group;
            break;
        }
    }

    if (dnsRead is null)
        throw new InvalidOperationException("DNS Read permission group not found");

    var result = await cf.ApiTokens.CreateAccountTokenAsync(accountId,
        new CreateApiTokenRequest(
            Name: $"Read-Only Token for Zone {zoneId}",
            Policies: new[]
            {
                new CreateTokenPolicyRequest(
                    Effect: "allow",
                    PermissionGroups: new[] { new TokenPermissionGroupReference(dnsRead.Id) },
                    Resources: new Dictionary<string, string>
                    {
                        [$"com.cloudflare.api.account.zone.{zoneId}"] = "*"
                    }
                )
            }
        ));

    return result.Value;
}
```

### Rotate All Tokens

```csharp
public async Task RotateAllTokensAsync(string accountId)
{
    await foreach (var token in cf.ApiTokens.ListAllAccountTokensAsync(accountId))
    {
        if (token.Status == TokenStatus.Active)
        {
            var newValue = await cf.ApiTokens.RollAccountTokenAsync(accountId, token.Id);
            Console.WriteLine($"Rotated {token.Name}: {newValue}");
        }
    }
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| API Tokens | Account | Read (for listing/get) |
| API Tokens | Account | Write (for create/update/delete/roll) |

## Related

- [User API Tokens](../user/api-tokens.md) - Manage user-level tokens
- [Account Management](account-management.md) - Manage accounts
