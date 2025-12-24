<style>
/* Consistent column widths for permissions tables */
.permissions-table th:nth-child(1),
.permissions-table td:nth-child(1) { width: 30%; }
.permissions-table th:nth-child(2),
.permissions-table td:nth-child(2) { width: 40%; }
.permissions-table th:nth-child(3),
.permissions-table td:nth-child(3) { width: 30%; }
</style>

# API Token Permissions

This guide covers the API token permissions required for each SDK feature.

## Overview

Cloudflare uses a permission-based system for API tokens. To adhere to the principle of least privilege, create API tokens with only the required scopes for your use case.

## Permission Reference

| Permission Group | Scope | Level | Typical Uses |
|:-----------------|:------|:------|:-------------|
| Workers R2 Storage | Account | Write | Create/delete R2 buckets, manage domains |
| Workers R2 Storage | Account | Read | List buckets and read configurations |
| Account Firewall Access Rules | Account | Write | Manage IP Access Rules at account level |
| Account Firewall Access Rules | Account | Read | Audit firewall rules at account level |
| Firewall Services | Zone | Write | Manage IP Access Rules at zone level |
| Account Rulesets | Account | Write | Deploy WAF custom rulesets at account level |
| Account Rulesets | Account | Read | List WAF rulesets at account level |
| Zone WAF | Zone | Write | Deploy WAF custom rulesets at zone level |
| Zone WAF | Zone | Read | List WAF rulesets at zone level |
| DNS | Zone | Write | Automate DNS changes, bulk import/export |
| DNS | Zone | Read | List and verify DNS state |
| Cache Purge | Zone | Purge | Purge cache via API |
| SSL and Certificates | Zone | Write | Manage client certificates, SaaS hostnames |
| SSL and Certificates | Zone | Read | Monitor certificate status |
| Account Analytics | Account | Read | Query R2 usage via GraphQL |

## R2 Credentials

> [!NOTE]
> The R2 S3-compatible client (<xref:Cloudflare.NET.R2.IR2Client>) uses separate R2 credentials (Access Key ID and Secret), not API tokens. These credentials are created in the Cloudflare dashboard under **R2 > Manage R2 API Tokens**.

## Permissions by Feature

### Zones API

<table class="permissions-table">
<thead><tr><th>Feature</th><th>Permission</th><th>Level</th></tr></thead>
<tbody>
<tr><td>DNS Records</td><td>DNS</td><td>Zone: Write</td></tr>
<tr><td>DNS Import/Export</td><td>DNS</td><td>Zone: Write</td></tr>
<tr><td>Cache Purge</td><td>Cache Purge</td><td>Zone: Purge</td></tr>
<tr><td>Zone Details</td><td>Zone</td><td>Zone: Read</td></tr>
<tr><td>Custom Hostnames</td><td>SSL and Certificates</td><td>Zone: Write</td></tr>
</tbody>
</table>

### Zone Security

<table class="permissions-table">
<thead><tr><th>Feature</th><th>Permission</th><th>Level</th></tr></thead>
<tbody>
<tr><td>IP Access Rules</td><td>Firewall Services</td><td>Zone: Write</td></tr>
<tr><td>Zone Lockdown</td><td>Firewall Services</td><td>Zone: Write</td></tr>
<tr><td>User-Agent Rules</td><td>Firewall Services</td><td>Zone: Write</td></tr>
<tr><td>WAF Rulesets</td><td>Zone WAF</td><td>Zone: Write</td></tr>
</tbody>
</table>

### Accounts API

<table class="permissions-table">
<thead><tr><th>Feature</th><th>Permission</th><th>Level</th></tr></thead>
<tbody>
<tr><td>R2 Buckets</td><td>Workers R2 Storage</td><td>Account: Write</td></tr>
<tr><td>R2 Custom Domains</td><td>Workers R2 Storage</td><td>Account: Write</td></tr>
<tr><td>R2 CORS Configuration</td><td>Workers R2 Storage</td><td>Account: Write</td></tr>
<tr><td>R2 Lifecycle Policies</td><td>Workers R2 Storage</td><td>Account: Write</td></tr>
</tbody>
</table>

### Workers KV

<table class="permissions-table">
<thead><tr><th>Feature</th><th>Permission</th><th>Level</th></tr></thead>
<tbody>
<tr><td>KV Namespaces (read)</td><td>Workers KV Storage</td><td>Account: Read</td></tr>
<tr><td>KV Namespaces (write)</td><td>Workers KV Storage</td><td>Account: Write</td></tr>
<tr><td>KV Keys (read)</td><td>Workers KV Storage</td><td>Account: Read</td></tr>
<tr><td>KV Values (read/write)</td><td>Workers KV Storage</td><td>Account: Write</td></tr>
<tr><td>KV Bulk Operations</td><td>Workers KV Storage</td><td>Account: Write</td></tr>
</tbody>
</table>

### Account Security

<table class="permissions-table">
<thead><tr><th>Feature</th><th>Permission</th><th>Level</th></tr></thead>
<tbody>
<tr><td>IP Access Rules</td><td>Account Firewall Access Rules</td><td>Account: Write</td></tr>
<tr><td>WAF Rulesets</td><td>Account Rulesets</td><td>Account: Write</td></tr>
</tbody>
</table>

### Analytics

<table class="permissions-table">
<thead><tr><th>Feature</th><th>Permission</th><th>Level</th></tr></thead>
<tbody>
<tr><td>GraphQL Queries</td><td>Account Analytics</td><td>Account: Read</td></tr>
</tbody>
</table>

## Creating an API Token

1. Log in to the [Cloudflare Dashboard](https://dash.cloudflare.com)
2. Go to **My Profile > API Tokens**
3. Click **Create Token**
4. Either use a template or create a custom token
5. Select only the permissions you need
6. Optionally restrict to specific zones or accounts
7. Copy the token (it won't be shown again)

## Best Practices

- **Least Privilege**: Only grant permissions that are actually needed
- **Scope Restriction**: Limit tokens to specific zones or accounts when possible
- **Token Rotation**: Rotate tokens periodically for security
- **Separate Tokens**: Use different tokens for different applications or environments
- **Never Commit**: Store tokens in environment variables or secret managers, never in source control

## Related

- [Configuration](configuration.md) - How to configure the SDK with your token
- [Getting Started](getting-started.md) - Quick start guide
