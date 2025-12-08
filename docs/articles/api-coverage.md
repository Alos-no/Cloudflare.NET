# API Coverage

This document provides a comprehensive overview of the Cloudflare API endpoints supported by the SDK.

## Implemented API Resources

### Accounts API

| Endpoint Group | Status | Purpose |
|:---------------|:-------|:--------|
| R2 Buckets | ✅ Implemented | `POST /accounts/{id}/r2/buckets`, `DELETE .../{name}` |
| R2 Custom Domains | ✅ Implemented | `POST .../domains/custom`, `PUT .../domains/managed` |
| R2 Bucket CORS | ✅ Implemented | `GET/PUT/DELETE .../cors` |
| R2 Bucket Lifecycle | ✅ Implemented | `GET/PUT .../lifecycle` |
| IP Access Rules | ✅ Implemented | `GET, POST, PATCH, DELETE /accounts/{id}/firewall/access_rules/rules` |
| Rulesets (WAF) | ✅ Implemented | `GET, POST, PUT, DELETE /accounts/{id}/rulesets` |

### Zones API

| Endpoint Group | Status | Purpose |
|:---------------|:-------|:--------|
| DNS Records | ✅ Implemented | `GET, POST, DELETE /zones/{id}/dns_records` |
| DNS Records (Bulk) | ✅ Implemented | `POST .../import`, `GET .../export` |
| Cache Purge | ✅ Implemented | `POST /zones/{id}/purge_cache` |
| Zone Details | ✅ Implemented | `GET /zones/{id}` |
| IP Access Rules | ✅ Implemented | `GET, POST, PATCH, DELETE /zones/{id}/firewall/access_rules/rules` |
| Zone Lockdown | ✅ Implemented | `GET, POST, PUT, DELETE /zones/{id}/firewall/lockdowns` |
| User-Agent Rules | ✅ Implemented | `GET, POST, PUT, DELETE /zones/{id}/firewall/ua_rules` |
| Rulesets (WAF) | ✅ Implemented | `GET, POST, PUT, DELETE /zones/{id}/rulesets` |
| Custom Hostnames (SaaS) | ✅ Implemented | `GET, POST, PATCH, DELETE /zones/{id}/custom_hostnames` |

### Extension Packages

| Package | API | Status | Purpose |
|:--------|:----|:-------|:--------|
| Cloudflare.NET.Analytics | GraphQL API | ✅ Implemented | Generic GraphQL client for Analytics |
| Cloudflare.NET.R2 | S3-Compatible API | ✅ Implemented | High-level object storage operations |

## Planned API Resources

The following API surfaces are planned for future implementation:

| API Family | Use Case | Notable Paths |
|:-----------|:---------|:--------------|
| SSL & Certificates (mTLS) | Securing endpoints with Mutual TLS | `GET /zones/{zoneId}/client_certificates` |
| R2 Object Metadata | Cloudflare-specific object metadata | `GET /accounts/.../r2/buckets/.../objects` |
| User & Tokens | API token management and auditing | `GET /user/permissions`, `GET /user/tokens` |

## Related

- [API Token Permissions](permissions.md) - Required permissions for each feature
- [SDK Conventions](conventions.md) - Pagination patterns and common usage
