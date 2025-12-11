namespace Cloudflare.NET.Tests.UnitTests;

using System.Text.Json;
using System.Text.Json.Serialization;
using Cloudflare.NET.Core.Json;
using Cloudflare.NET.Zones.Models;
using Shared.Fixtures;

/// <summary>
///   Contains unit tests for <see cref="ZoneStatus" /> extensible enum.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class ZoneStatusTests
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
    var status = ZoneStatus.Active;

    // Act
    var json = JsonSerializer.Serialize(status, _serializerOptions);

    // Assert
    json.Should().Be("\"active\"");
  }

  [Fact]
  public void Serialize_CustomValue_WritesCorrectString()
  {
    // Arrange
    ZoneStatus customStatus = "future_status";

    // Act
    var json = JsonSerializer.Serialize(customStatus, _serializerOptions);

    // Assert
    json.Should().Be("\"future_status\"");
  }

  [Fact]
  public void Serialize_AllKnownStatuses_WriteCorrectValues()
  {
    // Arrange & Act & Assert
    JsonSerializer.Serialize(ZoneStatus.Active, _serializerOptions).Should().Be("\"active\"");
    JsonSerializer.Serialize(ZoneStatus.Pending, _serializerOptions).Should().Be("\"pending\"");
    JsonSerializer.Serialize(ZoneStatus.Initializing, _serializerOptions).Should().Be("\"initializing\"");
    JsonSerializer.Serialize(ZoneStatus.Moved, _serializerOptions).Should().Be("\"moved\"");
    JsonSerializer.Serialize(ZoneStatus.Deleted, _serializerOptions).Should().Be("\"deleted\"");
    JsonSerializer.Serialize(ZoneStatus.Deactivated, _serializerOptions).Should().Be("\"deactivated\"");
  }

  #endregion


  #region Deserialization Tests

  [Fact]
  public void Deserialize_KnownValue_ReturnsCorrectInstance()
  {
    // Arrange
    const string json = "\"active\"";

    // Act
    var status = JsonSerializer.Deserialize<ZoneStatus>(json, _serializerOptions);

    // Assert
    status.Should().Be(ZoneStatus.Active);
    status.Value.Should().Be("active");
  }

  [Fact]
  public void Deserialize_CustomValue_ReturnsInstanceWithCustomValue()
  {
    // Arrange - simulating API returning a new status not yet defined in SDK
    const string json = "\"maintenance_mode\"";

    // Act
    var status = JsonSerializer.Deserialize<ZoneStatus>(json, _serializerOptions);

    // Assert
    status.Value.Should().Be("maintenance_mode");
  }

  [Fact]
  public void Deserialize_NullValue_ReturnsDefault()
  {
    // Arrange
    const string json = "null";

    // Act
    var status = JsonSerializer.Deserialize<ZoneStatus>(json, _serializerOptions);

    // Assert
    status.Should().Be(default(ZoneStatus));
    status.Value.Should().BeEmpty();
  }

  [Fact]
  public void Deserialize_CaseInsensitive_ReturnsCorrectInstance()
  {
    // Arrange - API values are lowercase, but we should be case-insensitive for equality
    const string json = "\"ACTIVE\"";

    // Act
    var status = JsonSerializer.Deserialize<ZoneStatus>(json, _serializerOptions);

    // Assert
    status.Should().Be(ZoneStatus.Active);
  }

  #endregion


  #region Round-Trip Tests

  [Theory]
  [InlineData("active")]
  [InlineData("pending")]
  [InlineData("initializing")]
  [InlineData("moved")]
  [InlineData("deleted")]
  [InlineData("deactivated")]
  [InlineData("custom_future_status")]
  public void RoundTrip_Status_PreservesValue(string value)
  {
    // Arrange
    var original = new ZoneStatus(value);

    // Act
    var json         = JsonSerializer.Serialize(original, _serializerOptions);
    var deserialized = JsonSerializer.Deserialize<ZoneStatus>(json, _serializerOptions);

    // Assert
    deserialized.Value.Should().Be(value);
  }

  #endregion


  #region Equality Tests

  [Fact]
  public void Equals_SameKnownValue_ReturnsTrue()
  {
    // Arrange
    var status1 = ZoneStatus.Active;
    var status2 = ZoneStatus.Active;

    // Act & Assert
    status1.Should().Be(status2);
    (status1 == status2).Should().BeTrue();
    status1.Equals(status2).Should().BeTrue();
  }

  [Fact]
  public void Equals_DifferentValues_ReturnsFalse()
  {
    // Arrange
    var status1 = ZoneStatus.Active;
    var status2 = ZoneStatus.Pending;

    // Act & Assert
    status1.Should().NotBe(status2);
    (status1 != status2).Should().BeTrue();
    status1.Equals(status2).Should().BeFalse();
  }

  [Fact]
  public void Equals_CaseInsensitive_ReturnsTrue()
  {
    // Arrange
    var status1 = new ZoneStatus("active");
    var status2 = new ZoneStatus("ACTIVE");

    // Act & Assert
    status1.Should().Be(status2);
    (status1 == status2).Should().BeTrue();
  }

  [Fact]
  public void GetHashCode_SameValue_ReturnsSameHash()
  {
    // Arrange
    var status1 = ZoneStatus.Active;
    var status2 = new ZoneStatus("active");
    var status3 = new ZoneStatus("ACTIVE");

    // Act & Assert
    status1.GetHashCode().Should().Be(status2.GetHashCode());
    status1.GetHashCode().Should().Be(status3.GetHashCode());
  }

  #endregion


  #region Implicit Conversion Tests

  [Fact]
  public void ImplicitConversion_FromString_CreatesInstance()
  {
    // Arrange & Act
    ZoneStatus status = "active";

    // Assert
    status.Value.Should().Be("active");
    status.Should().Be(ZoneStatus.Active);
  }

  [Fact]
  public void ImplicitConversion_ToString_ReturnsValue()
  {
    // Arrange
    var status = ZoneStatus.Pending;

    // Act
    string value = status;

    // Assert
    value.Should().Be("pending");
  }

  #endregion


  #region Model Integration Tests

  [Fact]
  public void Deserialize_Zone_WithKnownStatus_ParsesSuccessfully()
  {
    // Arrange
    const string json = """
                        {
                          "id": "zone-123",
                          "name": "example.com",
                          "status": "active"
                        }
                        """;

    // Act
    var zone = JsonSerializer.Deserialize<Zone>(json, _serializerOptions);

    // Assert
    zone.Should().NotBeNull();
    zone!.Status.Should().Be(ZoneStatus.Active);
    zone.Status.Value.Should().Be("active");
  }

  [Fact]
  public void Deserialize_Zone_WithUnknownStatus_ParsesSuccessfully()
  {
    // Arrange - simulating API returning a new status not yet defined in SDK
    const string json = """
                        {
                          "id": "zone-456",
                          "name": "example.org",
                          "status": "maintenance_v2"
                        }
                        """;

    // Act
    var zone = JsonSerializer.Deserialize<Zone>(json, _serializerOptions);

    // Assert
    zone.Should().NotBeNull();
    zone!.Status.Value.Should().Be("maintenance_v2");
  }

  [Fact]
  public void Serialize_Zone_ProducesCorrectJson()
  {
    // Arrange
    var zone = new Zone(
      Id: "zone-123",
      Name: "example.com",
      Status: ZoneStatus.Pending,
      Account: new ZoneAccount("acct-1", "Test Account"),
      ActivatedOn: null,
      CreatedOn: DateTime.UtcNow,
      ModifiedOn: DateTime.UtcNow,
      DevelopmentMode: 0,
      NameServers: ["ns1.example.com"],
      OriginalNameServers: null,
      OriginalRegistrar: null,
      OriginalDnsHost: null,
      Owner: new ZoneOwner(null, null, null),
      Plan: new ZonePlan("free", "Free", 0, "USD", null, true, true, null, false, false),
      Meta: null,
      Paused: false,
      Permissions: null,
      Type: ZoneType.Full,
      VanityNameServers: null,
      CnameSuffix: null,
      VerificationKey: null,
      Tenant: null,
      TenantUnit: null);

    // Act
    var json = JsonSerializer.Serialize(zone, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    doc.RootElement.GetProperty("status").GetString().Should().Be("pending");
  }

  #endregion


  #region ToString Tests

  [Fact]
  public void ToString_ReturnsValue()
  {
    // Arrange
    var status = ZoneStatus.Deactivated;

    // Act
    var result = status.ToString();

    // Assert
    result.Should().Be("deactivated");
  }

  #endregion


  #region Create Factory Tests

  [Fact]
  public void Create_ReturnsCorrectInstance()
  {
    // Arrange & Act
    var status = ZoneStatus.Create("active");

    // Assert
    status.Value.Should().Be("active");
    status.Should().Be(ZoneStatus.Active);
  }

  #endregion
}
