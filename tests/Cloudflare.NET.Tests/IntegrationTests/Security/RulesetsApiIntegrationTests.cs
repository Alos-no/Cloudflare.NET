namespace Cloudflare.NET.Tests.IntegrationTests.Security;

using System.Net;
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
  public async Task WafCustomRule_CanCreateAndCleanup()
  {
    // A WAF Custom Rule is created by updating the entrypoint ruleset for the `http_request_firewall_custom` phase. [1, 7, 15, 16, 20]
    var zoneId = _settings.ZoneId;
    var phase  = SecurityConstants.RulesetPhases.HttpRequestFirewallCustom;

    IReadOnlyList<CreateRuleRequest> originalRules = await GetOriginalRulesAsync(zoneId, phase);

    var newRuleDescription = $"cfnet-test-custom-rule-{Guid.NewGuid():N}";
    var newRule = new CreateRuleRequest(
      RulesetAction.Block,
      $"(http.host eq \"{_settings.BaseDomain}\" and http.request.uri.path eq \"/cfnet-test-block\")",
      newRuleDescription,
      true
    );
    var updatedRules = originalRules.Append(newRule).ToList();

    try
    {
      // Act
      var updatedRuleset = await _sut.Zones.Rulesets.UpdatePhaseEntrypointAsync(zoneId, phase, updatedRules);

      // Assert
      updatedRuleset.Should().NotBeNull();
      updatedRuleset.Rules.Should().HaveCount(originalRules.Count + 1);
      updatedRuleset.Rules.Should().Contain(r => r.Description == newRuleDescription);
    }
    finally
    {
      // Cleanup: Restore the ruleset to its original state.
      await _sut.Zones.Rulesets.UpdatePhaseEntrypointAsync(zoneId, phase, originalRules);
    }
  }

  [IntegrationTest]
  public async Task WafManagedRule_CanDeployAndCleanup()
  {
    // A Managed Ruleset is deployed by adding an 'execute' rule to the entrypoint for the `http_request_firewall_managed` phase. [2, 6, 9, 25]
    var zoneId = _settings.ZoneId;
    var phase  = SecurityConstants.RulesetPhases.HttpRequestFirewallManaged;

    IReadOnlyList<CreateRuleRequest> originalRules = await GetOriginalRulesAsync(zoneId, phase);

    // On Free plans, you must deploy the "Cloudflare Free Managed Ruleset".
    // Discover its ID from the account-level rulesets (managed rulesets live at account scope).
    // Ref: Availability matrix + recommended workflow to list then deploy. [Docs]
    // - Availability: Free includes "Cloudflare Free Managed Ruleset"; OWASP/Cloudflare Managed are Pro+. 
    // - Workflow: List account rulesets, then add an 'execute' rule at the zone entrypoint.
    // Sources: WAF Managed Rules (availability), Blog "WAF for everyone", Deploy via API. 
    var freeManaged =
      await _sut.Accounts.Rulesets
                .ListAllAsync()
                .FirstOrDefaultAsync(r => string.Equals(r.Name, "Cloudflare Managed Free Ruleset", StringComparison.Ordinal));

    freeManaged.Should().NotBeNull("the account should expose the Cloudflare Free Managed Ruleset on Free plans");

    var managedRulesetId = freeManaged.Id;
    var newRule = new CreateRuleRequest(
      RulesetAction.Execute,
      "true", // This expression means the managed ruleset will run for all requests
      $"cfnet-deploy-managed-ruleset-{Guid.NewGuid():N}",
      true,
      new ExecuteParameters(managedRulesetId, "latest")
    );
    var updatedRules = originalRules.Append(newRule).ToList();

    try
    {
      // Act
      var updatedRuleset = await _sut.Zones.Rulesets.UpdatePhaseEntrypointAsync(zoneId, phase, updatedRules);

      // Assert
      updatedRuleset.Should().NotBeNull();
      updatedRuleset.Rules.Should().HaveCount(originalRules.Count + 1);
      var deployedRule = updatedRuleset.Rules.Should().Contain(r => r.Action == RulesetAction.Execute).Subject;
      var actionParams = System.Text.Json.JsonSerializer.Deserialize<ExecuteParameters>(
        (System.Text.Json.JsonElement)deployedRule.ActionParameters!);
      actionParams!.Id.Should().Be(managedRulesetId);
    }
    finally
    {
      // Cleanup
      await _sut.Zones.Rulesets.UpdatePhaseEntrypointAsync(zoneId, phase, originalRules);
    }
  }

  [IntegrationTest]
  public async Task PhaseEntrypointVersioning_CanListVersions()
  {
    // Arrange
    var zoneId = _settings.ZoneId;
    var phase  = SecurityConstants.RulesetPhases.HttpRequestFirewallCustom;

    // Get the original rules, handling the case where the entrypoint doesn't exist yet.
    IReadOnlyList<CreateRuleRequest> originalRules;
    try
    {
      var originalRuleset = await _sut.Zones.Rulesets.GetPhaseEntrypointAsync(zoneId, phase);
      originalRules = originalRuleset.Rules?.Select(r => new CreateRuleRequest(
                                                      r.Action,
                                                      r.Expression,
                                                      r.Description,
                                                      r.Enabled,
                                                      r.ActionParameters,
                                                      r.Logging,
                                                      r.Ratelimit))
                                     .ToList() ?? [];
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
      // This is an expected state. The original state is an empty set of rules.
      originalRules = [];
    }

    var newRuleDescription = $"cfnet-test-{Guid.NewGuid():N}";
    var newRule = new CreateRuleRequest(
      RulesetAction.Block, // Block is available on all plans.
      "http.host eq \"example.com\"",
      newRuleDescription,
      false
    );
    var updatedRules = originalRules.Append(newRule).ToList();

    try
    {
      // Act
      // 1. Update the entrypoint. This will create it if it doesn't exist, or update it if it does.
      var updatedRuleset = await _sut.Zones.Rulesets.UpdatePhaseEntrypointAsync(
        zoneId,
        phase,
        updatedRules);
      updatedRuleset.Should().NotBeNull();
      updatedRuleset.Rules.Should().HaveCount(originalRules.Count + 1);

      // 2. List the versions.
      var versionsResult = await _sut.Zones.Rulesets.ListPhaseEntrypointVersionsAsync(zoneId, phase);

      // Assert
      versionsResult.Should().NotBeNull();
      versionsResult.Items.Should().NotBeEmpty();
      // Check that the new version is present. This is more robust than checking the count.
      versionsResult.Items.Should().Contain(v => v.Version == updatedRuleset.Version);
      var latestVersion = versionsResult.Items.MaxBy(v => v.LastUpdated);
      latestVersion!.Version.Should().Be(updatedRuleset.Version);
    }
    finally
    {
      // Cleanup: Restore the ruleset to its original state.
      await _sut.Zones.Rulesets.UpdatePhaseEntrypointAsync(
        zoneId,
        phase,
        originalRules);
    }
  }

  /// <summary>
  ///   Helper to get the original list of rules for a phase, handling the case where the entrypoint might not exist
  ///   yet (which is a valid state).
  /// </summary>
  private async Task<IReadOnlyList<CreateRuleRequest>> GetOriginalRulesAsync(string zoneId, string phase)
  {
    try
    {
      var originalRuleset = await _sut.Zones.Rulesets.GetPhaseEntrypointAsync(zoneId, phase);
      return originalRuleset.Rules?.Select(r => new CreateRuleRequest(
                                             r.Action,
                                             r.Expression,
                                             r.Description,
                                             r.Enabled,
                                             r.ActionParameters,
                                             r.Logging,
                                             r.Ratelimit))
                            .ToList() ?? [];
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
      // If the entrypoint doesn't exist, the original state is an empty set of rules.
      return [];
    }
  }

  #endregion
}
