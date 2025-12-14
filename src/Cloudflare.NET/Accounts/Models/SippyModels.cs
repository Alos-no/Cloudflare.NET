namespace Cloudflare.NET.Accounts.Models;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Core.Json;


#region Sippy Provider Enums

/// <summary>Defines the supported cloud storage providers for Sippy migrations.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum SippyProvider
{
  /// <summary>Amazon Web Services S3.</summary>
  [EnumMember(Value = "aws")] Aws,

  /// <summary>Google Cloud Storage.</summary>
  [EnumMember(Value = "gcs")] Gcs,

  /// <summary>Cloudflare R2 (used for destination).</summary>
  [EnumMember(Value = "r2")] R2
}

#endregion


#region Sippy Configuration Models

/// <summary>Represents the source bucket configuration for Sippy migration from AWS S3.</summary>
/// <param name="Provider">The source provider (must be <see cref="SippyProvider.Aws" />).</param>
/// <param name="Bucket">The name of the source AWS S3 bucket.</param>
/// <param name="Region">The AWS region where the bucket is located (e.g., "us-east-1").</param>
/// <param name="AccessKeyId">The AWS access key ID for reading from the source bucket.</param>
/// <param name="SecretAccessKey">The AWS secret access key for reading from the source bucket.</param>
public record SippyAwsSource(
  [property: JsonPropertyName("provider")]
  SippyProvider Provider,
  [property: JsonPropertyName("bucket")]
  string Bucket,
  [property: JsonPropertyName("region")]
  string? Region = null,
  [property: JsonPropertyName("accessKeyId")]
  string? AccessKeyId = null,
  [property: JsonPropertyName("secretAccessKey")]
  string? SecretAccessKey = null
)
{
  /// <summary>Creates a new AWS S3 source configuration.</summary>
  /// <param name="bucket">The name of the source AWS S3 bucket.</param>
  /// <param name="region">The AWS region where the bucket is located.</param>
  /// <param name="accessKeyId">The AWS access key ID for reading from the source bucket.</param>
  /// <param name="secretAccessKey">The AWS secret access key for reading from the source bucket.</param>
  /// <returns>A new <see cref="SippyAwsSource" /> with provider set to AWS.</returns>
  public static SippyAwsSource Create(string bucket, string region, string accessKeyId, string secretAccessKey) =>
    new(SippyProvider.Aws, bucket, region, accessKeyId, secretAccessKey);
}

/// <summary>Represents the source bucket configuration for Sippy migration from Google Cloud Storage.</summary>
/// <param name="Provider">The source provider (must be <see cref="SippyProvider.Gcs" />).</param>
/// <param name="Bucket">The name of the source GCS bucket.</param>
/// <param name="ClientEmail">The service account client email for GCS access.</param>
/// <param name="PrivateKey">The service account private key for GCS access.</param>
public record SippyGcsSource(
  [property: JsonPropertyName("provider")]
  SippyProvider Provider,
  [property: JsonPropertyName("bucket")]
  string Bucket,
  [property: JsonPropertyName("clientEmail")]
  string? ClientEmail = null,
  [property: JsonPropertyName("privateKey")]
  string? PrivateKey = null
)
{
  /// <summary>Creates a new GCS source configuration.</summary>
  /// <param name="bucket">The name of the source GCS bucket.</param>
  /// <param name="clientEmail">The service account client email for GCS access.</param>
  /// <param name="privateKey">The service account private key for GCS access.</param>
  /// <returns>A new <see cref="SippyGcsSource" /> with provider set to GCS.</returns>
  public static SippyGcsSource Create(string bucket, string clientEmail, string privateKey) =>
    new(SippyProvider.Gcs, bucket, clientEmail, privateKey);
}

/// <summary>Represents the destination R2 bucket configuration for Sippy.</summary>
/// <param name="Provider">The destination provider (always <see cref="SippyProvider.R2" />).</param>
/// <param name="Bucket">The name of the destination R2 bucket.</param>
/// <param name="Account">The Cloudflare account ID.</param>
/// <param name="AccessKeyId">The R2 access key ID (if using S3 API credentials).</param>
public record SippyDestination(
  [property: JsonPropertyName("provider")]
  SippyProvider Provider,
  [property: JsonPropertyName("bucket")]
  string? Bucket = null,
  [property: JsonPropertyName("account")]
  string? Account = null,
  [property: JsonPropertyName("accessKeyId")]
  string? AccessKeyId = null
);

/// <summary>Represents the current Sippy configuration for a bucket.</summary>
/// <param name="Enabled">Whether Sippy is currently enabled for the bucket.</param>
/// <param name="Source">The source bucket configuration (AWS S3 or GCS). Null when disabled.</param>
/// <param name="Destination">The destination R2 bucket configuration. Null when disabled.</param>
public record SippyConfig(
  [property: JsonPropertyName("enabled")]
  bool Enabled,
  [property: JsonPropertyName("source")]
  SippySourceInfo? Source = null,
  [property: JsonPropertyName("destination")]
  SippyDestination? Destination = null
);

/// <summary>
///   Represents the source configuration returned by the API.
///   This is a read-only representation without sensitive credential data.
/// </summary>
/// <param name="Provider">The source provider (aws or gcs).</param>
/// <param name="Bucket">The name of the source bucket.</param>
/// <param name="BucketUrl">The full URL of the source bucket.</param>
/// <param name="Region">The region of the source bucket (for AWS S3).</param>
public record SippySourceInfo(
  [property: JsonPropertyName("provider")]
  SippyProvider? Provider = null,
  [property: JsonPropertyName("bucket")]
  string? Bucket = null,
  [property: JsonPropertyName("bucketUrl")]
  string? BucketUrl = null,
  [property: JsonPropertyName("region")]
  string? Region = null
);

#endregion


#region Sippy Request Models

/// <summary>Base interface for Sippy enable requests.</summary>
public interface IEnableSippySource
{
  /// <summary>Gets the provider type for this source configuration.</summary>
  SippyProvider Provider { get; }
}

/// <summary>Request to enable Sippy migration from AWS S3.</summary>
/// <param name="Source">The AWS S3 source bucket configuration.</param>
public record EnableSippyFromAwsRequest(
  [property: JsonPropertyName("source")]
  SippyAwsSource Source
) : EnableSippyRequest(Source);

/// <summary>Request to enable Sippy migration from Google Cloud Storage.</summary>
/// <param name="Source">The GCS source bucket configuration.</param>
public record EnableSippyFromGcsRequest(
  [property: JsonPropertyName("source")]
  SippyGcsSource Source
) : EnableSippyRequest(Source);

/// <summary>Base class for Sippy enable requests.</summary>
/// <param name="SourceConfig">The source configuration (either AWS or GCS).</param>
public abstract record EnableSippyRequest(object SourceConfig);

#endregion
