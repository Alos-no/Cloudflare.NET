# Cloudflare.NET SDK

[![.NET Build and Test](https://github.com/Alos-no/Cloudflare.NET/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Alos-no/Cloudflare.NET/actions/workflows/dotnet.yml)

This is an unofficial .NET SDK for the Cloudflare API. Its primary goal is to provide a strongly-typed, testable, and maintainable library for interacting with various Cloudflare services. The core SDK is lean and focuses on the REST API, while optional functionality, like the Analytics GraphQL API, is provided in a separate extension package.

## 1. Installation

The SDK is split into two packages. Install the one(s) you need from NuGet.

**Core REST API Client (Required):**
```powershell
Install-Package Cloudflare.NET
```

**Analytics GraphQL Client (Optional):**
```powershell
Install-Package Cloudflare.NET.Analytics
```

---

## 2. Quick Start

### 2.1 Configuration

First, configure your secrets in `appsettings.json` or through user secrets / environment variables.

```json
{
  "Cloudflare": {
    "ApiToken": "your-cloudflare-api-token",
    "AccountId": "your-cloudflare-account-id",
    "ZoneId": "your-zone-id-for-testing-or-ops"
  }
}
```

### 2.2 Dependency Injection

Register the client(s) in your `Program.cs` or `Startup.cs`.

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Register the core REST API client
builder.Services.AddCloudflareApiClient(builder.Configuration);

// 2. (Optional) Register the Analytics GraphQL client
builder.Services.AddCloudflareAnalytics();
```

### 2.3 Usage Example

Inject the client interfaces and use them in your services.

```csharp
public class MyCloudflareService(ICloudflareApiClient cf, IAnalyticsApi analytics)
{
    // Example: Using the REST API client to manage DNS
    public async Task<DnsRecord?> FindDnsRecordAsync(string zoneId, string hostname)
    {
        // Use the strongly-typed Zones API
        return await cf.Zones.FindDnsRecordByNameAsync(zoneId, hostname);
    }

    // Example: Using the GraphQL client to get R2 analytics
    public async Task<long> GetTotalR2ObjectCountAsync(string accountId)
    {
        var request = new GraphQLRequest
        {
            Query = @"
                query GetStorage($account: String!) {
                    viewer {
                        accounts(filter: { accountTag: $account }) {
                            r2StorageAdaptiveGroups(limit: 1) {
                                max { objectCount }
                            }
                        }
                    }
                }",
            Variables = new { account = accountId }
        };

        // Send the query and get back a strongly-typed response model
        var response = await analytics.SendQueryAsync<GraphQLResponse>(request);
        
        return response.Accounts.FirstOrDefault()?
            .Storage.FirstOrDefault()?
            .Max?.ObjectCount ?? 0;
    }
}
```
---

## 3. API Coverage & Roadmap

### 3.1 Implemented API Resources

| API Family | Project / Package | Status | Purpose |
| :--- | :--- | :--- | :--- |
| **Accounts** | `Cloudflare.NET` | ✅ **Implemented** | Endpoints under `/accounts/{account_id}` such as R2 bucket and custom domain management. |
| **Zones** | `Cloudflare.NET` | ✅ **Implemented** | Zone-scoped operations like managing DNS records. |
| **Analytics** | `Cloudflare.NET.Analytics` | ✅ **Implemented** | Provides a GraphQL client for the R2 Analytics API. |

### 3.2 Planned API Resources (Roadmap)

The following API surfaces are planned for implementation to support advanced use cases.

| API Family / Endpoint Group | Use Case | Notable Paths |
| :--- | :--- | :--- |
| **Custom Hostnames (SaaS)** | SaaS application vanity domain management. | `POST /zones/{zoneId}/custom_hostnames` |
| **SSL & Certificates (mTLS)** | Securing endpoints with Mutual TLS (mTLS). | `GET /zones/{zoneId}/client_certificates` |
| **Account Rulesets (WAF)** | Programmatically manage and audit WAF custom rules. | `GET /accounts/{accountId}/rulesets` |
| **Zone DNS (Bulk)** | Bulk DNS record management and migrations. | `POST /zones/{zoneId}/dns_records/import` |
| **Cache Purge** | Automated cache purging after content updates. | `POST /zones/{zoneId}/purge_cache` |
| **R2 Object Metadata** | Manage advanced, Cloudflare-specific object metadata. | `GET /accounts/{...}/r2/buckets/{...}/objects` |
| **User & Tokens** | Automated API token management and permission auditing. | `GET /user/permissions`, `GET /user/tokens` |

---

## 4. API Token Permissions

To adhere to the principle of least privilege, create a Cloudflare API token with only the scopes your application requires.

| Permission Group (UI Label) | Scope | Level | Typical Uses |
| :--- | :--- | :--- | :--- |
| **Workers R2 Storage** | Account | **Write** | Create/delete R2 buckets, manage domains. |
| **Workers R2 Storage** | Account | **Read** | List buckets and read configurations. |
| **Account Firewall Access Rules** | Account | **Write** | Programmatically manage IP Access Rules. |
| **Account Firewall Access Rules** | Account | **Read** | Audit and list existing firewall rules. |
| **Account Rulesets** | Account | **Write** | Deploy and manage WAF custom rulesets. |
| **Zone DNS** | Zone | **Edit** | Automate DNS changes for SaaS or application onboarding. |
| **Zone DNS** | Zone | **Read** | Scan and verify DNS state before migrations. |
| **SSL and Certificates** | Zone | **Write** | Automate client certificate lifecycle management. |
| **SSL and Certificates** | Zone | **Read** | Monitor and report on client certificate status. |
| **Cloudflare for SaaS** | Zone | **Write** | Manage tenant vanity domains (Custom Hostnames). |
| **Analytics** | Account | **Read** | Query R2 usage and other datasets via GraphQL. |
| **User API-tokens** | User | **Read** | Build applications that can inspect their own token permissions. |

> **CI/CD Token Tip**: For running this repository's integration tests, your CI token needs `Workers R2 Storage Write` and `Zone DNS Edit` to create and tear down test resources.
