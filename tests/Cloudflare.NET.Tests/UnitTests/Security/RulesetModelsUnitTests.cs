namespace Cloudflare.NET.Tests.UnitTests.Security;

using System.Text.Json;
using Core.Json;
using NET.Security.Rulesets.Models;
using Shared.Fixtures;

[Trait("Category", TestConstants.TestCategories.Unit)]
public class RulesetModelsUnitTests
{
  #region Properties & Fields - Non-Public

  private readonly JsonSerializerOptions _serializerOptions = new()
  {
    PropertyNamingPolicy   = JsonNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    Converters             = { new JsonStringEnumMemberConverter() }
  };

  #endregion

  #region Methods

  [Fact]
  public void SkipParameters_ShouldSerialize_WithProducts()
  {
    // Arrange
    var skipParams = new SkipParameters(Products: ["waf", "zoneLockdown"]);
    var rule = new CreateRuleRequest(
      RulesetAction.Skip,
      "true",
      ActionParameters: skipParams);

    // Act
    var json = JsonSerializer.Serialize(rule, _serializerOptions);

    // Assert
    using var doc          = JsonDocument.Parse(json);
    var       actionParams = doc.RootElement.GetProperty("action_parameters");
    var       products     = actionParams.GetProperty("products");
    products.GetArrayLength().Should().Be(2);
    products[0].GetString().Should().Be("waf");
    products[1].GetString().Should().Be("zoneLockdown");
  }

  [Fact]
  public void RateLimitParameters_ShouldSerialize_WithSimulate()
  {
    // Arrange
    var rateLimitParams = new RateLimitParameters(
      ["ip.src"],
      60,
      300,
      10,
      Simulate: true
    );
    var rule = new CreateRuleRequest(
      RulesetAction.Block,
      "true",
      Ratelimit: rateLimitParams);

    // Act
    var json = JsonSerializer.Serialize(rule, _serializerOptions);

    // Assert
    using var doc       = JsonDocument.Parse(json);
    var       ratelimit = doc.RootElement.GetProperty("ratelimit");
    ratelimit.GetProperty("simulate").GetBoolean().Should().BeTrue();
  }


  #region ExecuteParameters Serialization Tests

  /// <summary>
  ///   Verifies that ExecuteParameters serializes correctly without Overrides (null).
  ///   This is a common scenario when deploying a managed ruleset without customizations.
  /// </summary>
  [Fact]
  public void ExecuteParameters_ShouldSerialize_WithoutOverrides()
  {
    // Arrange - Minimal execute parameters (no overrides)
    var executeParams = new ExecuteParameters("managed-ruleset-id", "latest");
    var rule = new CreateRuleRequest(
      RulesetAction.Execute,
      "true",
      ActionParameters: executeParams);

    // Act
    var json = JsonSerializer.Serialize(rule, _serializerOptions);

    // Assert
    using var doc          = JsonDocument.Parse(json);
    var       actionParams = doc.RootElement.GetProperty("action_parameters");

    actionParams.GetProperty("id").GetString().Should().Be("managed-ruleset-id");
    actionParams.GetProperty("version").GetString().Should().Be("latest");
    actionParams.TryGetProperty("overrides", out _).Should().BeFalse("null overrides should be omitted");
  }

  /// <summary>
  ///   Verifies that ExecuteParameters with empty Overrides serializes correctly.
  ///   Tests the case where Overrides is provided but has null Rules and Categories.
  /// </summary>
  [Fact]
  public void ExecuteParameters_ShouldSerialize_WithEmptyOverrides()
  {
    // Arrange - Execute parameters with empty overrides object
    var executeParams = new ExecuteParameters(
      "managed-ruleset-id",
      "latest",
      new ExecuteOverrides() // Empty overrides - all null
    );
    var rule = new CreateRuleRequest(
      RulesetAction.Execute,
      "true",
      ActionParameters: executeParams);

    // Act
    var json = JsonSerializer.Serialize(rule, _serializerOptions);

    // Assert
    using var doc          = JsonDocument.Parse(json);
    var       actionParams = doc.RootElement.GetProperty("action_parameters");

    actionParams.GetProperty("id").GetString().Should().Be("managed-ruleset-id");
    actionParams.GetProperty("version").GetString().Should().Be("latest");

    // With WhenWritingNull, an empty overrides object will serialize as {}
    var overrides = actionParams.GetProperty("overrides");
    overrides.TryGetProperty("rules", out _).Should().BeFalse("null rules should be omitted");
    overrides.TryGetProperty("categories", out _).Should().BeFalse("null categories should be omitted");
  }

  /// <summary>
  ///   Verifies that ExecuteParameters with only rule overrides (no categories) serializes correctly.
  /// </summary>
  [Fact]
  public void ExecuteParameters_ShouldSerialize_WithOnlyRuleOverrides()
  {
    // Arrange
    var executeParams = new ExecuteParameters(
      "managed-ruleset-id",
      "latest",
      new ExecuteOverrides(
        Rules: [new RuleOverride("rule-1", ManagedWafOverrideAction.Log, true)]
      )
    );
    var rule = new CreateRuleRequest(
      RulesetAction.Execute,
      "true",
      ActionParameters: executeParams);

    // Act
    var json = JsonSerializer.Serialize(rule, _serializerOptions);

    // Assert
    using var doc          = JsonDocument.Parse(json);
    var       actionParams = doc.RootElement.GetProperty("action_parameters");
    var       overrides    = actionParams.GetProperty("overrides");

    overrides.GetProperty("rules").GetArrayLength().Should().Be(1);
    overrides.TryGetProperty("categories", out _).Should().BeFalse("null categories should be omitted");
  }

  #endregion


  #region SkipParameters Serialization Tests

  /// <summary>
  ///   Verifies that SkipParameters with only one property set serializes correctly.
  ///   Other null properties should be omitted.
  /// </summary>
  [Fact]
  public void SkipParameters_ShouldSerialize_WithOnlyPhases()
  {
    // Arrange
    var skipParams = new SkipParameters(Phases: ["http_request_firewall_managed"]);
    var rule = new CreateRuleRequest(
      RulesetAction.Skip,
      "true",
      ActionParameters: skipParams);

    // Act
    var json = JsonSerializer.Serialize(rule, _serializerOptions);

    // Assert
    using var doc          = JsonDocument.Parse(json);
    var       actionParams = doc.RootElement.GetProperty("action_parameters");

    actionParams.GetProperty("phases").GetArrayLength().Should().Be(1);
    actionParams.TryGetProperty("rulesets", out _).Should().BeFalse("null rulesets should be omitted");
    actionParams.TryGetProperty("rules", out _).Should().BeFalse("null rules should be omitted");
    actionParams.TryGetProperty("products", out _).Should().BeFalse("null products should be omitted");
  }

  /// <summary>
  ///   Verifies that SkipParameters with only rulesets set serializes correctly.
  /// </summary>
  [Fact]
  public void SkipParameters_ShouldSerialize_WithOnlyRulesets()
  {
    // Arrange
    var skipParams = new SkipParameters(Rulesets: ["ruleset-id-1", "ruleset-id-2"]);
    var rule = new CreateRuleRequest(
      RulesetAction.Skip,
      "true",
      ActionParameters: skipParams);

    // Act
    var json = JsonSerializer.Serialize(rule, _serializerOptions);

    // Assert
    using var doc          = JsonDocument.Parse(json);
    var       actionParams = doc.RootElement.GetProperty("action_parameters");

    actionParams.GetProperty("rulesets").GetArrayLength().Should().Be(2);
    actionParams.TryGetProperty("phases", out _).Should().BeFalse("null phases should be omitted");
    actionParams.TryGetProperty("rules", out _).Should().BeFalse("null rules should be omitted");
    actionParams.TryGetProperty("products", out _).Should().BeFalse("null products should be omitted");
  }

  /// <summary>
  ///   Verifies that a completely empty SkipParameters (all null) serializes as an empty object.
  ///   NOTE: This is a diagnostic test to understand the serialization behavior.
  ///   An empty SkipParameters may not be valid for the Cloudflare API.
  /// </summary>
  [Fact]
  public void SkipParameters_ShouldSerialize_WhenAllNull()
  {
    // Arrange - All properties null
    var skipParams = new SkipParameters();
    var rule = new CreateRuleRequest(
      RulesetAction.Skip,
      "true",
      ActionParameters: skipParams);

    // Act
    var json = JsonSerializer.Serialize(rule, _serializerOptions);

    // Assert
    using var doc          = JsonDocument.Parse(json);
    var       actionParams = doc.RootElement.GetProperty("action_parameters");

    // An empty SkipParameters serializes as {} (empty object)
    actionParams.EnumerateObject().Should().BeEmpty("all null properties should be omitted");
  }

  #endregion


  #region ActionParametersWithResponse Serialization Tests

  /// <summary>
  ///   Verifies that ActionParametersWithResponse with null Response serializes correctly.
  /// </summary>
  [Fact]
  public void ActionParametersWithResponse_ShouldSerialize_WithNullResponse()
  {
    // Arrange
    var actionParams = new ActionParametersWithResponse(null);
    var rule = new CreateRuleRequest(
      RulesetAction.Block,
      "true",
      ActionParameters: actionParams);

    // Act
    var json = JsonSerializer.Serialize(rule, _serializerOptions);

    // Assert
    using var doc          = JsonDocument.Parse(json);
    var       actionParamsElement = doc.RootElement.GetProperty("action_parameters");

    actionParamsElement.TryGetProperty("response", out _).Should().BeFalse("null response should be omitted");
  }

  /// <summary>
  ///   Verifies that Response with minimal properties serializes correctly.
  ///   Content and ContentType are optional.
  /// </summary>
  [Fact]
  public void Response_ShouldSerialize_WithMinimalProperties()
  {
    // Arrange - Response with only StatusCode
    var response = new Response(403);
    var actionParams = new ActionParametersWithResponse(response);
    var rule = new CreateRuleRequest(
      RulesetAction.Block,
      "true",
      ActionParameters: actionParams);

    // Act
    var json = JsonSerializer.Serialize(rule, _serializerOptions);

    // Assert
    using var doc             = JsonDocument.Parse(json);
    var       actionParamsElement = doc.RootElement.GetProperty("action_parameters");
    var       responseElement = actionParamsElement.GetProperty("response");

    responseElement.GetProperty("status_code").GetInt32().Should().Be(403);
    responseElement.TryGetProperty("content", out _).Should().BeFalse("null content should be omitted");
    responseElement.TryGetProperty("content_type", out _).Should().BeFalse("null content_type should be omitted");
  }

  #endregion


  #region RuleOverride Serialization Tests

  /// <summary>
  ///   Verifies that RuleOverride with only Id serializes correctly.
  ///   Action and Enabled are optional.
  /// </summary>
  [Fact]
  public void RuleOverride_ShouldSerialize_WithOnlyId()
  {
    // Arrange - Minimal override (only disable, no action change)
    var executeParams = new ExecuteParameters(
      "managed-ruleset-id",
      "latest",
      new ExecuteOverrides(
        Rules: [new RuleOverride("rule-id")] // Only Id, no Action or Enabled
      )
    );
    var rule = new CreateRuleRequest(
      RulesetAction.Execute,
      "true",
      ActionParameters: executeParams);

    // Act
    var json = JsonSerializer.Serialize(rule, _serializerOptions);

    // Assert
    using var doc           = JsonDocument.Parse(json);
    var       actionParams  = doc.RootElement.GetProperty("action_parameters");
    var       overrides     = actionParams.GetProperty("overrides");
    var       ruleOverrides = overrides.GetProperty("rules");

    ruleOverrides.GetArrayLength().Should().Be(1);
    var ruleOverride = ruleOverrides[0];
    ruleOverride.GetProperty("id").GetString().Should().Be("rule-id");
    ruleOverride.TryGetProperty("action", out _).Should().BeFalse("null action should be omitted");
    ruleOverride.TryGetProperty("enabled", out _).Should().BeFalse("null enabled should be omitted");
  }

  #endregion

  #endregion
}
