namespace Cloudflare.NET.Tests.UnitTests.Security;

using System.Text.Json;
using System.Text.Json.Serialization;
using Cloudflare.NET.Core.Json;
using Cloudflare.NET.Security.Rulesets.Models;
using Shared.Fixtures;

/// <summary>
///   Contains unit tests for <see cref="RulesetAction" /> extensible enum.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class RulesetActionTests
{
  #region Properties & Fields - Non-Public

  /// <summary>JSON serializer options configured with the extensible enum converter factory.</summary>
  private readonly JsonSerializerOptions _serializerOptions = new()
  {
    PropertyNamingPolicy   = JsonNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters             = { new ExtensibleEnumConverterFactory() }
  };

  #endregion


  #region Serialization Tests

  [Fact]
  public void Serialize_KnownValue_WritesCorrectString()
  {
    // Arrange
    var action = RulesetAction.Block;

    // Act
    var json = JsonSerializer.Serialize(action, _serializerOptions);

    // Assert
    json.Should().Be("\"block\"");
  }

  [Fact]
  public void Serialize_CustomValue_WritesCorrectString()
  {
    // Arrange
    RulesetAction customAction = "future-action-2030";

    // Act
    var json = JsonSerializer.Serialize(customAction, _serializerOptions);

    // Assert
    json.Should().Be("\"future-action-2030\"");
  }

  [Fact]
  public void Serialize_AllKnownActions_WriteCorrectValues()
  {
    // Arrange & Act & Assert
    JsonSerializer.Serialize(RulesetAction.Block, _serializerOptions).Should().Be("\"block\"");
    JsonSerializer.Serialize(RulesetAction.Challenge, _serializerOptions).Should().Be("\"challenge\"");
    JsonSerializer.Serialize(RulesetAction.JsChallenge, _serializerOptions).Should().Be("\"js_challenge\"");
    JsonSerializer.Serialize(RulesetAction.ManagedChallenge, _serializerOptions).Should().Be("\"managed_challenge\"");
    JsonSerializer.Serialize(RulesetAction.Log, _serializerOptions).Should().Be("\"log\"");
    JsonSerializer.Serialize(RulesetAction.Skip, _serializerOptions).Should().Be("\"skip\"");
    JsonSerializer.Serialize(RulesetAction.Execute, _serializerOptions).Should().Be("\"execute\"");
    JsonSerializer.Serialize(RulesetAction.Rewrite, _serializerOptions).Should().Be("\"rewrite\"");
    JsonSerializer.Serialize(RulesetAction.Redirect, _serializerOptions).Should().Be("\"redirect\"");
    JsonSerializer.Serialize(RulesetAction.Route, _serializerOptions).Should().Be("\"route\"");
    JsonSerializer.Serialize(RulesetAction.SetConfig, _serializerOptions).Should().Be("\"set_config\"");
    JsonSerializer.Serialize(RulesetAction.CompressResponse, _serializerOptions).Should().Be("\"compress_response\"");
    JsonSerializer.Serialize(RulesetAction.SetCacheSettings, _serializerOptions).Should().Be("\"set_cache_settings\"");
    JsonSerializer.Serialize(RulesetAction.ServeError, _serializerOptions).Should().Be("\"serve_error\"");
    JsonSerializer.Serialize(RulesetAction.LogCustomField, _serializerOptions).Should().Be("\"log_custom_field\"");
  }

  #endregion


  #region Deserialization Tests

  [Fact]
  public void Deserialize_KnownValue_ReturnsCorrectInstance()
  {
    // Arrange
    const string json = "\"block\"";

    // Act
    var action = JsonSerializer.Deserialize<RulesetAction>(json, _serializerOptions);

    // Assert
    action.Should().Be(RulesetAction.Block);
    action.Value.Should().Be("block");
  }

  [Fact]
  public void Deserialize_CustomValue_ReturnsInstanceWithCustomValue()
  {
    // Arrange - simulating API returning a new action not yet defined in SDK
    const string json = "\"new-security-action-2030\"";

    // Act
    var action = JsonSerializer.Deserialize<RulesetAction>(json, _serializerOptions);

    // Assert
    action.Value.Should().Be("new-security-action-2030");
  }

  [Fact]
  public void Deserialize_NullValue_ReturnsDefault()
  {
    // Arrange
    const string json = "null";

    // Act
    var action = JsonSerializer.Deserialize<RulesetAction>(json, _serializerOptions);

    // Assert
    action.Should().Be(default(RulesetAction));
    action.Value.Should().BeEmpty();
  }

  [Fact]
  public void Deserialize_CaseInsensitive_ReturnsCorrectInstance()
  {
    // Arrange - API values are lowercase, but we should be case-insensitive
    const string json = "\"BLOCK\"";

    // Act
    var action = JsonSerializer.Deserialize<RulesetAction>(json, _serializerOptions);

    // Assert
    action.Should().Be(RulesetAction.Block);
  }

  #endregion


  #region Round-Trip Tests

  [Theory]
  [InlineData("block")]
  [InlineData("challenge")]
  [InlineData("js_challenge")]
  [InlineData("managed_challenge")]
  [InlineData("log")]
  [InlineData("skip")]
  [InlineData("execute")]
  [InlineData("rewrite")]
  [InlineData("redirect")]
  [InlineData("route")]
  [InlineData("set_config")]
  [InlineData("compress_response")]
  [InlineData("set_cache_settings")]
  [InlineData("serve_error")]
  [InlineData("log_custom_field")]
  [InlineData("custom-future-action")]
  public void RoundTrip_Action_PreservesValue(string value)
  {
    // Arrange
    var original = new RulesetAction(value);

    // Act
    var json         = JsonSerializer.Serialize(original, _serializerOptions);
    var deserialized = JsonSerializer.Deserialize<RulesetAction>(json, _serializerOptions);

    // Assert
    deserialized.Value.Should().Be(value);
  }

  #endregion


  #region Equality Tests

  [Fact]
  public void Equals_SameKnownValue_ReturnsTrue()
  {
    // Arrange
    var action1 = RulesetAction.Block;
    var action2 = RulesetAction.Block;

    // Act & Assert
    action1.Should().Be(action2);
    (action1 == action2).Should().BeTrue();
    action1.Equals(action2).Should().BeTrue();
  }

  [Fact]
  public void Equals_DifferentValues_ReturnsFalse()
  {
    // Arrange
    var action1 = RulesetAction.Block;
    var action2 = RulesetAction.Challenge;

    // Act & Assert
    action1.Should().NotBe(action2);
    (action1 != action2).Should().BeTrue();
    action1.Equals(action2).Should().BeFalse();
  }

  [Fact]
  public void Equals_CaseInsensitive_ReturnsTrue()
  {
    // Arrange
    var action1 = new RulesetAction("block");
    var action2 = new RulesetAction("BLOCK");

    // Act & Assert
    action1.Should().Be(action2);
    (action1 == action2).Should().BeTrue();
  }

  [Fact]
  public void GetHashCode_SameValue_ReturnsSameHash()
  {
    // Arrange
    var action1 = RulesetAction.Block;
    var action2 = new RulesetAction("block");
    var action3 = new RulesetAction("BLOCK");

    // Act & Assert
    action1.GetHashCode().Should().Be(action2.GetHashCode());
    action1.GetHashCode().Should().Be(action3.GetHashCode());
  }

  #endregion


  #region Implicit Conversion Tests

  [Fact]
  public void ImplicitConversion_FromString_CreatesInstance()
  {
    // Arrange & Act
    RulesetAction action = "block";

    // Assert
    action.Value.Should().Be("block");
    action.Should().Be(RulesetAction.Block);
  }

  [Fact]
  public void ImplicitConversion_ToString_ReturnsValue()
  {
    // Arrange
    var action = RulesetAction.Execute;

    // Act
    string value = action;

    // Assert
    value.Should().Be("execute");
  }

  #endregion


  #region Model Integration Tests

  [Fact]
  public void Serialize_RuleWithAction_ProducesCorrectJson()
  {
    // Arrange
    var rule = new Rule(
      "rule-123",
      "1",
      RulesetAction.Block,
      "ip.src eq 1.2.3.4",
      true,
      DateTime.Parse("2024-01-15T10:30:00Z").ToUniversalTime(),
      "Test rule"
    );

    // Act
    var json = JsonSerializer.Serialize(rule, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    doc.RootElement.GetProperty("action").GetString().Should().Be("block");
  }

  [Fact]
  public void Deserialize_RuleWithUnknownAction_ParsesSuccessfully()
  {
    // Arrange - simulating API returning a new action not yet defined in SDK
    const string json = """
                        {
                          "id": "rule-456",
                          "version": "2",
                          "action": "quantum_shield_v3",
                          "expression": "http.request.uri.path contains \"/admin\"",
                          "enabled": true,
                          "last_updated": "2030-06-15T14:30:00Z"
                        }
                        """;

    // Act
    var rule = JsonSerializer.Deserialize<Rule>(json, _serializerOptions);

    // Assert
    rule.Should().NotBeNull();
    rule!.Action.Value.Should().Be("quantum_shield_v3");
  }

  [Fact]
  public void Serialize_CreateRuleRequest_ProducesCorrectJson()
  {
    // Arrange
    var request = new CreateRuleRequest(
      RulesetAction.ManagedChallenge,
      "ip.geoip.country eq \"CN\"",
      "Block suspicious traffic"
    );

    // Act
    var json = JsonSerializer.Serialize(request, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    doc.RootElement.GetProperty("action").GetString().Should().Be("managed_challenge");
    doc.RootElement.GetProperty("expression").GetString().Should().Be("ip.geoip.country eq \"CN\"");
  }

  #endregion


  #region ToString Tests

  [Fact]
  public void ToString_ReturnsValue()
  {
    // Arrange
    var action = RulesetAction.Skip;

    // Act
    var result = action.ToString();

    // Assert
    result.Should().Be("skip");
  }

  #endregion
}
