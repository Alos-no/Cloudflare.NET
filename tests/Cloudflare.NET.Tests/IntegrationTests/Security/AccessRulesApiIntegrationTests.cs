namespace Cloudflare.NET.Tests.IntegrationTests.Security;

using System.Net;
using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NET.Security.Firewall.Models;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;

/// <summary>
///   Contains integration tests for account and zone access rules APIs. Tests are grouped in a collection to run
///   sequentially, preventing conflicts when multiple tests attempt to create rules with the same reserved IP addresses.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Integration)]
[Collection(TestCollections.AccessRules)]
public class AccessRulesApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  private readonly ICloudflareApiClient   _sut;
  private readonly TestCloudflareSettings _settings;

  #endregion

  #region Constructors

  public AccessRulesApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut      = fixture.ServiceProvider.GetRequiredService<ICloudflareApiClient>();
    _settings = TestConfiguration.CloudflareSettings;
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion

  #region Methods

  [IntegrationTest]
  public async Task AccountAccessRule_CanManageLifecycle()
  {
    // The IP 192.0.2.1 is reserved for documentation and examples by RFC 5737, ensuring this test IP will not conflict with a real-world address. [3, 10, 11, 13, 14]
    var testIp = "192.0.2.1";
    var createRequest = new CreateAccessRuleRequest(AccessRuleMode.Challenge, new IpConfiguration(testIp), "cfnet-test-account-rule");
    AccessRule? createdRule = null;

    // Pre-cleanup: Remove any stale rules from previous failed test runs.
    // This prevents "duplicate_of_existing" errors when the test is re-run after a failure.
    var existingRules = await _sut.Accounts.AccessRules
                                  .ListAllAsync(new ListAccessRulesFilters { ConfigurationValue = testIp })
                                  .ToListAsync();

    foreach (var staleRule in existingRules)
      await _sut.Accounts.AccessRules.DeleteAsync(staleRule.Id);

    try
    {
      // Act & Assert

      // 1. Create
      createdRule = await _sut.Accounts.AccessRules.CreateAsync(createRequest);
      createdRule.Should().NotBeNull();
      createdRule.Configuration.Value.Should().Be(testIp);
      createdRule.Mode.Should().Be(AccessRuleMode.Challenge);

      // 2. Get
      var getResult = await _sut.Accounts.AccessRules.GetAsync(createdRule.Id);
      getResult.Should().BeEquivalentTo(createdRule, options => options.Excluding(r => r.ModifiedOn));

      // 3. List
      var listResult = await _sut.Accounts.AccessRules.ListAllAsync(new ListAccessRulesFilters { ConfigurationValue = testIp })
                                 .ToListAsync();
      listResult.Should().ContainSingle(r => r.Id == createdRule.Id);

      // 4. Update
      var updateRequest = new UpdateAccessRuleRequest(Notes: "cfnet-test-updated-note");
      var updatedRule   = await _sut.Accounts.AccessRules.UpdateAsync(createdRule.Id, updateRequest);
      updatedRule.Notes.Should().Be("cfnet-test-updated-note");
    }
    finally
    {
      // 5. Delete (Cleanup)
      if (createdRule is not null)
      {
        await _sut.Accounts.AccessRules.DeleteAsync(createdRule.Id);
        var rules = await _sut.Accounts.AccessRules.ListAllAsync(new ListAccessRulesFilters { ConfigurationValue = testIp })
                              .ToListAsync();
        rules.Should().NotContain(r => r.Id == createdRule.Id);
      }
    }
  }

  [IntegrationTest]
  public async Task ZoneAccessRule_CanManageLifecycle()
  {
    // The IP 192.0.2.2 is part of a range reserved for documentation and examples by RFC 5737. [3, 10, 11, 13, 14]
    var         testIp        = "192.0.2.2";
    var         createRequest = new CreateAccessRuleRequest(AccessRuleMode.Block, new IpConfiguration(testIp), "cfnet-test-zone-rule");
    AccessRule? createdRule   = null;

    // Pre-cleanup: Remove any stale rules from previous failed test runs.
    // This prevents "duplicate_of_existing" errors when the test is re-run after a failure.
    var existingRules = await _sut.Zones.AccessRules
                                  .ListAllAsync(_settings.ZoneId, new ListAccessRulesFilters { ConfigurationValue = testIp })
                                  .ToListAsync();

    foreach (var staleRule in existingRules)
      await _sut.Zones.AccessRules.DeleteAsync(_settings.ZoneId, staleRule.Id);

    try
    {
      // Act & Assert
      createdRule = await _sut.Zones.AccessRules.CreateAsync(_settings.ZoneId, createRequest);
      createdRule.Should().NotBeNull();
      createdRule.Configuration.Value.Should().Be(testIp);
      createdRule.Mode.Should().Be(AccessRuleMode.Block);

      var getResult = await _sut.Zones.AccessRules.GetAsync(_settings.ZoneId, createdRule.Id);
      getResult.Should().BeEquivalentTo(createdRule, options => options.Excluding(r => r.ModifiedOn));

      var listResult = await _sut.Zones.AccessRules
                                 .ListAllAsync(_settings.ZoneId, new ListAccessRulesFilters { ConfigurationValue = testIp })
                                 .ToListAsync();
      listResult.Should().ContainSingle(r => r.Id == createdRule.Id);

      var updateRequest = new UpdateAccessRuleRequest(Notes: "cfnet-test-zone-updated-note");
      var updatedRule   = await _sut.Zones.AccessRules.UpdateAsync(_settings.ZoneId, createdRule.Id, updateRequest);
      updatedRule.Notes.Should().Be("cfnet-test-zone-updated-note");
    }
    finally
    {
      if (createdRule is not null)
      {
        await _sut.Zones.AccessRules.DeleteAsync(_settings.ZoneId, createdRule.Id);
        var rules = await _sut.Zones.AccessRules.ListAllAsync(_settings.ZoneId, new ListAccessRulesFilters { ConfigurationValue = testIp })
                              .ToListAsync();
        rules.Should().NotContain(r => r.Id == createdRule.Id);
      }
    }
  }

  [IntegrationTest]
  public async Task GetAsync_WhenRuleDoesNotExist_ThrowsNotFound()
  {
    // Arrange
    var nonExistentRuleId = "00000000000000000000000000000000";

    // Act
    var action = async () => await _sut.Zones.AccessRules.GetAsync(_settings.ZoneId, nonExistentRuleId);

    // Assert
    var ex = await action.Should().ThrowAsync<HttpRequestException>();
    ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
  }

  #endregion
}
