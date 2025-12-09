namespace Cloudflare.NET.Tests.UnitTests.Json;

using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Fixtures;
using Zones.CustomHostnames;
using Zones.CustomHostnames.Models;

/// <summary>
///   Contains unit tests for verifying the JSON serialization of CustomHostname models (SslConfiguration, SslSettings).
///   These tests ensure the SDK produces JSON that matches the Cloudflare API expectations.
/// </summary>
[Trait("Category", TestConstants.TestCategories.Unit)]
public class CustomHostnameModelSerializationTests
{
  #region Properties & Fields - Non-Public

  /// <summary>
  ///   JSON serializer options that match the configuration used in CustomHostnamesApi.
  ///   This must be kept in sync with the actual implementation.
  /// </summary>
  private readonly JsonSerializerOptions _serializerOptions = new()
  {
    PropertyNamingPolicy   = JsonNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  #endregion


  #region SslConfiguration Serialization Tests

  /// <summary>
  ///   Verifies that SslConfiguration serializes correctly with minimal properties.
  ///   Only Method and Type are required; Settings and other properties are optional.
  /// </summary>
  [Fact]
  public void Serialize_SslConfiguration_Minimal_OmitsOptionalProperties()
  {
    // Arrange - Minimal SSL configuration
    var sslConfig = new SslConfiguration(DcvMethod.Http, CertificateType.Dv);

    // Act
    var json = JsonSerializer.Serialize(sslConfig, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("method").GetString().Should().Be("http");
    root.GetProperty("type").GetString().Should().Be("dv");
    root.TryGetProperty("settings", out _).Should().BeFalse("null Settings should be omitted");
    root.TryGetProperty("bundle_method", out _).Should().BeFalse("null BundleMethod should be omitted");
    root.TryGetProperty("wildcard", out _).Should().BeFalse("null Wildcard should be omitted");
    root.TryGetProperty("certificate_authority", out _).Should().BeFalse("null CertificateAuthority should be omitted");
  }

  /// <summary>
  ///   Verifies that SslConfiguration with empty SslSettings serializes correctly.
  ///   Tests the case where Settings is provided but has all null properties.
  /// </summary>
  [Fact]
  public void Serialize_SslConfiguration_WithEmptySettings_IncludesEmptySettingsObject()
  {
    // Arrange - SSL configuration with empty settings object
    var sslConfig = new SslConfiguration(
      DcvMethod.Txt,
      CertificateType.Dv,
      Settings: new SslSettings() // Empty settings - all null
    );

    // Act
    var json = JsonSerializer.Serialize(sslConfig, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("method").GetString().Should().Be("txt");
    root.GetProperty("type").GetString().Should().Be("dv");

    // With WhenWritingNull, an empty SslSettings object will serialize as {}
    var settings = root.GetProperty("settings");
    settings.TryGetProperty("http2", out _).Should().BeFalse("null Http2 should be omitted");
    settings.TryGetProperty("min_tls_version", out _).Should().BeFalse("null MinTlsVersion should be omitted");
    settings.TryGetProperty("tls_1_3", out _).Should().BeFalse("null Tls13 should be omitted");
    settings.TryGetProperty("ciphers", out _).Should().BeFalse("null Ciphers should be omitted");
    settings.TryGetProperty("early_hints", out _).Should().BeFalse("null EarlyHints should be omitted");
  }

  /// <summary>
  ///   Verifies that SslConfiguration with partial SslSettings serializes correctly.
  ///   Only the set properties should be included in the JSON.
  /// </summary>
  [Fact]
  public void Serialize_SslConfiguration_WithPartialSettings_IncludesOnlySetProperties()
  {
    // Arrange - SSL configuration with partial settings
    var sslConfig = new SslConfiguration(
      DcvMethod.Http,
      CertificateType.Dv,
      Settings: new SslSettings(
        Http2: SslToggle.On,
        MinTlsVersion: MinTlsVersion.Tls12
        // Tls13, Ciphers, EarlyHints are null
      )
    );

    // Act
    var json = JsonSerializer.Serialize(sslConfig, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    var settings = root.GetProperty("settings");
    settings.GetProperty("http2").GetString().Should().Be("on");
    settings.GetProperty("min_tls_version").GetString().Should().Be("1.2");
    settings.TryGetProperty("tls_1_3", out _).Should().BeFalse("null Tls13 should be omitted");
    settings.TryGetProperty("ciphers", out _).Should().BeFalse("null Ciphers should be omitted");
    settings.TryGetProperty("early_hints", out _).Should().BeFalse("null EarlyHints should be omitted");
  }

  /// <summary>
  ///   Verifies that SslConfiguration with all properties serializes correctly.
  /// </summary>
  [Fact]
  public void Serialize_SslConfiguration_Full_IncludesAllProperties()
  {
    // Arrange - Full SSL configuration
    var sslConfig = new SslConfiguration(
      DcvMethod.Txt,
      CertificateType.Dv,
      Settings: new SslSettings(
        Http2: SslToggle.On,
        MinTlsVersion: MinTlsVersion.Tls12,
        Tls13: SslToggle.On,
        Ciphers: ["ECDHE-RSA-AES128-GCM-SHA256"],
        EarlyHints: SslToggle.Off
      ),
      BundleMethod: BundleMethod.Ubiquitous,
      Wildcard: true,
      CertificateAuthority: CertificateAuthority.Digicert
    );

    // Act
    var json = JsonSerializer.Serialize(sslConfig, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("method").GetString().Should().Be("txt");
    root.GetProperty("type").GetString().Should().Be("dv");
    root.GetProperty("bundle_method").GetString().Should().Be("ubiquitous");
    root.GetProperty("wildcard").GetBoolean().Should().BeTrue();
    root.GetProperty("certificate_authority").GetString().Should().Be("digicert");

    var settings = root.GetProperty("settings");
    settings.GetProperty("http2").GetString().Should().Be("on");
    settings.GetProperty("min_tls_version").GetString().Should().Be("1.2");
    settings.GetProperty("tls_1_3").GetString().Should().Be("on");
    settings.GetProperty("ciphers").GetArrayLength().Should().Be(1);
    settings.GetProperty("early_hints").GetString().Should().Be("off");
  }

  #endregion


  #region CreateCustomHostnameRequest Serialization Tests

  /// <summary>
  ///   Verifies that CreateCustomHostnameRequest serializes correctly with minimal properties.
  /// </summary>
  [Fact]
  public void Serialize_CreateCustomHostnameRequest_Minimal_OmitsOptionalProperties()
  {
    // Arrange - Minimal custom hostname creation request
    var request = new CreateCustomHostnameRequest(
      "app.customer.com",
      new SslConfiguration(DcvMethod.Http, CertificateType.Dv)
    );

    // Act
    var json = JsonSerializer.Serialize(request, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("hostname").GetString().Should().Be("app.customer.com");
    root.GetProperty("ssl").Should().NotBeNull();
    root.TryGetProperty("custom_metadata", out _).Should().BeFalse("null CustomMetadata should be omitted");
    root.TryGetProperty("custom_origin_server", out _).Should().BeFalse("null CustomOriginServer should be omitted");
    root.TryGetProperty("custom_origin_sni", out _).Should().BeFalse("null CustomOriginSni should be omitted");
  }

  /// <summary>
  ///   Verifies that CreateCustomHostnameRequest with custom origin settings serializes correctly.
  /// </summary>
  [Fact]
  public void Serialize_CreateCustomHostnameRequest_WithCustomOrigin_IncludesOriginProperties()
  {
    // Arrange
    var request = new CreateCustomHostnameRequest(
      "app.customer.com",
      new SslConfiguration(DcvMethod.Http, CertificateType.Dv),
      CustomOriginServer: "origin.myservice.com",
      CustomOriginSni: CustomHostnameConstants.Sni.UseRequestHostHeader
    );

    // Act
    var json = JsonSerializer.Serialize(request, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("custom_origin_server").GetString().Should().Be("origin.myservice.com");
    root.GetProperty("custom_origin_sni").GetString().Should().Be(":request_host_header:");
  }

  #endregion


  #region UpdateCustomHostnameRequest Serialization Tests

  /// <summary>
  ///   Verifies that UpdateCustomHostnameRequest with all null properties serializes as an empty object.
  ///   This is a diagnostic test to understand the serialization behavior.
  /// </summary>
  [Fact]
  public void Serialize_UpdateCustomHostnameRequest_AllNull_ProducesEmptyObject()
  {
    // Arrange - All properties null
    var request = new UpdateCustomHostnameRequest();

    // Act
    var json = JsonSerializer.Serialize(request, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    // An empty UpdateCustomHostnameRequest serializes as {}
    root.EnumerateObject().Should().BeEmpty("all null properties should be omitted");
  }

  /// <summary>
  ///   Verifies that UpdateCustomHostnameRequest with only SSL configuration serializes correctly.
  /// </summary>
  [Fact]
  public void Serialize_UpdateCustomHostnameRequest_OnlySsl_OmitsOtherProperties()
  {
    // Arrange
    var request = new UpdateCustomHostnameRequest(
      Ssl: new SslConfiguration(DcvMethod.Txt, CertificateType.Dv)
    );

    // Act
    var json = JsonSerializer.Serialize(request, _serializerOptions);

    // Assert
    using var doc = JsonDocument.Parse(json);
    var root = doc.RootElement;

    root.GetProperty("ssl").GetProperty("method").GetString().Should().Be("txt");
    root.TryGetProperty("custom_metadata", out _).Should().BeFalse();
    root.TryGetProperty("custom_origin_server", out _).Should().BeFalse();
    root.TryGetProperty("custom_origin_sni", out _).Should().BeFalse();
  }

  #endregion
}
