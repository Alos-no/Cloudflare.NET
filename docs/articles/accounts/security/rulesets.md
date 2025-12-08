# Account WAF Rulesets

WAF Rulesets at the account level allow you to create custom firewall rules, rate limiting rules, and managed rule configurations that apply across **all zones** in your account.

## Overview

Access the Account Rulesets API through `cf.Accounts.Rulesets`:

```csharp
public class AccountWafService(ICloudflareApiClient cf)
{
    public async Task CreateGlobalRuleAsync()
    {
        await cf.Accounts.Rulesets.CreateAsync(
            new CreateRulesetRequest(
                Name: "Global Security Rules",
                Kind: "root",
                Phase: SecurityConstants.RulesetPhases.HttpRequestFirewallCustom,
                Rules: [
                    new CreateRuleRequest(
                        Action: RuleAction.Block,
                        Expression: "(http.request.uri.query contains \"<script\")",
                        Description: "Block XSS attempts globally",
                        Enabled: true
                    )
                ]
            ));
    }
}
```

## Account vs Zone Rulesets

| Scope | Kind | Applied To | API Path |
|-------|------|------------|----------|
| Account | `root` | All zones in account | `cf.Accounts.Rulesets` |
| Zone | `zone` | Single zone only | `cf.Zones.Rulesets` |

Account-level rulesets are useful for:
- Organization-wide security policies
- Centralized rate limiting across all properties
- Managed WAF rule deployments
- Consistent security posture across zones

## Ruleset Phases

Use `SecurityConstants.RulesetPhases` for phase constants:

| Constant | Phase | Description |
|----------|-------|-------------|
| `HttpRequestFirewallManaged` | `http_request_firewall_managed` | Deploy managed WAF rules |
| `HttpRequestFirewallCustom` | `http_request_firewall_custom` | Custom WAF rules |
| `HttpRateLimit` | `http_ratelimit` | Rate limiting rules |
| `HttpRequestLateTransform` | `http_request_late_transform` | Header modification |
| `HttpRequestDynamicRedirect` | `http_request_dynamic_redirect` | URL redirects |

## Creating Rulesets

### Custom WAF Ruleset

```csharp
var ruleset = await cf.Accounts.Rulesets.CreateAsync(
    new CreateRulesetRequest(
        Name: "Global Security Rules",
        Kind: "root",
        Phase: SecurityConstants.RulesetPhases.HttpRequestFirewallCustom,
        Description: "Organization-wide security rules",
        Rules: [
            new CreateRuleRequest(
                Action: RuleAction.Block,
                Expression: "(http.request.uri.query contains \"<script\")",
                Description: "Block XSS attempts",
                Enabled: true
            ),
            new CreateRuleRequest(
                Action: RuleAction.Challenge,
                Expression: "(cf.threat_score gt 50)",
                Description: "Challenge high threat scores",
                Enabled: true
            )
        ]
    ));

Console.WriteLine($"Created ruleset: {ruleset.Id}");
```

### Global Rate Limiting

```csharp
var ruleset = await cf.Accounts.Rulesets.CreateAsync(
    new CreateRulesetRequest(
        Name: "Global Rate Limits",
        Kind: "root",
        Phase: SecurityConstants.RulesetPhases.HttpRateLimit,
        Rules: [
            new CreateRuleRequest(
                Action: RuleAction.Block,
                Expression: "(http.request.uri.path starts_with \"/api/\")",
                Description: "Global API rate limit",
                Enabled: true,
                Ratelimit: new RateLimitParameters
                {
                    Characteristics = [SecurityConstants.RateLimiting.Characteristics.IpSource],
                    Period = SecurityConstants.RateLimiting.Periods.P60_SECONDS,
                    RequestsPerPeriod = 1000,
                    MitigationTimeout = 60
                }
            )
        ]
    ));
```

## Phase Entrypoints

Phase entrypoints are the main rulesets that execute for each phase at the account level.

### Get Phase Entrypoint

```csharp
var entrypoint = await cf.Accounts.Rulesets.GetPhaseEntrypointAsync(
    SecurityConstants.RulesetPhases.HttpRequestFirewallCustom);

Console.WriteLine($"Entrypoint has {entrypoint.Rules?.Count ?? 0} rules");
```

### Update Phase Entrypoint

Replace all rules in a phase:

```csharp
var updated = await cf.Accounts.Rulesets.UpdatePhaseEntrypointAsync(
    SecurityConstants.RulesetPhases.HttpRequestFirewallCustom,
    [
        new CreateRuleRequest(
            Action: RuleAction.Block,
            Expression: "(ip.src in {192.168.1.0/24})",
            Description: "Block suspicious network globally",
            Enabled: true
        ),
        new CreateRuleRequest(
            Action: RuleAction.Log,
            Expression: "(http.request.uri.path contains \"/admin\")",
            Description: "Log admin access attempts",
            Enabled: true
        )
    ]);
```

### List Phase Entrypoint Versions

```csharp
var versions = await cf.Accounts.Rulesets.ListPhaseEntrypointVersionsAsync(
    SecurityConstants.RulesetPhases.HttpRequestFirewallCustom,
    new ListRulesetVersionsFilters { PerPage = 10 });

foreach (var version in versions.Items)
{
    Console.WriteLine($"Version {version.Version}: {version.Rules?.Count ?? 0} rules");
}
```

### Get Specific Version

```csharp
var historicalVersion = await cf.Accounts.Rulesets.GetPhaseEntrypointVersionAsync(
    SecurityConstants.RulesetPhases.HttpRequestFirewallCustom,
    "1");
```

## Managing Rules

### Add a Rule to Existing Ruleset

```csharp
var updated = await cf.Accounts.Rulesets.AddRuleAsync(rulesetId,
    new CreateRuleRequest(
        Action: RuleAction.Log,
        Expression: "(http.request.uri.path eq \"/health\")",
        Description: "Log health check requests",
        Enabled: true
    ));
```

### Update a Rule

```csharp
var updated = await cf.Accounts.Rulesets.UpdateRuleAsync(rulesetId, ruleId,
    new { enabled = false, description = "Temporarily disabled" });
```

### Delete a Rule

```csharp
var updated = await cf.Accounts.Rulesets.DeleteRuleAsync(rulesetId, ruleId);
```

## Listing Rulesets

### List with Pagination

```csharp
var page = await cf.Accounts.Rulesets.ListAsync(
    new ListRulesetsFilters { PerPage = 50 });

foreach (var ruleset in page.Items)
{
    Console.WriteLine($"{ruleset.Name} ({ruleset.Phase}): {ruleset.Rules?.Count ?? 0} rules");
}

// Use cursor for next page
if (page.CursorInfo?.Cursor != null)
{
    var nextPage = await cf.Accounts.Rulesets.ListAsync(
        new ListRulesetsFilters { Cursor = page.CursorInfo.Cursor });
}
```

### List All Rulesets

```csharp
await foreach (var ruleset in cf.Accounts.Rulesets.ListAllAsync())
{
    Console.WriteLine($"{ruleset.Id}: {ruleset.Name}");
    Console.WriteLine($"  Kind: {ruleset.Kind}");
    Console.WriteLine($"  Phase: {ruleset.Phase}");
    Console.WriteLine($"  Version: {ruleset.Version}");
}
```

### Get Ruleset Details

```csharp
var ruleset = await cf.Accounts.Rulesets.GetAsync(rulesetId);

Console.WriteLine($"Ruleset: {ruleset.Name}");
Console.WriteLine($"Description: {ruleset.Description}");
Console.WriteLine($"Last updated: {ruleset.LastUpdated}");

foreach (var rule in ruleset.Rules ?? [])
{
    Console.WriteLine($"  [{rule.Action}] {rule.Description}");
    Console.WriteLine($"    Expression: {rule.Expression}");
    Console.WriteLine($"    Enabled: {rule.Enabled}");
}
```

## Updating Rulesets

```csharp
var updated = await cf.Accounts.Rulesets.UpdateAsync(rulesetId,
    new UpdateRulesetRequest(
        Name: "Updated Global Rules",
        Description: "Updated description",
        Rules: [
            new CreateRuleRequest(
                Action: RuleAction.Block,
                Expression: "(cf.threat_score gt 80)",
                Description: "Block very high threat scores",
                Enabled: true
            )
        ]
    ));
```

## Deleting Rulesets

```csharp
await cf.Accounts.Rulesets.DeleteAsync(rulesetId);
```

## Models Reference

### Ruleset

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier |
| `Name` | `string` | Human-readable name |
| `Description` | `string?` | Optional description |
| `Kind` | `string` | Ruleset kind (`root` for account-level) |
| `Phase` | `string` | Execution phase |
| `Version` | `string` | Current version number |
| `Rules` | `IReadOnlyList<Rule>?` | List of rules |
| `LastUpdated` | `DateTimeOffset?` | Last modification time |

### Rule

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Unique identifier |
| `Action` | `RuleAction` | Action to take |
| `Expression` | `string` | Wirefilter expression |
| `Description` | `string?` | Rule description |
| `Enabled` | `bool` | Whether rule is active |
| `Ratelimit` | `RateLimitParameters?` | Rate limit config (for rate limit rules) |
| `ActionParameters` | `ActionParameters?` | Additional action config |

### RuleAction

| Action | Description |
|--------|-------------|
| `Block` | Block the request with error response |
| `Challenge` | Present CAPTCHA challenge |
| `ManagedChallenge` | Cloudflare-managed challenge |
| `JsChallenge` | JavaScript challenge |
| `Log` | Log without blocking |
| `Skip` | Skip subsequent rules or products |
| `Execute` | Execute another ruleset |
| `Rewrite` | Rewrite request/response |

### RateLimitParameters

| Property | Type | Description |
|----------|------|-------------|
| `Characteristics` | `string[]` | Properties to count by |
| `Period` | `int` | Time window in seconds |
| `RequestsPerPeriod` | `int` | Request threshold |
| `MitigationTimeout` | `int` | Block duration in seconds |
| `CountingExpression` | `string?` | Custom counting expression |
| `RequestsToOrigin` | `bool?` | Count only origin requests |

## Common Patterns

### Deploy Managed WAF Rules

```csharp
public async Task DeployManagedRulesAsync()
{
    await cf.Accounts.Rulesets.UpdatePhaseEntrypointAsync(
        SecurityConstants.RulesetPhases.HttpRequestFirewallManaged,
        [
            new CreateRuleRequest(
                Action: RuleAction.Execute,
                Expression: "true",
                Description: "Execute Cloudflare Managed Ruleset",
                Enabled: true,
                ActionParameters: new ActionParameters
                {
                    Id = "efb7b8c949ac4650a09736fc376e9aee" // OWASP Core Ruleset
                }
            )
        ]);
}
```

### Protect Login Endpoints Globally

```csharp
public async Task ProtectLoginEndpointsAsync()
{
    await cf.Accounts.Rulesets.CreateAsync(
        new CreateRulesetRequest(
            Name: "Global Login Protection",
            Kind: "root",
            Phase: SecurityConstants.RulesetPhases.HttpRateLimit,
            Rules: [
                new CreateRuleRequest(
                    Action: RuleAction.Block,
                    Expression: "(http.request.uri.path contains \"/login\" and http.request.method eq \"POST\")",
                    Description: "Rate limit login attempts across all zones",
                    Enabled: true,
                    Ratelimit: new RateLimitParameters
                    {
                        Characteristics = [
                            SecurityConstants.RateLimiting.Characteristics.IpSource,
                            "http.request.uri.path"
                        ],
                        Period = 60,
                        RequestsPerPeriod = 5,
                        MitigationTimeout = 600 // 10 minute block
                    }
                )
            ]
        ));
}
```

### Skip Rules for Trusted Sources

```csharp
new CreateRuleRequest(
    Action: RuleAction.Skip,
    Expression: "(ip.src in {10.0.0.0/8 172.16.0.0/12 192.168.0.0/16})",
    Description: "Skip security rules for internal IPs",
    Enabled: true,
    ActionParameters: new ActionParameters
    {
        Ruleset = "current" // Skip remaining rules in this ruleset
    }
)
```

### Audit All Rules

```csharp
public async Task AuditRulesetsAsync()
{
    await foreach (var ruleset in cf.Accounts.Rulesets.ListAllAsync())
    {
        Console.WriteLine($"\n=== {ruleset.Name} ({ruleset.Phase}) ===");

        var details = await cf.Accounts.Rulesets.GetAsync(ruleset.Id);

        foreach (var rule in details.Rules ?? [])
        {
            var status = rule.Enabled ? "ENABLED" : "DISABLED";
            Console.WriteLine($"  [{status}] {rule.Description}");
            Console.WriteLine($"    Action: {rule.Action}");
            Console.WriteLine($"    Expression: {rule.Expression}");
        }
    }
}
```

## Required Permissions

| Permission | Scope | Level |
|------------|-------|-------|
| Account WAF | Account | Read (for listing) |
| Account WAF | Account | Write (for create/update/delete) |

## Related

- [Zone WAF Rulesets](../../zones/security/rulesets.md) - Zone-level WAF rules
- [Account Access Rules](access-rules.md) - Account-level IP access rules
- [Zone Access Rules](../../zones/security/access-rules.md) - Zone-level access rules
