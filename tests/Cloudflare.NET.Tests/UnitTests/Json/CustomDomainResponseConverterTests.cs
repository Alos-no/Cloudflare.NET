namespace Cloudflare.NET.Tests.UnitTests.Json;

using System.Text.Json;
using Accounts.Models;
using Shared.Fixtures;

/// <summary>
///   Contains unit tests for <see cref="CustomDomainResponseConverter" />, the converter behind
///   <see cref="CustomDomainResponse" />. The Cloudflare API returns the custom domain 'status' field in two shapes:
///   a nested object (<c>{ "ownership": ..., "ssl": ... }</c>) when querying an existing domain, and a plain string
///   (or nothing at all) on the attach and update responses. These tests pin down how each shape maps onto
///   <see cref="CustomDomainResponse.Status" /> and <see cref="CustomDomainResponse.SslStatus" />.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class CustomDomainResponseConverterTests
{
  #region Properties & Fields - Non-Public

  /// <summary>
  ///   JSON serializer options matching the configuration used by the API resources. The converter is picked up from
  ///   the <see cref="System.Text.Json.Serialization.JsonConverterAttribute" /> on the record, so no explicit
  ///   registration is required here.
  /// </summary>
  private readonly JsonSerializerOptions _serializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
  };

  #endregion


  #region Deserialization Tests

  /// <summary>
  ///   Verifies that the documented GET response shape (a nested status object) populates both the ownership status
  ///   and the certificate status, so a caller can wait for <c>ssl</c> to become "active" instead of stopping at
  ///   <c>ownership</c>.
  /// </summary>
  [Fact]
  public void Deserialize_NestedStatusObject_PopulatesOwnershipAndSslStatus()
  {
    // Arrange
    const string json =
      """
      {
        "domain": "files.example.com",
        "edgeHostname": "files.example.com.cdn.cloudflare.net",
        "status": {
          "ownership": "active",
          "ssl": "pending"
        }
      }
      """;

    // Act
    var result = JsonSerializer.Deserialize<CustomDomainResponse>(json, _serializerOptions);

    // Assert
    result.Should().NotBeNull();
    result!.Domain.Should().Be("files.example.com");
    result.EdgeHostname.Should().Be("files.example.com.cdn.cloudflare.net");
    result.Status.Should().Be("active", "the ownership value is the primary status");
    result.SslStatus.Should().Be("pending", "the nested ssl value must be surfaced unchanged");
  }

  /// <summary>
  ///   Verifies that a response without any 'status' field (as a freshly attached domain may return) keeps the
  ///   existing "pending_validation" default for the ownership status and leaves the certificate status null, since
  ///   the API reported nothing about the certificate.
  /// </summary>
  [Fact]
  public void Deserialize_MissingStatus_DefaultsOwnershipAndLeavesSslStatusNull()
  {
    // Arrange
    const string json =
      """
      {
        "domain": "files.example.com"
      }
      """;

    // Act
    var result = JsonSerializer.Deserialize<CustomDomainResponse>(json, _serializerOptions);

    // Assert
    result.Should().NotBeNull();
    result!.Domain.Should().Be("files.example.com");
    result.EdgeHostname.Should().BeNull();
    result.Status.Should().Be("pending_validation", "the converter's default for a missing status must be preserved");
    result.SslStatus.Should().BeNull("no nested status object means no certificate status was reported");
  }

  /// <summary>
  ///   Verifies that the plain-string status shape (attach and update responses) is stored as the ownership status
  ///   and does not invent a certificate status.
  /// </summary>
  [Fact]
  public void Deserialize_StringStatus_KeepsStatusAndLeavesSslStatusNull()
  {
    // Arrange
    const string json =
      """
      {
        "domain": "files.example.com",
        "status": "active"
      }
      """;

    // Act
    var result = JsonSerializer.Deserialize<CustomDomainResponse>(json, _serializerOptions);

    // Assert
    result.Should().NotBeNull();
    result!.Status.Should().Be("active");
    result.SslStatus.Should().BeNull("a plain-string status carries no certificate information");
  }

  /// <summary>
  ///   Verifies that unknown properties inside the response are skipped, so the converter stays robust against API
  ///   additions (e.g. 'enabled', 'minTLS', 'zoneId', 'ciphers') while still reading the nested status.
  /// </summary>
  [Fact]
  public void Deserialize_UnknownProperties_AreSkippedAndStatusStillRead()
  {
    // Arrange
    const string json =
      """
      {
        "domain": "files.example.com",
        "enabled": true,
        "minTLS": "1.2",
        "zoneId": "zone-123",
        "ciphers": ["ECDHE-ECDSA-AES128-GCM-SHA256"],
        "status": {
          "ownership": "pending",
          "ssl": "initializing"
        },
        "zoneName": "example.com"
      }
      """;

    // Act
    var result = JsonSerializer.Deserialize<CustomDomainResponse>(json, _serializerOptions);

    // Assert
    result.Should().NotBeNull();
    result!.Status.Should().Be("pending");
    result.SslStatus.Should().Be("initializing");
  }

  #endregion


  #region Serialization Tests

  /// <summary>
  ///   Verifies that serializing a response writes the certificate status under the snake_case name and omits it
  ///   when null, mirroring how the converter already treats the edge hostname.
  /// </summary>
  [Fact]
  public void Serialize_WritesSslStatusOnlyWhenPresent()
  {
    // Arrange
    var withSsl    = new CustomDomainResponse("files.example.com", null, "active", "active");
    var withoutSsl = new CustomDomainResponse("files.example.com", null, "pending_validation");

    // Act
    var jsonWithSsl    = JsonSerializer.Serialize(withSsl, _serializerOptions);
    var jsonWithoutSsl = JsonSerializer.Serialize(withoutSsl, _serializerOptions);

    // Assert
    using var docWithSsl = JsonDocument.Parse(jsonWithSsl);
    docWithSsl.RootElement.GetProperty("status").GetString().Should().Be("active");
    docWithSsl.RootElement.GetProperty("ssl_status").GetString().Should().Be("active");

    using var docWithoutSsl = JsonDocument.Parse(jsonWithoutSsl);
    docWithoutSsl.RootElement.GetProperty("status").GetString().Should().Be("pending_validation");
    docWithoutSsl.RootElement.TryGetProperty("ssl_status", out _).Should().BeFalse("a null SslStatus must be omitted");
  }

  #endregion
}
