namespace Cloudflare.NET.Tests.UnitTests.Json;

using System.Text.Json;
using System.Text.Json.Serialization;
using Accounts.Models;
using Core.Json;
using Shared.Fixtures;

/// <summary>
///   Contains unit tests for <see cref="ExtensibleEnumConverter{T}" /> and
///   <see cref="ExtensibleEnumConverterFactory" />.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class ExtensibleEnumConverterTests
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


  #region ExtensibleEnumConverter - Serialization Tests

  [Fact]
  public void Serialize_KnownValue_WritesCorrectString()
  {
    // Arrange
    var location = R2LocationHint.EastNorthAmerica;

    // Act
    var json = JsonSerializer.Serialize(location, _serializerOptions);

    // Assert
    json.Should().Be("\"enam\"");
  }

  [Fact]
  public void Serialize_CustomValue_WritesCorrectString()
  {
    // Arrange
    R2LocationHint customLocation = "custom-region-2025";

    // Act
    var json = JsonSerializer.Serialize(customLocation, _serializerOptions);

    // Assert
    json.Should().Be("\"custom-region-2025\"");
  }

  [Fact]
  public void Serialize_DefaultValue_WritesEmptyString()
  {
    // Arrange
    var defaultLocation = default(R2LocationHint);

    // Act
    var json = JsonSerializer.Serialize(defaultLocation, _serializerOptions);

    // Assert
    json.Should().Be("\"\"");
  }

  [Fact]
  public void Serialize_AllKnownLocationHints_WriteCorrectValues()
  {
    // Arrange & Act & Assert
    JsonSerializer.Serialize(R2LocationHint.WestNorthAmerica, _serializerOptions).Should().Be("\"wnam\"");
    JsonSerializer.Serialize(R2LocationHint.EastNorthAmerica, _serializerOptions).Should().Be("\"enam\"");
    JsonSerializer.Serialize(R2LocationHint.WestEurope, _serializerOptions).Should().Be("\"weur\"");
    JsonSerializer.Serialize(R2LocationHint.EastEurope, _serializerOptions).Should().Be("\"eeur\"");
    JsonSerializer.Serialize(R2LocationHint.AsiaPacific, _serializerOptions).Should().Be("\"apac\"");
    JsonSerializer.Serialize(R2LocationHint.Oceania, _serializerOptions).Should().Be("\"oc\"");
  }

  [Fact]
  public void Serialize_AllKnownJurisdictions_WriteCorrectValues()
  {
    // Arrange & Act & Assert
    JsonSerializer.Serialize(R2Jurisdiction.Default, _serializerOptions).Should().Be("\"default\"");
    JsonSerializer.Serialize(R2Jurisdiction.EuropeanUnion, _serializerOptions).Should().Be("\"eu\"");
    JsonSerializer.Serialize(R2Jurisdiction.FedRamp, _serializerOptions).Should().Be("\"fedramp\"");
  }

  [Fact]
  public void Serialize_AllKnownStorageClasses_WriteCorrectValues()
  {
    // Arrange & Act & Assert
    JsonSerializer.Serialize(R2StorageClass.Standard, _serializerOptions).Should().Be("\"Standard\"");
    JsonSerializer.Serialize(R2StorageClass.InfrequentAccess, _serializerOptions).Should().Be("\"InfrequentAccess\"");
  }

  #endregion


  #region ExtensibleEnumConverter - Deserialization Tests

  [Fact]
  public void Deserialize_KnownValue_ReturnsCorrectInstance()
  {
    // Arrange
    const string json = "\"enam\"";

    // Act
    var location = JsonSerializer.Deserialize<R2LocationHint>(json, _serializerOptions);

    // Assert
    location.Should().Be(R2LocationHint.EastNorthAmerica);
    location.Value.Should().Be("enam");
  }

  [Fact]
  public void Deserialize_CustomValue_ReturnsInstanceWithCustomValue()
  {
    // Arrange
    const string json = "\"future-region-2030\"";

    // Act
    var location = JsonSerializer.Deserialize<R2LocationHint>(json, _serializerOptions);

    // Assert
    location.Value.Should().Be("future-region-2030");
  }

  [Fact]
  public void Deserialize_NullValue_ReturnsDefault()
  {
    // Arrange
    const string json = "null";

    // Act
    var location = JsonSerializer.Deserialize<R2LocationHint>(json, _serializerOptions);

    // Assert
    location.Should().Be(default(R2LocationHint));
    location.Value.Should().BeEmpty();
  }

  [Fact]
  public void Deserialize_EmptyString_ReturnsInstanceWithEmptyValue()
  {
    // Arrange
    const string json = "\"\"";

    // Act
    var location = JsonSerializer.Deserialize<R2LocationHint>(json, _serializerOptions);

    // Assert
    location.Value.Should().BeEmpty();
  }

  #endregion


  #region ExtensibleEnumConverter - Round-Trip Tests

  [Theory]
  [InlineData("wnam")]
  [InlineData("enam")]
  [InlineData("weur")]
  [InlineData("eeur")]
  [InlineData("apac")]
  [InlineData("oc")]
  [InlineData("custom-value")]
  [InlineData("UPPERCASE")]
  public void RoundTrip_LocationHint_PreservesValue(string value)
  {
    // Arrange
    var original = new R2LocationHint(value);

    // Act
    var json         = JsonSerializer.Serialize(original, _serializerOptions);
    var deserialized = JsonSerializer.Deserialize<R2LocationHint>(json, _serializerOptions);

    // Assert
    deserialized.Value.Should().Be(value);
  }

  [Theory]
  [InlineData("default")]
  [InlineData("eu")]
  [InlineData("fedramp")]
  [InlineData("future-jurisdiction")]
  public void RoundTrip_Jurisdiction_PreservesValue(string value)
  {
    // Arrange
    var original = new R2Jurisdiction(value);

    // Act
    var json         = JsonSerializer.Serialize(original, _serializerOptions);
    var deserialized = JsonSerializer.Deserialize<R2Jurisdiction>(json, _serializerOptions);

    // Assert
    deserialized.Value.Should().Be(value);
  }

  [Theory]
  [InlineData("Standard")]
  [InlineData("InfrequentAccess")]
  [InlineData("FutureStorageClass")]
  public void RoundTrip_StorageClass_PreservesValue(string value)
  {
    // Arrange
    var original = new R2StorageClass(value);

    // Act
    var json         = JsonSerializer.Serialize(original, _serializerOptions);
    var deserialized = JsonSerializer.Deserialize<R2StorageClass>(json, _serializerOptions);

    // Assert
    deserialized.Value.Should().Be(value);
  }

  #endregion


  #region ExtensibleEnumConverterFactory Tests

  [Fact]
  public void CanConvert_ExtensibleEnumType_ReturnsTrue()
  {
    // Arrange
    var factory = new ExtensibleEnumConverterFactory();

    // Act & Assert
    factory.CanConvert(typeof(R2LocationHint)).Should().BeTrue();
    factory.CanConvert(typeof(R2Jurisdiction)).Should().BeTrue();
    factory.CanConvert(typeof(R2StorageClass)).Should().BeTrue();
  }

  [Fact]
  public void CanConvert_NonExtensibleEnumValueType_ReturnsFalse()
  {
    // Arrange
    var factory = new ExtensibleEnumConverterFactory();

    // Act & Assert
    factory.CanConvert(typeof(int)).Should().BeFalse();
    factory.CanConvert(typeof(DateTime)).Should().BeFalse();
    factory.CanConvert(typeof(Guid)).Should().BeFalse();
  }

  [Fact]
  public void CanConvert_ReferenceType_ReturnsFalse()
  {
    // Arrange
    var factory = new ExtensibleEnumConverterFactory();

    // Act & Assert
    factory.CanConvert(typeof(string)).Should().BeFalse();
    factory.CanConvert(typeof(object)).Should().BeFalse();
    factory.CanConvert(typeof(List<int>)).Should().BeFalse();
  }

  [Fact]
  public void CanConvert_RegularEnum_ReturnsFalse()
  {
    // Arrange
    var factory = new ExtensibleEnumConverterFactory();

    // Act & Assert
    factory.CanConvert(typeof(LifecycleConditionType)).Should().BeFalse();
  }

  [Fact]
  public void CreateConverter_ForExtensibleEnumType_ReturnsCorrectConverterType()
  {
    // Arrange
    var factory = new ExtensibleEnumConverterFactory();
    var options = new JsonSerializerOptions();

    // Act
    var converter = factory.CreateConverter(typeof(R2LocationHint), options);

    // Assert
    converter.Should().BeOfType<ExtensibleEnumConverter<R2LocationHint>>();
  }

  #endregion


  #region Model Integration Tests

  [Fact]
  public void Serialize_R2Bucket_WithExtensibleEnums_ProducesCorrectJson()
  {
    // Arrange
    var bucket = new R2Bucket(
      "test-bucket",
      new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
      R2LocationHint.EastNorthAmerica,
      R2Jurisdiction.EuropeanUnion,
      R2StorageClass.Standard
    );

    // Act
    var json = JsonSerializer.Serialize(bucket, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    doc.RootElement.GetProperty("name").GetString().Should().Be("test-bucket");
    doc.RootElement.GetProperty("location").GetString().Should().Be("enam");
    doc.RootElement.GetProperty("jurisdiction").GetString().Should().Be("eu");
    doc.RootElement.GetProperty("storage_class").GetString().Should().Be("Standard");
  }

  [Fact]
  public void Serialize_R2Bucket_WithNullExtensibleEnums_OmitsNullProperties()
  {
    // Arrange
    var bucket = new R2Bucket(
      "test-bucket",
      new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
      null,
      null,
      null
    );

    // Act
    var json = JsonSerializer.Serialize(bucket, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    doc.RootElement.GetProperty("name").GetString().Should().Be("test-bucket");
    doc.RootElement.TryGetProperty("location", out _).Should().BeFalse();
    doc.RootElement.TryGetProperty("jurisdiction", out _).Should().BeFalse();
    doc.RootElement.TryGetProperty("storage_class", out _).Should().BeFalse();
  }

  [Fact]
  public void Deserialize_R2Bucket_WithExtensibleEnums_ParsesCorrectly()
  {
    // Arrange
    const string json = """
                        {
                          "name": "my-bucket",
                          "creation_date": "2024-06-15T14:30:00Z",
                          "location": "weur",
                          "jurisdiction": "eu",
                          "storage_class": "InfrequentAccess"
                        }
                        """;

    // Act
    var bucket = JsonSerializer.Deserialize<R2Bucket>(json, _serializerOptions);

    // Assert
    bucket.Should().NotBeNull();
    bucket!.Name.Should().Be("my-bucket");
    bucket.Location.Should().Be(R2LocationHint.WestEurope);
    bucket.Jurisdiction.Should().Be(R2Jurisdiction.EuropeanUnion);
    bucket.StorageClass.Should().Be(R2StorageClass.InfrequentAccess);
  }

  [Fact]
  public void Deserialize_R2Bucket_WithUnknownExtensibleEnumValues_ParsesCorrectly()
  {
    // Arrange - simulating API returning new values not yet defined in SDK
    const string json = """
                        {
                          "name": "future-bucket",
                          "creation_date": "2025-12-01T00:00:00Z",
                          "location": "mars-colony-1",
                          "jurisdiction": "space-treaty-2050",
                          "storage_class": "CryogenicStorage"
                        }
                        """;

    // Act
    var bucket = JsonSerializer.Deserialize<R2Bucket>(json, _serializerOptions);

    // Assert
    bucket.Should().NotBeNull();
    bucket!.Location!.Value.Value.Should().Be("mars-colony-1");
    bucket.Jurisdiction!.Value.Value.Should().Be("space-treaty-2050");
    bucket.StorageClass!.Value.Value.Should().Be("CryogenicStorage");
  }

  [Fact]
  public void Deserialize_R2Bucket_WithMissingExtensibleEnums_SetsNullValues()
  {
    // Arrange
    const string json = """
                        {
                          "name": "minimal-bucket",
                          "creation_date": "2024-01-01T00:00:00Z"
                        }
                        """;

    // Act
    var bucket = JsonSerializer.Deserialize<R2Bucket>(json, _serializerOptions);

    // Assert
    bucket.Should().NotBeNull();
    bucket!.Name.Should().Be("minimal-bucket");
    bucket.Location.Should().BeNull();
    bucket.Jurisdiction.Should().BeNull();
    bucket.StorageClass.Should().BeNull();
  }

  [Fact]
  public void Serialize_StorageClassTransition_WithExtensibleEnum_ProducesCorrectJson()
  {
    // Arrange
    var transition = new StorageClassTransition(
      LifecycleCondition.AfterDays(30),
      R2StorageClass.InfrequentAccess
    );

    // Act
    var json = JsonSerializer.Serialize(transition, _serializerOptions);

    // Assert - Note: The model uses [JsonPropertyName("storageClass")] which overrides snake_case policy
    using var doc = JsonDocument.Parse(json);
    doc.RootElement.GetProperty("storageClass").GetString().Should().Be("InfrequentAccess");
  }

  #endregion


  #region Equality Tests

  [Fact]
  public void Deserialize_SameKnownValue_EqualsStaticProperty()
  {
    // Arrange
    const string json = "\"enam\"";

    // Act
    var deserialized = JsonSerializer.Deserialize<R2LocationHint>(json, _serializerOptions);

    // Assert
    deserialized.Should().Be(R2LocationHint.EastNorthAmerica);
    (deserialized == R2LocationHint.EastNorthAmerica).Should().BeTrue();
  }

  [Fact]
  public void Deserialize_CaseInsensitiveComparison_ForLocationHint()
  {
    // Arrange - Location hints use case-insensitive comparison
    const string json = "\"ENAM\"";

    // Act
    var deserialized = JsonSerializer.Deserialize<R2LocationHint>(json, _serializerOptions);

    // Assert
    // The values are different strings but should be equal due to OrdinalIgnoreCase
    deserialized.Should().Be(R2LocationHint.EastNorthAmerica);
  }

  [Fact]
  public void Deserialize_CaseSensitiveComparison_ForStorageClass()
  {
    // Arrange - Storage classes use case-sensitive comparison (Ordinal)
    const string jsonLower = "\"standard\"";
    const string jsonProper = "\"Standard\"";

    // Act
    var deserializedLower = JsonSerializer.Deserialize<R2StorageClass>(jsonLower, _serializerOptions);
    var deserializedProper = JsonSerializer.Deserialize<R2StorageClass>(jsonProper, _serializerOptions);

    // Assert
    deserializedProper.Should().Be(R2StorageClass.Standard);
    deserializedLower.Should().NotBe(R2StorageClass.Standard); // Case-sensitive, so lowercase is different
  }

  #endregion
}
