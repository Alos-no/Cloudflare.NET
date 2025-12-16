# Cloudflare.NET.SampleCoreConsole

This sample demonstrates how to use the **Cloudflare.NET** SDK to interact with various Cloudflare APIs. The sample is organized into multiple scenario runners, each focusing on a specific API family.

## Scenarios Included

### Zone/DNS Management (`ZoneSamples.cs`)
- Create a DNS CNAME record
- Find a DNS record by name
- Enumerate DNS records with automatic pagination
- Export DNS records in BIND format
- Purge zone cache

### Account/R2 Management (`AccountSamples.cs`)
- Create an R2 bucket
- List R2 buckets with automatic pagination
- Attach a custom domain to an R2 bucket
- Get custom domain status
- Detach custom domain

### Security/Zone Firewall (`SecuritySamples.cs`)
- Create/delete Zone IP Access Rules
- Create/delete Zone Lockdown Rules
- Create/delete User-Agent Block Rules

### Security/Account Firewall (`SecuritySamples.cs`)
- Create/delete Account IP Access Rules

### Custom Hostnames - Cloudflare for SaaS (`CustomHostnameSamples.cs`)
- Create a custom hostname with SSL/TLS configuration
- Retrieve custom hostname status and ownership verification details
- Update custom hostname TLS settings (min TLS version, Early Hints)
- List all custom hostnames with automatic pagination
- Get fallback origin configuration
- Delete custom hostname

## Configuration

Configure your credentials in `appsettings.json`, environment variables, or User Secrets:

| Setting | Description |
|---------|-------------|
| `Cloudflare:ApiToken` | Your Cloudflare API token |
| `Cloudflare:AccountId` | Your Cloudflare account ID |
| `Cloudflare:ZoneId` | The zone ID to operate on |

### Using User Secrets (Recommended)

```bash
cd samples/Cloudflare.NET.Sample.csproj
dotnet user-secrets set "Cloudflare:ApiToken" "your-token-here"
dotnet user-secrets set "Cloudflare:AccountId" "your-account-id"
dotnet user-secrets set "Cloudflare:ZoneId" "your-zone-id"
```

## Run

```bash
dotnet run
```

## Notes

- All resources created during the sample run are automatically cleaned up at the end.
- Each scenario logs its actions and results to the console.
- The sample uses the `Runner` utility to manage cleanup actions in reverse order.
- For Custom Hostnames, ownership verification is displayed but not automated - in a real SaaS scenario, you would share these with your customer.
