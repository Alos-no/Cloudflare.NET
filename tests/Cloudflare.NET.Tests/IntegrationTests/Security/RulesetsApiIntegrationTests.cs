namespace Cloudflare.NET.Tests.IntegrationTests.Security;

using Fixtures;
using Microsoft.Extensions.DependencyInjection;
using NET.Security;
using NET.Security.Rulesets.Models;
using Shared.Fixtures;
using Shared.Helpers;
using Xunit.Abstractions;

[Trait("Category", TestConstants.TestCategories.Integration)]
public class RulesetsApiIntegrationTests : IClassFixture<CloudflareApiTestFixture>
{
  #region Properties & Fields - Non-Public

  private readonly ICloudflareApiClient   _sut;
  private readonly TestCloudflareSettings _settings;

  #endregion

  #region Constructors

  public RulesetsApiIntegrationTests(CloudflareApiTestFixture fixture, ITestOutputHelper output)
  {
    _sut      = fixture.ServiceProvider.GetRequiredService<ICloudflareApiClient>();
    _settings = TestConfiguration.CloudflareSettings;
    var loggerProvider = fixture.ServiceProvider.GetRequiredService<XunitTestOutputLoggerProvider>();
    loggerProvider.Current = output;
  }

  #endregion

  #region Methods

  [IntegrationTest]
  public async Task PhaseEntrypointVersioning_CanListVersions()
  {
    // Arrange
    var zoneId             = _settings.ZoneId;
    var phase              = SecurityConstants.RulesetPhases.HttpRequestFirewallCustom;
    var originalRuleset    = await _sut.Zones.Rulesets.GetPhaseEntrypointAsync(zoneId, phase);
    var newRuleDescription = $"cfnet-test-{Guid.NewGuid():N}";
    var newRule = new CreateRuleRequest(
      RulesetAction.Log,
      "http.host eq \"example.com\"",
      newRuleDescription,
      false
    );

    // Create a new list of rules including the original ones plus our new one.
    var originalRules = originalRuleset.Rules?.Select(r => new CreateRuleRequest(
                                                        r.Action,
                                                        r.Expression,
                                                        r.Description,
                                                        r.Enabled,
                                                        r.ActionParameters,
                                                        r.Logging,
                                                        r.Ratelimit))
      ?? [];
    var updatedRules = originalRules.Append(newRule).ToList();

    Ruleset? updatedRuleset = null;
    try
    {
      // Act
      // 1. Update the entrypoint to create a new version.
      updatedRuleset = await _sut.Zones.Rulesets.UpdatePhaseEntrypointAsync(
        zoneId,
        phase,
        updatedRules.Select(cr => new Rule("", "", cr.Action, cr.Expression, cr.Enabled ?? true, DateTime.UtcNow,
                                           cr.Description, cr.ActionParameters, cr.Logging, cr.Ratelimit)).ToList());
      updatedRuleset.Should().NotBeNull();
      updatedRuleset.Rules.Should().HaveCount(originalRuleset.Rules?.Count ?? 0 + 1);

      // 2. List the versions.
      var versionsResult = await _sut.Zones.Rulesets.ListPhaseEntrypointVersionsAsync(zoneId, phase);

      // Assert
      versionsResult.Should().NotBeNull();
      versionsResult.Items.Should().NotBeEmpty();
      versionsResult.Items.Count.Should().BeGreaterThanOrEqualTo(2);
      var latestVersion = versionsResult.Items.MaxBy(v => v.LastUpdated);
      latestVersion!.Version.Should().Be(updatedRuleset.Version);
    }
    finally
    {
      // Cleanup: Restore the ruleset to its original state.
      if (updatedRuleset is not null)
        await _sut.Zones.Rulesets.UpdatePhaseEntrypointAsync(
          zoneId,
          phase,
          originalRuleset.Rules ?? Enumerable.Empty<Rule>());
    }
  }

  #endregion
}
