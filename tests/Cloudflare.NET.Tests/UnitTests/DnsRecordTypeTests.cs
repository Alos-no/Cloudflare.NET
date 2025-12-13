namespace Cloudflare.NET.Tests.UnitTests;

using System.Text.Json;
using System.Text.Json.Serialization;
using Cloudflare.NET.Core.Json;
using Cloudflare.NET.Dns.Models;
using Cloudflare.NET.Zones.Models;
using Shared.Fixtures;

/// <summary>
///   Contains unit tests for <see cref="DnsRecordType" /> extensible enum.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class DnsRecordTypeTests
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
    var recordType = DnsRecordType.A;

    // Act
    var json = JsonSerializer.Serialize(recordType, _serializerOptions);

    // Assert
    json.Should().Be("\"A\"");
  }

  [Fact]
  public void Serialize_CustomValue_WritesCorrectString()
  {
    // Arrange
    DnsRecordType customType = "FUTURE_RECORD_TYPE";

    // Act
    var json = JsonSerializer.Serialize(customType, _serializerOptions);

    // Assert
    json.Should().Be("\"FUTURE_RECORD_TYPE\"");
  }

  [Fact]
  public void Serialize_CommonRecordTypes_WriteCorrectValues()
  {
    // Arrange & Act & Assert
    JsonSerializer.Serialize(DnsRecordType.A, _serializerOptions).Should().Be("\"A\"");
    JsonSerializer.Serialize(DnsRecordType.AAAA, _serializerOptions).Should().Be("\"AAAA\"");
    JsonSerializer.Serialize(DnsRecordType.CNAME, _serializerOptions).Should().Be("\"CNAME\"");
    JsonSerializer.Serialize(DnsRecordType.MX, _serializerOptions).Should().Be("\"MX\"");
    JsonSerializer.Serialize(DnsRecordType.TXT, _serializerOptions).Should().Be("\"TXT\"");
    JsonSerializer.Serialize(DnsRecordType.NS, _serializerOptions).Should().Be("\"NS\"");
    JsonSerializer.Serialize(DnsRecordType.SOA, _serializerOptions).Should().Be("\"SOA\"");
    JsonSerializer.Serialize(DnsRecordType.PTR, _serializerOptions).Should().Be("\"PTR\"");
  }

  [Fact]
  public void Serialize_ServiceRecordTypes_WriteCorrectValues()
  {
    // Arrange & Act & Assert
    JsonSerializer.Serialize(DnsRecordType.SRV, _serializerOptions).Should().Be("\"SRV\"");
    JsonSerializer.Serialize(DnsRecordType.HTTPS, _serializerOptions).Should().Be("\"HTTPS\"");
    JsonSerializer.Serialize(DnsRecordType.SVCB, _serializerOptions).Should().Be("\"SVCB\"");
    JsonSerializer.Serialize(DnsRecordType.URI, _serializerOptions).Should().Be("\"URI\"");
    JsonSerializer.Serialize(DnsRecordType.NAPTR, _serializerOptions).Should().Be("\"NAPTR\"");
  }

  [Fact]
  public void Serialize_SecurityRecordTypes_WriteCorrectValues()
  {
    // Arrange & Act & Assert
    JsonSerializer.Serialize(DnsRecordType.CAA, _serializerOptions).Should().Be("\"CAA\"");
    JsonSerializer.Serialize(DnsRecordType.DS, _serializerOptions).Should().Be("\"DS\"");
    JsonSerializer.Serialize(DnsRecordType.DNSKEY, _serializerOptions).Should().Be("\"DNSKEY\"");
    JsonSerializer.Serialize(DnsRecordType.TLSA, _serializerOptions).Should().Be("\"TLSA\"");
    JsonSerializer.Serialize(DnsRecordType.SSHFP, _serializerOptions).Should().Be("\"SSHFP\"");
    JsonSerializer.Serialize(DnsRecordType.CERT, _serializerOptions).Should().Be("\"CERT\"");
    JsonSerializer.Serialize(DnsRecordType.SMIMEA, _serializerOptions).Should().Be("\"SMIMEA\"");
  }

  #endregion


  #region Deserialization Tests

  [Fact]
  public void Deserialize_KnownValue_ReturnsCorrectInstance()
  {
    // Arrange
    const string json = "\"CNAME\"";

    // Act
    var recordType = JsonSerializer.Deserialize<DnsRecordType>(json, _serializerOptions);

    // Assert
    recordType.Should().Be(DnsRecordType.CNAME);
    recordType.Value.Should().Be("CNAME");
  }

  [Fact]
  public void Deserialize_CustomValue_ReturnsInstanceWithCustomValue()
  {
    // Arrange - simulating API returning a new record type not yet defined in SDK
    const string json = "\"NEW_RECORD_TYPE_2030\"";

    // Act
    var recordType = JsonSerializer.Deserialize<DnsRecordType>(json, _serializerOptions);

    // Assert
    recordType.Value.Should().Be("NEW_RECORD_TYPE_2030");
  }

  [Fact]
  public void Deserialize_NullValue_ReturnsDefault()
  {
    // Arrange
    const string json = "null";

    // Act
    var recordType = JsonSerializer.Deserialize<DnsRecordType>(json, _serializerOptions);

    // Assert
    recordType.Should().Be(default(DnsRecordType));
    recordType.Value.Should().BeEmpty();
  }

  [Fact]
  public void Deserialize_CaseInsensitive_ReturnsCorrectInstance()
  {
    // Arrange - DNS record types are uppercase, but we should be case-insensitive for equality
    const string json = "\"cname\"";

    // Act
    var recordType = JsonSerializer.Deserialize<DnsRecordType>(json, _serializerOptions);

    // Assert
    recordType.Should().Be(DnsRecordType.CNAME);
  }

  #endregion


  #region Round-Trip Tests

  [Theory]
  [InlineData("A")]
  [InlineData("AAAA")]
  [InlineData("CNAME")]
  [InlineData("MX")]
  [InlineData("TXT")]
  [InlineData("NS")]
  [InlineData("SOA")]
  [InlineData("PTR")]
  [InlineData("SRV")]
  [InlineData("HTTPS")]
  [InlineData("SVCB")]
  [InlineData("URI")]
  [InlineData("NAPTR")]
  [InlineData("CAA")]
  [InlineData("DS")]
  [InlineData("DNSKEY")]
  [InlineData("TLSA")]
  [InlineData("SSHFP")]
  [InlineData("CERT")]
  [InlineData("SMIMEA")]
  [InlineData("CUSTOM_FUTURE_TYPE")]
  public void RoundTrip_RecordType_PreservesValue(string value)
  {
    // Arrange
    var original = new DnsRecordType(value);

    // Act
    var json         = JsonSerializer.Serialize(original, _serializerOptions);
    var deserialized = JsonSerializer.Deserialize<DnsRecordType>(json, _serializerOptions);

    // Assert
    deserialized.Value.Should().Be(value);
  }

  #endregion


  #region Equality Tests

  [Fact]
  public void Equals_SameKnownValue_ReturnsTrue()
  {
    // Arrange
    var type1 = DnsRecordType.A;
    var type2 = DnsRecordType.A;

    // Act & Assert
    type1.Should().Be(type2);
    (type1 == type2).Should().BeTrue();
    type1.Equals(type2).Should().BeTrue();
  }

  [Fact]
  public void Equals_DifferentValues_ReturnsFalse()
  {
    // Arrange
    var type1 = DnsRecordType.A;
    var type2 = DnsRecordType.AAAA;

    // Act & Assert
    type1.Should().NotBe(type2);
    (type1 != type2).Should().BeTrue();
    type1.Equals(type2).Should().BeFalse();
  }

  [Fact]
  public void Equals_CaseInsensitive_ReturnsTrue()
  {
    // Arrange
    var type1 = new DnsRecordType("CNAME");
    var type2 = new DnsRecordType("cname");

    // Act & Assert
    type1.Should().Be(type2);
    (type1 == type2).Should().BeTrue();
  }

  [Fact]
  public void GetHashCode_SameValue_ReturnsSameHash()
  {
    // Arrange
    var type1 = DnsRecordType.CNAME;
    var type2 = new DnsRecordType("CNAME");
    var type3 = new DnsRecordType("cname");

    // Act & Assert
    type1.GetHashCode().Should().Be(type2.GetHashCode());
    type1.GetHashCode().Should().Be(type3.GetHashCode());
  }

  #endregion


  #region Implicit Conversion Tests

  [Fact]
  public void ImplicitConversion_FromString_CreatesInstance()
  {
    // Arrange & Act
    DnsRecordType recordType = "A";

    // Assert
    recordType.Value.Should().Be("A");
    recordType.Should().Be(DnsRecordType.A);
  }

  [Fact]
  public void ImplicitConversion_ToString_ReturnsValue()
  {
    // Arrange
    var recordType = DnsRecordType.TXT;

    // Act
    string value = recordType;

    // Assert
    value.Should().Be("TXT");
  }

  #endregion


  #region Model Integration Tests

  [Fact]
  public void Serialize_CreateDnsRecordRequest_ProducesCorrectJson()
  {
    // Arrange
    var request = new CreateDnsRecordRequest(
      DnsRecordType.CNAME,
      "www.example.com",
      "example.com",
      300,
      true
    );

    // Act
    var json = JsonSerializer.Serialize(request, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    doc.RootElement.GetProperty("type").GetString().Should().Be("CNAME");
    doc.RootElement.GetProperty("name").GetString().Should().Be("www.example.com");
    doc.RootElement.GetProperty("content").GetString().Should().Be("example.com");
  }

  [Fact]
  public void Deserialize_DnsRecord_WithKnownType_ParsesSuccessfully()
  {
    // Arrange
    const string json = """
                        {
                          "id": "dns-record-123",
                          "name": "example.com",
                          "type": "A"
                        }
                        """;

    // Act
    var record = JsonSerializer.Deserialize<DnsRecord>(json, _serializerOptions);

    // Assert
    record.Should().NotBeNull();
    record!.Type.Should().Be(DnsRecordType.A);
    record.Type.Value.Should().Be("A");
  }

  [Fact]
  public void Deserialize_DnsRecord_WithUnknownType_ParsesSuccessfully()
  {
    // Arrange - simulating API returning a new record type not yet defined in SDK
    const string json = """
                        {
                          "id": "dns-record-456",
                          "name": "example.com",
                          "type": "QUANTUM_DNS_V3"
                        }
                        """;

    // Act
    var record = JsonSerializer.Deserialize<DnsRecord>(json, _serializerOptions);

    // Assert
    record.Should().NotBeNull();
    record!.Type.Value.Should().Be("QUANTUM_DNS_V3");
  }

  [Fact]
  public void Serialize_DnsRecord_ProducesCorrectJson()
  {
    // Arrange
    var record = new DnsRecord(
      Id: "dns-123",
      Name: "api.example.com",
      Type: DnsRecordType.AAAA,
      Content: "2001:db8::1",
      Proxied: false,
      Proxiable: true,
      Ttl: 1,
      CreatedOn: DateTime.UtcNow,
      ModifiedOn: DateTime.UtcNow
    );

    // Act
    var json = JsonSerializer.Serialize(record, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    doc.RootElement.GetProperty("type").GetString().Should().Be("AAAA");
  }

  #endregion


  #region ToString Tests

  [Fact]
  public void ToString_ReturnsValue()
  {
    // Arrange
    var recordType = DnsRecordType.MX;

    // Act
    var result = recordType.ToString();

    // Assert
    result.Should().Be("MX");
  }

  #endregion


  #region Create Factory Tests

  [Fact]
  public void Create_ReturnsCorrectInstance()
  {
    // Arrange & Act
    var recordType = DnsRecordType.Create("TXT");

    // Assert
    recordType.Value.Should().Be("TXT");
    recordType.Should().Be(DnsRecordType.TXT);
  }

  #endregion
}
