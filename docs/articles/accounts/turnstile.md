# Turnstile Widgets

Manage Turnstile widgets for bot protection. Turnstile is Cloudflare's CAPTCHA alternative that provides protection without user friction.

## Overview

Access Turnstile through `cf.Turnstile`:

```csharp
public class TurnstileService(ICloudflareApiClient cf)
{
    public async Task<TurnstileWidget> CreateWidgetAsync(
        string accountId,
        string name,
        IEnumerable<string> domains)
    {
        return await cf.Turnstile.CreateWidgetAsync(accountId,
            new CreateTurnstileWidgetRequest(
                Name: name,
                Domains: domains.ToList(),
                Mode: WidgetMode.Managed
            ));
    }
}
```

> [!WARNING]
> Widget secrets are only returned when created or rotated. Store them securely.

## Listing Widgets

### With Pagination

```csharp
var result = await cf.Turnstile.ListWidgetsAsync(accountId,
    new ListTurnstileWidgetsFilters(
        Order: TurnstileOrderField.CreatedOn,
        Direction: ListOrderDirection.Desc
    ));

foreach (var widget in result.Result)
{
    Console.WriteLine($"{widget.Name}: {widget.Sitekey}");
    Console.WriteLine($"  Mode: {widget.Mode}");
    Console.WriteLine($"  Domains: {string.Join(", ", widget.Domains)}");
}
```

### List All Widgets

```csharp
await foreach (var widget in cf.Turnstile.ListAllWidgetsAsync(accountId))
{
    Console.WriteLine($"{widget.Name} [{widget.Mode}]");
}
```

## Getting Widget Details

```csharp
var widget = await cf.Turnstile.GetWidgetAsync(accountId, sitekey);

Console.WriteLine($"Name: {widget.Name}");
Console.WriteLine($"Sitekey: {widget.Sitekey}");
Console.WriteLine($"Mode: {widget.Mode}");
Console.WriteLine($"Domains: {string.Join(", ", widget.Domains)}");
Console.WriteLine($"Bot Fight Mode: {widget.BotFightMode}");
Console.WriteLine($"Created: {widget.CreatedOn}");
```

> [!NOTE]
> The secret is **not** returned when getting widget details. It's only available at creation or after rotation.

## Creating Widgets

### Basic Widget

```csharp
var widget = await cf.Turnstile.CreateWidgetAsync(accountId,
    new CreateTurnstileWidgetRequest(
        Name: "Login Form",
        Domains: new[] { "example.com", "www.example.com" },
        Mode: WidgetMode.Managed
    ));

// IMPORTANT: Store this secret securely!
Console.WriteLine($"Sitekey: {widget.Sitekey}");
Console.WriteLine($"Secret: {widget.Secret}");
```

### Widget Modes

| Mode | Description |
|------|-------------|
| `Managed` | Cloudflare decides when to show challenges |
| `NonInteractive` | Never shows visible challenges to users |
| `Invisible` | Completely invisible to users |

```csharp
// Invisible widget for seamless UX
var invisible = await cf.Turnstile.CreateWidgetAsync(accountId,
    new CreateTurnstileWidgetRequest(
        Name: "Invisible Protection",
        Domains: new[] { "example.com" },
        Mode: WidgetMode.Invisible
    ));
```

### With Bot Fight Mode

Enable additional bot protection:

```csharp
var widget = await cf.Turnstile.CreateWidgetAsync(accountId,
    new CreateTurnstileWidgetRequest(
        Name: "Contact Form",
        Domains: new[] { "example.com" },
        Mode: WidgetMode.Managed,
        BotFightMode: true
    ));
```

### With Custom Region

```csharp
var widget = await cf.Turnstile.CreateWidgetAsync(accountId,
    new CreateTurnstileWidgetRequest(
        Name: "EU Form",
        Domains: new[] { "eu.example.com" },
        Mode: WidgetMode.Managed,
        Region: TurnstileRegion.World
    ));
```

## Updating Widgets

```csharp
var updated = await cf.Turnstile.UpdateWidgetAsync(accountId, sitekey,
    new UpdateTurnstileWidgetRequest(
        Name: "Updated Widget Name",
        Domains: new[] { "example.com", "api.example.com" },
        Mode: WidgetMode.NonInteractive
    ));
```

### Add Domain

```csharp
var widget = await cf.Turnstile.GetWidgetAsync(accountId, sitekey);

var domains = widget.Domains.ToList();
domains.Add("new.example.com");

await cf.Turnstile.UpdateWidgetAsync(accountId, sitekey,
    new UpdateTurnstileWidgetRequest(
        Name: widget.Name,
        Domains: domains,
        Mode: widget.Mode
    ));
```

## Deleting Widgets

```csharp
await cf.Turnstile.DeleteWidgetAsync(accountId, sitekey);
Console.WriteLine($"Deleted widget: {sitekey}");
```

## Rotating Secrets

### With Grace Period (Default)

The old secret remains valid for 2 hours:

```csharp
var result = await cf.Turnstile.RotateSecretAsync(accountId, sitekey);

Console.WriteLine($"New secret: {result.Secret}");
Console.WriteLine("Old secret valid for 2 more hours");
```

### Immediate Invalidation

Revoke the old secret immediately:

```csharp
var result = await cf.Turnstile.RotateSecretAsync(
    accountId,
    sitekey,
    invalidateImmediately: true);

Console.WriteLine($"New secret: {result.Secret}");
Console.WriteLine("Old secret is now invalid");
```

> [!WARNING]
> Immediate invalidation may cause validation failures for in-flight requests.

## Models Reference

### TurnstileWidget

| Property | Type | Description |
|----------|------|-------------|
| `Sitekey` | `string` | Public key for client-side integration |
| `Secret` | `string?` | Private key (only on create/rotate) |
| `Name` | `string` | Widget name |
| `Domains` | `IReadOnlyList<string>` | Allowed domains |
| `Mode` | `WidgetMode` | Challenge mode |
| `BotFightMode` | `bool` | Extra bot protection |
| `Region` | `TurnstileRegion?` | Processing region |
| `CreatedOn` | `DateTime` | Creation timestamp |
| `ModifiedOn` | `DateTime` | Last modification |

### WidgetMode (Extensible Enum)

| Known Value | Description |
|-------------|-------------|
| `Managed` | Cloudflare decides when to challenge |
| `NonInteractive` | Never shows visible challenges |
| `Invisible` | Completely invisible |

### CreateTurnstileWidgetRequest

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Widget name |
| `Domains` | `IReadOnlyList<string>` | Allowed domains |
| `Mode` | `WidgetMode` | Challenge mode |
| `BotFightMode` | `bool?` | Enable bot fight mode |
| `Region` | `TurnstileRegion?` | Processing region |

### RotateWidgetSecretResult

| Property | Type | Description |
|----------|------|-------------|
| `Secret` | `string` | New secret key |

## Common Patterns

### Find Widget by Name

```csharp
public async Task<TurnstileWidget?> FindWidgetByNameAsync(
    string accountId,
    string name)
{
    await foreach (var widget in cf.Turnstile.ListAllWidgetsAsync(accountId))
    {
        if (widget.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            return widget;
        }
    }

    return null;
}
```

### Create Widget If Not Exists

```csharp
public async Task<TurnstileWidget> EnsureWidgetExistsAsync(
    string accountId,
    string name,
    IEnumerable<string> domains)
{
    var existing = await FindWidgetByNameAsync(accountId, name);

    if (existing is not null)
    {
        return existing;
    }

    return await cf.Turnstile.CreateWidgetAsync(accountId,
        new CreateTurnstileWidgetRequest(
            Name: name,
            Domains: domains.ToList(),
            Mode: WidgetMode.Managed
        ));
}
```

### Rotate All Secrets

```csharp
public async Task<Dictionary<string, string>> RotateAllSecretsAsync(
    string accountId)
{
    var newSecrets = new Dictionary<string, string>();

    await foreach (var widget in cf.Turnstile.ListAllWidgetsAsync(accountId))
    {
        var result = await cf.Turnstile.RotateSecretAsync(accountId, widget.Sitekey);
        newSecrets[widget.Name] = result.Secret;

        Console.WriteLine($"Rotated secret for: {widget.Name}");
    }

    return newSecrets;
}
```

## Client-Side Integration

After creating a widget, use the sitekey in your HTML:

```html
<script src="https://challenges.cloudflare.com/turnstile/v0/api.js" async defer></script>
<div class="cf-turnstile" data-sitekey="YOUR_SITEKEY"></div>
```

Then validate the token server-side using the secret.

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Turnstile | Account | Read (for listing/get) |
| Turnstile | Account | Write (for create/update/delete/rotate) |

## Related

- [Account Management](account-management.md) - Manage accounts
- [Zone Settings](../zones/zone-settings.md) - Zone security settings
