# Custom Hostnames (Cloudflare for SaaS)

Custom Hostnames enable SaaS providers to extend Cloudflare's security and performance benefits to their customers' vanity domains. This is the foundation of [Cloudflare for SaaS](https://developers.cloudflare.com/cloudflare-for-platforms/cloudflare-for-saas/).

## Overview

Access the Custom Hostnames API through `cf.Zones.CustomHostnames`:

```csharp
public class SaasService(ICloudflareApiClient cf)
{
    public async Task OnboardCustomerAsync(string zoneId, string customerDomain)
    {
        var hostname = await cf.Zones.CustomHostnames.CreateAsync(zoneId,
            new CreateCustomHostnameRequest(
                Hostname: customerDomain,
                Ssl: new SslConfiguration(Method: DcvMethod.Txt)
            ));

        Console.WriteLine($"Status: {hostname.Status}");
    }
}
```

## Creating Custom Hostnames

### Basic Creation

```csharp
var hostname = await cf.Zones.CustomHostnames.CreateAsync(zoneId,
    new CreateCustomHostnameRequest(
        Hostname: "app.customer.com",
        Ssl: new SslConfiguration(Method: DcvMethod.Txt)
    ));
```

### With Custom Origin Server

Route traffic to a specific origin for this hostname:

```csharp
var hostname = await cf.Zones.CustomHostnames.CreateAsync(zoneId,
    new CreateCustomHostnameRequest(
        Hostname: "app.customer.com",
        Ssl: new SslConfiguration(Method: DcvMethod.Http),
        CustomOriginServer: "customer-123.origin.example.com"
    ));
```

### With Custom Metadata

Attach arbitrary metadata to the hostname:

```csharp
var metadata = JsonSerializer.SerializeToElement(new
{
    customerId = "cust_123",
    plan = "enterprise",
    region = "us-east"
});

var hostname = await cf.Zones.CustomHostnames.CreateAsync(zoneId,
    new CreateCustomHostnameRequest(
        Hostname: "app.customer.com",
        Ssl: new SslConfiguration(Method: DcvMethod.Txt),
        CustomMetadata: metadata
    ));
```

## Domain Validation

Custom hostnames require domain ownership verification before SSL certificates are issued.

### TXT Validation

The customer adds a TXT record to their DNS:

```csharp
var hostname = await cf.Zones.CustomHostnames.GetAsync(zoneId, hostnameId);

if (hostname.OwnershipVerification is { } verification)
{
    Console.WriteLine($"Add TXT record:");
    Console.WriteLine($"  Name: {verification.Name}");
    Console.WriteLine($"  Value: {verification.Value}");
}
```

### HTTP Validation

The customer serves a token at a specific URL:

```csharp
if (hostname.OwnershipVerificationHttp is { } httpVerification)
{
    Console.WriteLine($"Serve content at: {httpVerification.HttpUrl}");
    Console.WriteLine($"Content: {httpVerification.HttpBody}");
}
```

### Checking Validation Errors

```csharp
if (hostname.VerificationErrors?.Count > 0)
{
    foreach (var error in hostname.VerificationErrors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

## Listing Custom Hostnames

### List with Pagination

```csharp
var page = await cf.Zones.CustomHostnames.ListAsync(zoneId,
    new ListCustomHostnamesFilters
    {
        Page = 1,
        PerPage = 50
    });

foreach (var hostname in page.Items)
{
    Console.WriteLine($"{hostname.Hostname}: {hostname.Status}");
}
```

### List All Hostnames

```csharp
await foreach (var hostname in cf.Zones.CustomHostnames.ListAllAsync(zoneId))
{
    Console.WriteLine($"{hostname.Hostname}: {hostname.Status}, SSL: {hostname.Ssl.Status}");
}
```

### Filter by Hostname

```csharp
var filters = new ListCustomHostnamesFilters
{
    Hostname = "app.customer.com"
};

await foreach (var hostname in cf.Zones.CustomHostnames.ListAllAsync(zoneId, filters))
{
    // Process matching hostnames
}
```

## Updating Custom Hostnames

Update hostname configuration (PATCH operation):

```csharp
var updated = await cf.Zones.CustomHostnames.UpdateAsync(zoneId, hostnameId,
    new UpdateCustomHostnameRequest(
        CustomOriginServer: "new-origin.example.com"
    ));
```

### Refresh Validation Status

Send an empty update to refresh the validation status:

```csharp
var refreshed = await cf.Zones.CustomHostnames.UpdateAsync(zoneId, hostnameId,
    new UpdateCustomHostnameRequest());
```

## Deleting Custom Hostnames

```csharp
await cf.Zones.CustomHostnames.DeleteAsync(zoneId, hostnameId);
```

## Fallback Origin

The fallback origin is used when a custom hostname doesn't have a specific `CustomOriginServer` configured.

### Get Fallback Origin

```csharp
var fallback = await cf.Zones.CustomHostnames.GetFallbackOriginAsync(zoneId);
Console.WriteLine($"Fallback origin: {fallback.Origin}");
```

### Set Fallback Origin

```csharp
var fallback = await cf.Zones.CustomHostnames.UpdateFallbackOriginAsync(zoneId,
    new UpdateFallbackOriginRequest(Origin: "fallback.origin.example.com"));
```

### Delete Fallback Origin

```csharp
await cf.Zones.CustomHostnames.DeleteFallbackOriginAsync(zoneId);
```

## Models Reference

### CustomHostname

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier |
| `Hostname` | `string` | The custom hostname (e.g., `app.customer.com`) |
| `Status` | `CustomHostnameStatus` | Current status (see enum below) |
| `Ssl` | `SslResponse` | SSL certificate status and configuration |
| `OwnershipVerification` | `OwnershipVerification?` | TXT validation details |
| `OwnershipVerificationHttp` | `OwnershipVerificationHttp?` | HTTP validation details |
| `VerificationErrors` | `IReadOnlyList<string>?` | Validation error messages |
| `CustomMetadata` | `JsonElement?` | Arbitrary JSON metadata |
| `CustomOriginServer` | `string?` | Custom origin hostname |
| `CustomOriginSni` | `string?` | SNI for origin TLS handshake |
| `CreatedAt` | `DateTimeOffset?` | Creation timestamp |

### CustomHostnameStatus

| Value | Description |
|-------|-------------|
| `Pending` | Pending initial validation |
| `Active` | Active and serving traffic |
| `ActiveRedeploying` | Active, changes being redeployed |
| `Moved` | Moved to another zone |
| `PendingDeletion` | Pending deletion |
| `Deleted` | Deleted |
| `Blocked` | Blocked due to policy violation |

### SslStatus

| Value | Description |
|-------|-------------|
| `Initializing` | Certificate being initialized |
| `PendingValidation` | Pending domain control validation |
| `PendingIssuance` | Pending CA issuance |
| `PendingDeployment` | Pending edge deployment |
| `Active` | Active and serving traffic |
| `Expired` | Certificate expired |

### DcvMethod (Domain Control Validation)

| Value | Description |
|-------|-------------|
| `Http` | Serve token at specific URL |
| `Txt` | Add TXT record to DNS |
| `Email` | Email approval to domain contacts |

### CertificateAuthority

| Value | Description |
|-------|-------------|
| `Digicert` | DigiCert CA |
| `Google` | Google Trust Services |
| `LetsEncrypt` | Let's Encrypt |
| `SslCom` | SSL.com |

## Common Patterns

### Complete Onboarding Flow

```csharp
public class CustomerOnboardingService(ICloudflareApiClient cf)
{
    public async Task<OnboardingResult> OnboardAsync(
        string zoneId,
        string customerDomain,
        string customerId)
    {
        // Create hostname with metadata
        var metadata = JsonSerializer.SerializeToElement(new { customerId });

        var hostname = await cf.Zones.CustomHostnames.CreateAsync(zoneId,
            new CreateCustomHostnameRequest(
                Hostname: customerDomain,
                Ssl: new SslConfiguration(Method: DcvMethod.Txt),
                CustomMetadata: metadata
            ));

        return new OnboardingResult
        {
            HostnameId = hostname.Id,
            Status = hostname.Status,
            TxtName = hostname.OwnershipVerification?.Name,
            TxtValue = hostname.OwnershipVerification?.Value
        };
    }

    public async Task<bool> CheckValidationAsync(string zoneId, string hostnameId)
    {
        var hostname = await cf.Zones.CustomHostnames.GetAsync(zoneId, hostnameId);
        return hostname.Status == CustomHostnameStatus.Active;
    }
}
```

### Bulk Status Check

```csharp
public async Task<Dictionary<string, CustomHostnameStatus>> GetStatusesAsync(string zoneId)
{
    var statuses = new Dictionary<string, CustomHostnameStatus>();

    await foreach (var hostname in cf.Zones.CustomHostnames.ListAllAsync(zoneId))
    {
        statuses[hostname.Hostname] = hostname.Status;
    }

    return statuses;
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| SSL and Certificates | Zone | Read (for listing/get) |
| SSL and Certificates | Zone | Write (for create/update/delete) |
