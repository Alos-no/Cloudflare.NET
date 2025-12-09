---
_layout: landing
title: Cloudflare.NET SDK
---

<style>
/* Container to match docfx content width */
.landing-container {
  max-width: 1140px;
  margin: 0 auto;
  padding: 0 1.5rem;
}

/* Hero section */
.hero {
  text-align: center;
  padding: 3rem 0 2rem;
}

.hero h1 {
  font-size: 3rem;
  font-weight: 600;
  margin-bottom: 1rem;
  background: linear-gradient(135deg, #f38020, #512bd4);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.hero .tagline {
  font-size: 1.25rem;
  color: var(--bs-secondary-color);
  margin-bottom: 2rem;
  max-width: 600px;
  margin-left: auto;
  margin-right: auto;
}

.hero .badges {
  margin-bottom: 2rem;
}

.hero .badges img {
  margin: 0 0.25rem;
}

/* CTA buttons */
.cta-buttons {
  display: flex;
  gap: 1rem;
  justify-content: center;
  flex-wrap: wrap;
  margin-bottom: 1rem;
}

.cta-buttons .btn {
  padding: 0.75rem 2rem;
  font-size: 1.1rem;
  font-weight: 500;
  border-radius: 0.5rem;
  text-decoration: none;
  transition: transform 0.2s, box-shadow 0.2s;
}

.cta-buttons .btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0,0,0,0.15);
}

.cta-buttons .btn-primary {
  background: linear-gradient(135deg, #f38020, #faad3f);
  border: none;
  color: white;
}

.cta-buttons .btn-outline {
  border: 2px solid var(--bs-border-color);
  background: transparent;
  color: var(--bs-body-color);
}

/* Packages section */
.packages {
  padding: 2rem 0;
}

.packages h2 {
  text-align: center;
  margin-bottom: 2rem;
  font-weight: 600;
}

.package-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
  gap: 1.5rem;
}

.package-card {
  border: 1px solid var(--bs-border-color);
  border-radius: 0.75rem;
  padding: 1.5rem;
  background: var(--bs-body-bg);
  transition: box-shadow 0.2s, transform 0.2s;
}

.package-card:hover {
  box-shadow: 0 4px 20px rgba(0,0,0,0.1);
  transform: translateY(-2px);
}

.package-card h3 {
  font-size: 1.25rem;
  font-weight: 600;
  margin-bottom: 0.75rem;
  color: var(--bs-heading-color);
}

.package-card p {
  color: var(--bs-secondary-color);
  margin-bottom: 1rem;
  font-size: 0.95rem;
}

.package-card code {
  display: block;
  background: var(--bs-tertiary-bg);
  padding: 0.5rem 0.75rem;
  border-radius: 0.375rem;
  font-size: 0.85rem;
  margin-bottom: 1rem;
}

/* Quick start section */
.example {
  padding: 2rem;
  background: var(--bs-tertiary-bg);
  border-radius: 1rem;
  margin: 2rem 0;
}

.example h2 {
  text-align: center;
  margin-bottom: 1.5rem;
  font-weight: 600;
}

/* Features section */
.features {
  padding: 2rem 0;
}

.features h2 {
  text-align: center;
  margin-bottom: 2rem;
  font-weight: 600;
}

.feature-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1.5rem;
}

@media (max-width: 768px) {
  .feature-grid {
    grid-template-columns: 1fr;
  }
}

.feature-item {
  text-align: left;
  padding: 1.5rem;
  border: 1px solid var(--bs-border-color);
  border-radius: 0.75rem;
  background: var(--bs-body-bg);
  transition: box-shadow 0.2s, transform 0.2s;
}

.feature-item:hover {
  box-shadow: 0 4px 20px rgba(0,0,0,0.08);
  transform: translateY(-2px);
}

.feature-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}

.feature-icon {
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 0.5rem;
  background: linear-gradient(135deg, rgba(243, 128, 32, 0.1), rgba(250, 173, 63, 0.1));
  flex-shrink: 0;
}

.feature-icon svg {
  width: 20px;
  height: 20px;
  color: #f38020;
}

.feature-item h4 {
  font-weight: 600;
  margin: 0;
  font-size: 1.1rem;
}

.feature-item p {
  color: var(--bs-secondary-color);
  font-size: 0.9rem;
  margin: 0;
  line-height: 1.6;
}

/* Framework table */
.frameworks {
  padding: 2rem 0;
}

.frameworks h2 {
  text-align: center;
  margin-bottom: 1.5rem;
  font-weight: 600;
}

.frameworks table {
  margin: 0 auto;
}

/* Contributing section */
.contributing {
  padding: 2rem 0;
  text-align: center;
}

.contributing h2 {
  margin-bottom: 1rem;
  font-weight: 600;
}

.contributing p {
  color: var(--bs-secondary-color);
  margin-bottom: 1.5rem;
  max-width: 600px;
  margin-left: auto;
  margin-right: auto;
}
</style>

<div class="landing-container">

<div class="hero">
  <h1>Cloudflare.NET SDK</h1>
  <p class="tagline">An unofficial, strongly-typed .NET SDK for the Cloudflare API. Built with testability and maintainability in mind.</p>

  <div class="badges">
    <a href="https://github.com/Alos-no/Cloudflare.NET/actions/workflows/CI.yml"><img src="https://github.com/Alos-no/Cloudflare.NET/actions/workflows/CI.yml/badge.svg" alt="Build Status"></a>
    <a href="https://github.com/Alos-no/Cloudflare.NET/blob/main/LICENSE.txt"><img src="https://img.shields.io/badge/license-Apache--2.0-blue" alt="License"></a>
  </div>

  <div class="cta-buttons">
    <a href="articles/getting-started.md" class="btn btn-primary">Get Started</a>
    <a href="api/index.md" class="btn btn-outline">API Reference</a>
    <a href="https://github.com/Alos-no/Cloudflare.NET" class="btn btn-outline">GitHub</a>
  </div>
</div>

<div class="packages">
  <h2>Packages</h2>
  <div class="package-grid">
    <div class="package-card">
      <h3>Cloudflare.NET.Api</h3>
      <p>Core REST API client for Cloudflare services including Zones, DNS, Security, and R2 bucket management.</p>
      <code>dotnet add package Cloudflare.NET.Api</code>
      <a href="https://www.nuget.org/packages/Cloudflare.NET.Api/"><img src="https://img.shields.io/nuget/v/Cloudflare.NET.Api?label=NuGet" alt="NuGet"></a>
    </div>
    <div class="package-card">
      <h3>Cloudflare.NET.R2</h3>
      <p>High-level S3-compatible client for R2 object storage with multipart upload support.</p>
      <code>dotnet add package Cloudflare.NET.R2</code>
      <a href="https://www.nuget.org/packages/Cloudflare.NET.R2/"><img src="https://img.shields.io/nuget/v/Cloudflare.NET.R2?label=NuGet" alt="NuGet"></a>
    </div>
    <div class="package-card">
      <h3>Cloudflare.NET.Analytics</h3>
      <p>GraphQL client for querying Cloudflare Analytics API datasets.</p>
      <code>dotnet add package Cloudflare.NET.Analytics</code>
      <a href="https://www.nuget.org/packages/Cloudflare.NET.Analytics/"><img src="https://img.shields.io/nuget/v/Cloudflare.NET.Analytics?label=NuGet" alt="NuGet"></a>
    </div>
  </div>
</div>

<div class="example">
  <h2>Example</h2>

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

</div>

<div class="features">
  <h2>Features</h2>
  <div class="feature-grid">
    <div class="feature-item">
      <div class="feature-header">
        <div class="feature-icon">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M17.25 6.75L22.5 12l-5.25 5.25m-10.5 0L1.5 12l5.25-5.25m7.5-3l-4.5 16.5" />
          </svg>
        </div>
        <h4>Strongly-typed API</h4>
      </div>
      <p>Full IntelliSense support with comprehensive XML documentation for every method, parameter, and model. All Cloudflare API responses are deserialized into rich .NET types with proper nullability annotations.</p>
    </div>
    <div class="feature-item">
      <div class="feature-header">
        <div class="feature-icon">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 0013.803-3.7M4.031 9.865a8.25 8.25 0 0113.803-3.7l3.181 3.182m0-4.991v4.99" />
          </svg>
        </div>
        <h4>CI/CD Pipeline</h4>
      </div>
      <p>Every commit triggers automated builds and tests. Releases are published automatically to NuGet.</p>
    </div>
    <div class="feature-item">
      <div class="feature-header">
        <div class="feature-icon">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z" />
          </svg>
        </div>
        <h4>Resilience Built-in</h4>
      </div>
      <p>Production-ready resilience via Polly: automatic retries with exponential backoff, circuit breaker for fault tolerance, rate limiting to respect API limits, and configurable timeouts per request.</p>
    </div>
    <div class="feature-item">
      <div class="feature-header">
        <div class="feature-icon">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M18 18.72a9.094 9.094 0 003.741-.479 3 3 0 00-4.682-2.72m.94 3.198l.001.031c0 .225-.012.447-.037.666A11.944 11.944 0 0112 21c-2.17 0-4.207-.576-5.963-1.584A6.062 6.062 0 016 18.719m12 0a5.971 5.971 0 00-.941-3.197m0 0A5.995 5.995 0 0012 12.75a5.995 5.995 0 00-5.058 2.772m0 0a3 3 0 00-4.681 2.72 8.986 8.986 0 003.74.477m.94-3.197a5.971 5.971 0 00-.94 3.197M15 6.75a3 3 0 11-6 0 3 3 0 016 0zm6 3a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0zm-13.5 0a2.25 2.25 0 11-4.5 0 2.25 2.25 0 014.5 0z" />
          </svg>
        </div>
        <h4>Multi-account Support</h4>
      </div>
      <p>Manage multiple Cloudflare accounts with named clients and keyed services. Perfect for multi-tenant applications or agencies managing client accounts with isolated configuration per client.</p>
    </div>
    <div class="feature-item">
      <div class="feature-header">
        <div class="feature-icon">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M20.25 6.375c0 2.278-3.694 4.125-8.25 4.125S3.75 8.653 3.75 6.375m16.5 0c0-2.278-3.694-4.125-8.25-4.125S3.75 4.097 3.75 6.375m16.5 0v11.25c0 2.278-3.694 4.125-8.25 4.125s-8.25-1.847-8.25-4.125V6.375m16.5 0v3.75m-16.5-3.75v3.75m16.5 0v3.75C20.25 16.153 16.556 18 12 18s-8.25-1.847-8.25-4.125v-3.75m16.5 0c0 2.278-3.694 4.125-8.25 4.125s-8.25-1.847-8.25-4.125" />
          </svg>
        </div>
        <h4>S3-Compatible R2</h4>
      </div>
      <p>High-level R2 client with intelligent multipart uploads for large files, presigned URLs for direct browser uploads, and automatic retry handling. Compatible with S3 tools and workflows.</p>
    </div>
    <div class="feature-item">
      <div class="feature-header">
        <div class="feature-icon">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M9.75 3.104v5.714a2.25 2.25 0 01-.659 1.591L5 14.5M9.75 3.104c-.251.023-.501.05-.75.082m.75-.082a24.301 24.301 0 014.5 0m0 0v5.714c0 .597.237 1.17.659 1.591L19.8 15.3M14.25 3.104c.251.023.501.05.75.082M19.8 15.3l-1.57.393A9.065 9.065 0 0112 15a9.065 9.065 0 00-6.23-.693L5 14.5m14.8.8l1.402 1.402c1.232 1.232.65 3.318-1.067 3.611A48.309 48.309 0 0112 21c-2.773 0-5.491-.235-8.135-.687-1.718-.293-2.3-2.379-1.067-3.61L5 14.5" />
          </svg>
        </div>
        <h4>Testable by Design</h4>
      </div>
      <p>Developed test-first with integration tests running against real Cloudflare APIs and unit tests validating request/response handling. All tests execute on every commit via CI.</p>
    </div>
  </div>
</div>

<div class="frameworks">
  <h2>Supported Frameworks</h2>

| Package | .NET 8 | .NET 9 | .NET 10 | Strong Named |
|---------|:------:|:------:|:-------:|:------------:|
| **Cloudflare.NET.Api** | ✅ | ✅ | ✅ | ✅ |
| **Cloudflare.NET.R2** | ✅ | ✅ | ✅ | ✅ |
| **Cloudflare.NET.Analytics** | ✅ | ✅ | ✅ | ❌* |

> \* `Cloudflare.NET.Analytics` cannot be strong-named because its dependency (`GraphQL.Client`) is not strong-named.

</div>

<div class="contributing">
  <h2>Contributing</h2>
  <p>We welcome contributions! Whether it's bug reports, feature requests, documentation improvements, or code contributions, we appreciate your help making the SDK better.</p>
  <div class="cta-buttons">
    <a href="https://github.com/Alos-no/Cloudflare.NET" class="btn btn-outline">View on GitHub</a>
    <a href="https://github.com/Alos-no/Cloudflare.NET/issues" class="btn btn-outline">Report an Issue</a>
  </div>
</div>

</div>
