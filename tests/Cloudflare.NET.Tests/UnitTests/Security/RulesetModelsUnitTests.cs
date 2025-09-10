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

  #endregion
}
