# Zone WAF Rulesets

WAF Rulesets allow you to create custom firewall rules and rate limiting rules at the zone level using Cloudflare's Ruleset Engine.

## Overview

Access the Zone Rulesets API through `cf.Zones.Rulesets`:

```csharp
public class WafService(ICloudflareApiClient cf)
{
    public async Task CreateBlockingRuleAsync(string zoneId)
    {
        await cf.Zones.Rulesets.CreateAsync(zoneId,
            new CreateRulesetRequest(
                Name: "Custom Security Rules",
                Kind: "zone",
                Phase: SecurityConstants.RulesetPhases.HttpRequestFirewallCustom,
                Rules: [
                    new CreateRuleRequest(
                        Action: RulesetAction.Block,
                        Expression: "(http.request.uri.path contains \"/admin\")",
                        Description: "Block admin access",
                        Enabled: true
                    )
                ]
            ));
    }
}
```

> [!TIP]
> Use <xref:Cloudflare.NET.Security.SecurityConstants> for type-safe phase and rate limiting constants.

## Ruleset Phases

Use <xref:Cloudflare.NET.Security.SecurityConstants.RulesetPhases> for phase constants:

| Constant | Phase | Description |
|----------|-------|-------------|
| `HttpRequestFirewallManaged` | `http_request_firewall_managed` | Managed WAF rules |
| `HttpRequestFirewallCustom` | `http_request_firewall_custom` | Custom WAF rules |
| `HttpRateLimit` | `http_ratelimit` | Rate limiting rules |
| `HttpRequestLateTransform` | `http_request_late_transform` | Header modification |
| `HttpRequestOrigin` | `http_request_origin` | HTTP origin rules |
| `HttpRequestSanitize` | `http_request_sanitize` | HTTP request sanitize |
| `HttpRequestDynamicRedirect` | `http_request_dynamic_redirect` | URL redirects |
| `HttpResponseHeadersTransform` | `http_response_headers_transform` | Response header modification |
| `HttpLogCustomFields` | `http_log_custom_fields` | Log custom fields |

```csharp
using Cloudflare.NET.Security;

// Use constants for type-safe phase references
var phase = SecurityConstants.RulesetPhases.HttpRequestFirewallCustom;
var rateLimitPhase = SecurityConstants.RulesetPhases.HttpRateLimit;
```

## Creating Rulesets

### Custom WAF Ruleset

```csharp
var ruleset = await cf.Zones.Rulesets.CreateAsync(zoneId,
    new CreateRulesetRequest(
        Name: "Security Rules",
        Kind: "zone",
        Phase: SecurityConstants.RulesetPhases.HttpRequestFirewallCustom,
        Description: "Custom security rules for the zone",
        Rules: [
            new CreateRuleRequest(
                Action: RulesetAction.Block,
                Expression: "(http.request.uri.query contains \"<script\")",
                Description: "Block XSS attempts",
                Enabled: true
            ),
            new CreateRuleRequest(
                Action: RulesetAction.Challenge,
                Expression: "(cf.threat_score gt 50)",
                Description: "Challenge high threat scores",
                Enabled: true
            )
        ]
    ));
```

### Rate Limiting Ruleset

```csharp
var ruleset = await cf.Zones.Rulesets.CreateAsync(zoneId,
    new CreateRulesetRequest(
        Name: "API Rate Limits",
        Kind: "zone",
        Phase: SecurityConstants.RulesetPhases.HttpRateLimit,
        Rules: [
            new CreateRuleRequest(
                Action: RulesetAction.Block,
                Expression: "(http.request.uri.path starts_with \"/api/\")",
                Description: "Rate limit API endpoints",
                Enabled: true,
                Ratelimit: new RateLimitParameters
                {
                    Characteristics = [SecurityConstants.RateLimiting.Characteristics.IpSource],
                    Period = SecurityConstants.RateLimiting.Periods.P60_SECONDS,
                    RequestsPerPeriod = 100,
                    MitigationTimeout = 60
                }
            )
        ]
    ));
```

## Phase Entrypoints

Phase entrypoints are the main rulesets that execute for each phase.

### Get Phase Entrypoint

```csharp
var entrypoint = await cf.Zones.Rulesets.GetPhaseEntrypointAsync(
    zoneId,
    SecurityConstants.RulesetPhases.HttpRequestFirewallCustom);
```

### Update Phase Entrypoint

Replace all rules in a phase:

```csharp
var updated = await cf.Zones.Rulesets.UpdatePhaseEntrypointAsync(
    zoneId,
    SecurityConstants.RulesetPhases.HttpRequestFirewallCustom,
    [
        new CreateRuleRequest(
            Action: RulesetAction.Block,
            Expression: "(ip.src eq 192.168.1.1)",
            Description: "Block specific IP",
            Enabled: true
        )
    ]);
```

## Managing Rules

### Add a Rule to Existing Ruleset

```csharp
var updated = await cf.Zones.Rulesets.AddRuleAsync(zoneId, rulesetId,
    new CreateRuleRequest(
        Action: RulesetAction.Log,
        Expression: "(http.request.uri.path eq \"/debug\")",
        Description: "Log debug requests",
        Enabled: true
    ));
```

### Update a Rule

```csharp
var updated = await cf.Zones.Rulesets.UpdateRuleAsync(zoneId, rulesetId, ruleId,
    new { enabled = false });
```

### Delete a Rule

```csharp
var updated = await cf.Zones.Rulesets.DeleteRuleAsync(zoneId, rulesetId, ruleId);
```

## Listing Rulesets

### List All Rulesets

```csharp
await foreach (var ruleset in cf.Zones.Rulesets.ListAllAsync(zoneId))
{
    Console.WriteLine($"{ruleset.Name} ({ruleset.Phase}): {ruleset.Rules?.Count ?? 0} rules");
}
```

### Get Ruleset Details

```csharp
var ruleset = await cf.Zones.Rulesets.GetAsync(zoneId, rulesetId);

foreach (var rule in ruleset.Rules ?? [])
{
    Console.WriteLine($"  {rule.Description}: {rule.Action}");
}
```

## Rule Actions

<xref:Cloudflare.NET.Security.Rulesets.Models.RulesetAction> is an extensible enum that defines what happens when a rule matches.

### Terminating Actions

These actions stop rule evaluation for the request:

| Action | API Value | Description |
|--------|-----------|-------------|
| `Block` | `block` | Returns 403 Forbidden (429 for rate limits) |
| `Challenge` | `challenge` | Presents interactive CAPTCHA challenge |
| `JsChallenge` | `js_challenge` | JavaScript challenge (blocks most bots) |
| `ManagedChallenge` | `managed_challenge` | Cloudflare dynamically chooses challenge type |
| `Redirect` | `redirect` | Redirects to another URL |
| `ServeError` | `serve_error` | Serves custom error content |

### Non-Terminating Actions

These actions allow continued rule evaluation:

| Action | API Value | Description |
|--------|-----------|-------------|
| `Log` | `log` | Log without blocking (Enterprise only) |
| `Skip` | `skip` | Skip subsequent rules/products |
| `Execute` | `execute` | Execute another ruleset |
| `Rewrite` | `rewrite` | Modify URI/headers (Transform Rules) |
| `Route` | `route` | Adjust routing to origin (Origin Rules) |
| `SetConfig` | `set_config` | Change Cloudflare settings |
| `CompressResponse` | `compress_response` | Configure compression |
| `SetCacheSettings` | `set_cache_settings` | Configure caching |
| `LogCustomField` | `log_custom_field` | Add custom Logpush fields |

```csharp
using Cloudflare.NET.Security.Rulesets.Models;

// Use the static properties for IntelliSense support
var action = RulesetAction.Block;
var challenge = RulesetAction.ManagedChallenge;

// Extensible - custom values work too
RulesetAction customAction = "future-action";
```

## Managed WAF Override Actions

When overriding rules in managed WAF rulesets, use <xref:Cloudflare.NET.Security.Rulesets.Models.ManagedWafOverrideAction>:

| Action | Description |
|--------|-------------|
| `ManagedChallenge` | Dynamically chooses challenge type |
| `Challenge` | Interactive challenge |
| `JsChallenge` | JavaScript challenge |
| `Block` | Block the request |
| `Log` | Log only (Enterprise) |
| `Default` | Remove override, use default behavior |

## Skip Products

Use <xref:Cloudflare.NET.Security.SecurityConstants.SkipProducts> to skip specific security products:

| Constant | Value | Description |
|----------|-------|-------------|
| `ZoneLockdown` | `zoneLockdown` | Zone Lockdown |
| `UaBlock` | `uaBlock` | User-Agent blocking |
| `Bic` | `bic` | Browser Integrity Check |
| `Hot` | `hot` | Hotlink Protection |
| `SecurityLevel` | `securityLevel` | Security Level |
| `RateLimit` | `rateLimit` | Rate Limiting (legacy) |
| `Waf` | `waf` | Web Application Firewall |

```csharp
new CreateRuleRequest(
    Action: RulesetAction.Skip,
    Expression: "(http.request.uri.path starts_with \"/internal\")",
    Description: "Skip WAF for internal paths",
    Enabled: true,
    ActionParameters: new
    {
        products = new[] { SecurityConstants.SkipProducts.Waf }
    }
)
```

## Rate Limiting

### RateLimitParameters

| Property | Type | Description |
|----------|------|-------------|
| `Characteristics` | `string[]` | Properties to count (e.g., `ip.src`) |
| `Period` | `int` | Time window in seconds |
| `RequestsPerPeriod` | `int` | Request threshold |
| `MitigationTimeout` | `int` | Block duration in seconds |
| `CountingExpression` | `string?` | Custom counting expression |

### Rate Limit Periods

Use <xref:Cloudflare.NET.Security.SecurityConstants.RateLimiting.Periods> for valid periods:

| Constant | Value | Description |
|----------|-------|-------------|
| `P10_SECONDS` | 10 | 10 second window |
| `P60_SECONDS` | 60 | 1 minute window |
| `P120_SECONDS` | 120 | 2 minute window |
| `P300_SECONDS` | 300 | 5 minute window |
| `P600_SECONDS` | 600 | 10 minute window |

### Rate Limit Characteristics

Use <xref:Cloudflare.NET.Security.SecurityConstants.RateLimiting.Characteristics> for counting properties:

| Constant | Value | Description |
|----------|-------|-------------|
| `IpSource` | `ip.src` | Client IP address |
| `UriPath` | `http.request.uri.path` | Request path |
| `HttpMethod` | `http.request.method` | HTTP method |
| `CfConnectingIp` | `http.request.headers["cf-connecting-ip"]` | CF-Connecting-IP header |
| `XForwardedFor` | `http.request.headers["x-forwarded-for"]` | X-Forwarded-For header |
| `ColoId` | `cf.colo.id` | Cloudflare colo ID |
| `BotManagementScore` | `cf.bot_management.score` | Bot management score |
| `ThreatScore` | `cf.threat_score` | Threat score |

### Rate Limit Response Content Types

Use <xref:Cloudflare.NET.Security.SecurityConstants.RateLimiting.ContentTypes> for custom response content types:

| Constant | Value |
|----------|-------|
| `TextPlain` | `text/plain` |
| `TextXml` | `text/xml` |
| `ApplicationJson` | `application/json` |
| `TextHtml` | `text/html` |

## Common Patterns

### Protect Login Endpoint

```csharp
var ruleset = await cf.Zones.Rulesets.CreateAsync(zoneId,
    new CreateRulesetRequest(
        Name: "Login Protection",
        Kind: "zone",
        Phase: SecurityConstants.RulesetPhases.HttpRateLimit,
        Rules: [
            new CreateRuleRequest(
                Action: RulesetAction.Block,
                Expression: "(http.request.uri.path eq \"/login\" and http.request.method eq \"POST\")",
                Description: "Rate limit login attempts",
                Enabled: true,
                Ratelimit: new RateLimitParameters
                {
                    Characteristics = [SecurityConstants.RateLimiting.Characteristics.IpSource],
                    Period = SecurityConstants.RateLimiting.Periods.P60_SECONDS,
                    RequestsPerPeriod = 5,
                    MitigationTimeout = 300
                }
            )
        ]
    ));
```

### Block Bad Bots

```csharp
new CreateRuleRequest(
    Action: RulesetAction.Block,
    Expression: "(cf.bot_management.score lt 30 and not cf.bot_management.verified_bot)",
    Description: "Block likely bots",
    Enabled: true
)
```

### API Rate Limiting with Custom Response

```csharp
new CreateRuleRequest(
    Action: RulesetAction.Block,
    Expression: "(http.request.uri.path starts_with \"/api/\")",
    Description: "API rate limit",
    Enabled: true,
    Ratelimit: new RateLimitParameters
    {
        Characteristics = [
            SecurityConstants.RateLimiting.Characteristics.IpSource,
            SecurityConstants.RateLimiting.Characteristics.UriPath
        ],
        Period = SecurityConstants.RateLimiting.Periods.P60_SECONDS,
        RequestsPerPeriod = 100,
        MitigationTimeout = 60
    }
)
```

### Geographic Restrictions

```csharp
new CreateRuleRequest(
    Action: RulesetAction.Block,
    Expression: "(ip.geoip.country in {\"CN\" \"RU\" \"KP\"})",
    Description: "Block specific countries",
    Enabled: true
)
```

## Models Reference

### Ruleset

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Ruleset identifier |
| `Name` | `string` | Ruleset name |
| `Description` | `string?` | Ruleset description |
| `Kind` | `string` | Ruleset kind (`zone`, `custom`, `managed`) |
| `Phase` | `string` | Execution phase |
| `Version` | `string` | Ruleset version |
| `Rules` | `IReadOnlyList<Rule>?` | List of rules |

### Rule

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Rule identifier |
| `Action` | `RulesetAction` | Action to take |
| `Expression` | `string` | Filter expression |
| `Description` | `string?` | Rule description |
| `Enabled` | `bool` | Whether rule is enabled |
| `Ratelimit` | `RateLimitParameters?` | Rate limit config |
| `ActionParameters` | `object?` | Action-specific parameters |

### CreateRulesetRequest

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Ruleset name |
| `Kind` | `string` | Ruleset kind |
| `Phase` | `string` | Execution phase |
| `Description` | `string?` | Description |
| `Rules` | `CreateRuleRequest[]?` | Rules to create |

### CreateRuleRequest

| Property | Type | Description |
|----------|------|-------------|
| `Action` | `RulesetAction` | Action to take |
| `Expression` | `string` | Filter expression |
| `Description` | `string?` | Rule description |
| `Enabled` | `bool` | Whether rule is enabled |
| `Ratelimit` | `RateLimitParameters?` | Rate limit config |
| `ActionParameters` | `object?` | Action-specific parameters |

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Zone WAF | Zone | Read (for listing) |
| Zone WAF | Zone | Write (for create/update/delete) |

## Related

- [Account Rulesets](../../accounts/security/rulesets.md) - Account-level rulesets
- [Access Rules](access-rules.md) - IP/country blocking
- [Lockdown](lockdown.md) - URL-based access control
