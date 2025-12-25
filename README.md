# Cloudflare.NET - .NET SDK for Cloudflare API

[![.NET Build and Test](https://github.com/Alos-no/Cloudflare.NET/actions/workflows/CI.yml/badge.svg)](https://github.com/Alos-no/Cloudflare.NET/actions/workflows/CI.yml)
[![Documentation](https://img.shields.io/badge/docs-alos.no%2Fcfnet-27ae60)](https://alos.no/cfnet)
[![License: Apache-2.0](https://img.shields.io/badge/license-Apache--2.0-27ae60)](https://github.com/Alos-no/Cloudflare.NET/blob/main/LICENSE.txt)

[![NuGet (Cloudflare.NET.Api)](https://img.shields.io/nuget/v/Cloudflare.NET.Api?label=Cloudflare.NET.Api&color=27ae60)](https://www.nuget.org/packages/Cloudflare.NET.Api/)
[![NuGet (Cloudflare.NET.R2)](https://img.shields.io/nuget/v/Cloudflare.NET.R2?label=Cloudflare.NET.R2&color=27ae60)](https://www.nuget.org/packages/Cloudflare.NET.R2/)
[![NuGet (Cloudflare.NET.Analytics)](https://img.shields.io/nuget/v/Cloudflare.NET.Analytics?label=Cloudflare.NET.Analytics&color=27ae60)](https://www.nuget.org/packages/Cloudflare.NET.Analytics/)

**Cloudflare.NET** is a comprehensive **C# client library** for the **Cloudflare REST API**. It provides strongly-typed access to Cloudflare services including DNS management, Zone configuration, R2 object storage, Workers, WAF rules, and more. Built with testability and maintainability in mind.

> Cloudflare does not provide an official **.NET SDK** or **C# library**. This project aims to fill that gap with a community-maintained alternative.

**[Documentation](https://alos.no/cfnet)** | **[Getting Started](https://alos.no/cfnet/articles/getting-started.html)** | **[API Reference](https://alos.no/cfnet/api/)**

## Packages

| Package | Description |
|---------|-------------|
| **Cloudflare.NET.Api** | Core REST API client for Zones, DNS, Security, and R2 bucket management |
| **Cloudflare.NET.R2** | High-level S3-compatible client for R2 object storage |
| **Cloudflare.NET.Analytics** | GraphQL client for Cloudflare Analytics API |

```bash
dotnet add package Cloudflare.NET.Api
dotnet add package Cloudflare.NET.R2        # Optional
dotnet add package Cloudflare.NET.Analytics # Optional
```

## Example

```csharp
// Register in Program.cs
builder.Services.AddCloudflareApiClient(builder.Configuration);
builder.Services.AddCloudflareR2Client(builder.Configuration);

// Inject and use
public class MyService(ICloudflareApiClient cf)
{
    public async Task<DnsRecord?> FindRecordAsync(string zoneId, string hostname)
        => await cf.Zones.FindDnsRecordByNameAsync(zoneId, hostname);
}
```

## Features

- **Strongly-typed API** · Full IntelliSense with comprehensive XML documentation and proper nullability annotations

- **CI/CD Pipeline** · Every commit triggers automated builds and tests; releases published automatically to NuGet

- **Resilience Built-in** · Automatic retries, circuit breaker, proactive throttling based on rate limit headers, and configurable timeouts via Polly

- **Multi-account Support** · Named clients and keyed services for managing multiple Cloudflare accounts

- **Dependency Injection** · First-class support for `Microsoft.Extensions.DependencyInjection` with `IHttpClientFactory`

- **S3-Compatible R2** · Intelligent multipart uploads, presigned URLs, and automatic retry handling

- **Testable by Design** · Integration tests against real Cloudflare APIs and unit tests for request/response validation

## Get Started

<div align="center">

### 📚 Ready to dive in?

**[Explore the Full Documentation →](https://alos.no/cfnet)**

*Comprehensive guides, configuration options, multi-account setup, and more.*

</div>

## API Coverage

| API Family | Features |
|------------|----------|
| **Zones** | CRUD, Holds, Settings, Subscriptions, Cache Purge, Custom Hostnames (SaaS) |
| **DNS** | Record CRUD, Batch Operations, Import/Export, Record Scanning |
| **Zone Security** | IP Access Rules, Zone Lockdown, User-Agent Rules, WAF Rulesets |
| **Accounts** | Management, Members, Roles, Audit Logs, API Tokens, Subscriptions |
| **Account Storage** | R2 Buckets, R2 Custom Domains, R2 CORS, R2 Lifecycle Policies |
| **Account Security** | IP Access Rules, WAF Rulesets |
| **Users** | Profile Management, Memberships, Invitations, Audit Logs, API Tokens, Subscriptions |
| **Workers** | Routes (zone-scoped) |
| **Workers KV** | Namespace CRUD, Key-Value CRUD, Metadata, Expiration, Bulk Operations |
| **D1 Database** | Database CRUD, SQL Queries, Raw Queries, Export/Import |
| **Turnstile** | Widget CRUD, Secret Rotation |
| **R2 Client** | Upload, Download, Multipart, Presigned URLs, Batch Delete |
| **Analytics** | GraphQL queries for traffic, security, and R2 metrics |

See [API Coverage](https://alos.no/cfnet/articles/api-coverage.html) for full details and roadmap.

## Supported Frameworks

| Package | .NET 8 | .NET 9 | .NET 10 | Strong Named |
|---------|:------:|:------:|:-------:|:------------:|
| **Cloudflare.NET.Api** | ✅ | ✅ | ✅ | ✅ |
| **Cloudflare.NET.R2** | ✅ | ✅ | ✅ | ✅ |
| **Cloudflare.NET.Analytics** | ✅ | ✅ | ✅ | ❌* |

> \* `Cloudflare.NET.Analytics` cannot be strong-named because its dependency (`GraphQL.Client`) is not strong-named.

## Contributing

We welcome contributions! Whether it's bug reports, feature requests, or code contributions.

- [Report an Issue](https://github.com/Alos-no/Cloudflare.NET/issues)
- [View Documentation](https://alos.no/cfnet)

## License

This project is licensed under the [Apache 2.0 License](LICENSE.txt).
