# API Reference

Welcome to the Cloudflare.NET SDK API reference documentation.

This documentation is automatically generated from the XML documentation comments in the source code.

## Packages

### Cloudflare.NET.Api

The core REST API client for Cloudflare services.

- **Entry Point**: @Cloudflare.NET.ICloudflareApiClient
- **Accounts API**: @Cloudflare.NET.Accounts.IAccountsApi
- **Zones API**: @Cloudflare.NET.Zones.IZonesApi
- **Security Models**: @Cloudflare.NET.Security.Rulesets.Models

### Cloudflare.NET.R2

High-level S3-compatible client for R2 object storage.

- **Entry Point**: @Cloudflare.NET.R2.IR2Client
- **Result Types**: @Cloudflare.NET.R2.Models.R2Result

### Cloudflare.NET.Analytics

GraphQL client for Cloudflare Analytics API.

- **Entry Point**: @Cloudflare.NET.Analytics.IAnalyticsApi

## Getting Started

For usage examples and tutorials, see the [Articles](../articles/getting-started.md) section.

## Namespace Reference

| Namespace | Description |
|-----------|-------------|
| `Cloudflare.NET` | Core client interfaces and implementations |
| `Cloudflare.NET.Core` | Base infrastructure, HTTP handling, resilience |
| `Cloudflare.NET.Core.Models` | API envelope types, pagination models |
| `Cloudflare.NET.Core.Exceptions` | Exception types |
| `Cloudflare.NET.Accounts` | Account-level API resources |
| `Cloudflare.NET.Zones` | Zone-level API resources |
| `Cloudflare.NET.Security` | Security and firewall models |
| `Cloudflare.NET.R2` | R2 object storage client |
| `Cloudflare.NET.Analytics` | GraphQL analytics client |
