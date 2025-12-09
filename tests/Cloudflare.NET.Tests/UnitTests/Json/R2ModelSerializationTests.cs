namespace Cloudflare.NET.Tests.UnitTests.Json;

using System.Text.Json;
using System.Text.Json.Serialization;
using Accounts.Models;
using Shared.Fixtures;

/// <summary>
///   Contains unit tests for verifying the JSON serialization of R2 bucket models (CORS, bucket creation).
///   These tests ensure the SDK produces JSON that matches the Cloudflare R2 API expectations.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class R2ModelSerializationTests
{
  #region Properties & Fields - Non-Public

  /// <summary>
  ///   JSON serializer options that match the configuration used in AccountsApi.
  ///   This must be kept in sync with the actual implementation.
  /// </summary>
  private readonly JsonSerializerOptions _serializerOptions = new()
  {
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  #endregion


  #region CorsAllowed Serialization Tests

  /// <summary>
  ///   Verifies that CorsAllowed serializes correctly with null Headers.
  ///   Headers are optional in CORS rules.
  /// </summary>
  [Fact]
  public void Serialize_CorsAllowed_NullHeaders_OmitsHeaders()
  {
    // Arrange - CorsAllowed with null Headers
    var corsAllowed = new CorsAllowed(
      Methods: ["GET", "PUT"],
      Origins: ["https://example.com"]
      // Headers is null by default
    );

    // Act
    var json = JsonSerializer.Serialize(corsAllowed, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("methods").GetArrayLength().Should().Be(2);
    root.GetProperty("origins").GetArrayLength().Should().Be(1);
    root.TryGetProperty("headers", out _).Should().BeFalse("null Headers should be omitted");
  }

  /// <summary>
  ///   Verifies that CorsAllowed serializes correctly with explicit empty Headers array.
  /// </summary>
  [Fact]
  public void Serialize_CorsAllowed_EmptyHeaders_IncludesEmptyArray()
  {
    // Arrange - CorsAllowed with empty Headers array
    var corsAllowed = new CorsAllowed(
      Methods: ["GET"],
      Origins: ["*"],
      Headers: [] // Explicit empty array
    );

    // Act
    var json = JsonSerializer.Serialize(corsAllowed, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("headers").GetArrayLength().Should().Be(0, "empty array should be serialized");
  }

  /// <summary>
  ///   Verifies that CorsAllowed serializes correctly with Headers populated.
  /// </summary>
  [Fact]
  public void Serialize_CorsAllowed_WithHeaders_IncludesHeaders()
  {
    // Arrange
    var corsAllowed = new CorsAllowed(
      Methods: ["GET", "POST"],
      Origins: ["https://example.com"],
      Headers: ["Content-Type", "Authorization"]
    );

    // Act
    var json = JsonSerializer.Serialize(corsAllowed, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    var headers = root.GetProperty("headers");
    headers.GetArrayLength().Should().Be(2);
    headers[0].GetString().Should().Be("Content-Type");
    headers[1].GetString().Should().Be("Authorization");
  }

  #endregion


  #region CorsRule Serialization Tests

  /// <summary>
  ///   Verifies that CorsRule serializes correctly with minimal properties.
  ///   Only Allowed is required; Id, ExposeHeaders, and MaxAgeSeconds are optional.
  /// </summary>
  [Fact]
  public void Serialize_CorsRule_Minimal_OmitsOptionalProperties()
  {
    // Arrange - Minimal CorsRule with only required Allowed
    var corsRule = new CorsRule(
      Allowed: new CorsAllowed(["GET"], ["*"])
      // Id, ExposeHeaders, MaxAgeSeconds are null by default
    );

    // Act
    var json = JsonSerializer.Serialize(corsRule, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("allowed").Should().NotBeNull();
    root.TryGetProperty("id", out _).Should().BeFalse("null Id should be omitted");
    root.TryGetProperty("exposeHeaders", out _).Should().BeFalse("null ExposeHeaders should be omitted");
    root.TryGetProperty("maxAgeSeconds", out _).Should().BeFalse("null MaxAgeSeconds should be omitted");
  }

  /// <summary>
  ///   Verifies that CorsRule serializes correctly with all properties populated.
  /// </summary>
  [Fact]
  public void Serialize_CorsRule_Full_IncludesAllProperties()
  {
    // Arrange
    var corsRule = new CorsRule(
      Allowed: new CorsAllowed(["GET", "PUT"], ["https://example.com"], ["Content-Type"]),
      Id: "my-cors-rule",
      ExposeHeaders: ["ETag", "Content-Length"],
      MaxAgeSeconds: 3600
    );

    // Act
    var json = JsonSerializer.Serialize(corsRule, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("id").GetString().Should().Be("my-cors-rule");
    root.GetProperty("exposeHeaders").GetArrayLength().Should().Be(2);
    root.GetProperty("maxAgeSeconds").GetInt32().Should().Be(3600);
    root.GetProperty("allowed").GetProperty("methods").GetArrayLength().Should().Be(2);
  }

  #endregion


  #region BucketCorsPolicy Serialization Tests

  /// <summary>
  ///   Verifies that BucketCorsPolicy with minimal CorsRules serializes correctly.
  ///   This tests the scenario where a user creates a CORS policy with minimal configuration.
  /// </summary>
  [Fact]
  public void Serialize_BucketCorsPolicy_MinimalRules_ProducesValidJson()
  {
    // Arrange - CORS policy with minimal rules
    var corsPolicy = new BucketCorsPolicy([
      new CorsRule(new CorsAllowed(["GET"], ["*"])),
      new CorsRule(new CorsAllowed(["PUT", "POST"], ["https://app.example.com"]))
    ]);

    // Act
    var json = JsonSerializer.Serialize(corsPolicy, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var rules = doc.RootElement.GetProperty("rules");

    rules.GetArrayLength().Should().Be(2);

    // Verify first rule
    var rule1 = rules[0];
    rule1.TryGetProperty("id", out _).Should().BeFalse();
    rule1.GetProperty("allowed").GetProperty("methods")[0].GetString().Should().Be("GET");

    // Verify second rule
    var rule2 = rules[1];
    rule2.GetProperty("allowed").GetProperty("methods").GetArrayLength().Should().Be(2);
  }

  #endregion


  #region CreateBucketRequest Serialization Tests

  /// <summary>
  ///   Verifies that CreateBucketRequest serializes correctly with minimal properties.
  ///   Only Name is required; LocationHint and StorageClass are optional.
  /// </summary>
  [Fact]
  public void Serialize_CreateBucketRequest_Minimal_OmitsOptionalProperties()
  {
    // Arrange - Minimal bucket creation request
    var request = new CreateBucketRequest("my-bucket");

    // Act
    var json = JsonSerializer.Serialize(request, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("name").GetString().Should().Be("my-bucket");
    root.TryGetProperty("locationHint", out _).Should().BeFalse("null LocationHint should be omitted");
    root.TryGetProperty("storageClass", out _).Should().BeFalse("null StorageClass should be omitted");
  }

  /// <summary>
  ///   Verifies that CreateBucketRequest serializes correctly with all properties populated.
  /// </summary>
  [Fact]
  public void Serialize_CreateBucketRequest_Full_IncludesAllProperties()
  {
    // Arrange
    var request = new CreateBucketRequest(
      "my-bucket",
      R2LocationHint.EastEurope,
      R2StorageClass.Standard
    );

    // Act
    var json = JsonSerializer.Serialize(request, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("name").GetString().Should().Be("my-bucket");
    root.GetProperty("locationHint").GetString().Should().Be("eeur");
    root.GetProperty("storageClass").GetString().Should().Be("Standard");
  }

  #endregion
}
