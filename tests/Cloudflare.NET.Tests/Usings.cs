global using Xunit;
global using FluentAssertions;
global using Moq;
global using Cloudflare.NET;
global using Cloudflare.NET.Core;
global using System.Net.Http.Json;

// Use custom test collection orderer to ensure permission validation tests run first.
// The IntegrationTestCollectionOrderer prioritizes collections starting with '!' (like PermissionValidation).
[assembly: TestCollectionOrderer(
  "Cloudflare.NET.Tests.Shared.IntegrationTestCollectionOrderer",
  "Cloudflare.NET.Tests.Shared")]
