namespace Cloudflare.NET.Accounts.Models;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Core.Json;


#region Temporary Credentials Enums

/// <summary>Defines the permission levels for temporary R2 credentials.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum TempCredentialPermission
{
  /// <summary>Read-only access to objects.</summary>
  [EnumMember(Value = "object-read-only")] ObjectReadOnly,

  /// <summary>Write-only access to objects.</summary>
  [EnumMember(Value = "object-write-only")] ObjectWriteOnly,

  /// <summary>Read and write access to objects.</summary>
  [EnumMember(Value = "object-read-write")] ObjectReadWrite,

  /// <summary>Admin-level read-only access.</summary>
  [EnumMember(Value = "admin-read-only")] AdminReadOnly,

  /// <summary>Admin-level read and write access.</summary>
  [EnumMember(Value = "admin-read-write")] AdminReadWrite
}

#endregion


#region Temporary Credentials Request Models

/// <summary>Defines the request payload for creating temporary R2 access credentials.</summary>
/// <param name="Bucket">The name of the R2 bucket to scope the credentials to.</param>
/// <param name="ParentAccessKeyId">The parent access key ID to derive the temporary credentials from.</param>
/// <param name="Permission">The permission level for the temporary credentials.</param>
/// <param name="TtlSeconds">The time-to-live for the credentials in seconds.</param>
/// <param name="Objects">
///   Optional list of specific object keys to scope access to.
///   If not specified, credentials have access to all objects matching the bucket/prefix scope.
/// </param>
/// <param name="Prefixes">
///   Optional list of object key prefixes to scope access to.
///   If not specified, credentials have access to all objects in the bucket.
/// </param>
public record CreateTempCredentialsRequest(
  [property: JsonPropertyName("bucket")]
  string Bucket,
  [property: JsonPropertyName("parentAccessKeyId")]
  string ParentAccessKeyId,
  [property: JsonPropertyName("permission")]
  TempCredentialPermission Permission,
  [property: JsonPropertyName("ttlSeconds")]
  int TtlSeconds,
  [property: JsonPropertyName("objects")]
  IReadOnlyList<string>? Objects = null,
  [property: JsonPropertyName("prefixes")]
  IReadOnlyList<string>? Prefixes = null
);

#endregion


#region Temporary Credentials Response Models

/// <summary>Represents the temporary access credentials returned by the API.</summary>
/// <param name="AccessKeyId">The temporary access key identifier for S3-compatible API requests.</param>
/// <param name="SecretAccessKey">The temporary secret access key for S3-compatible API requests.</param>
/// <param name="SessionToken">The session token to include with S3-compatible API requests.</param>
public record TempCredentials(
  [property: JsonPropertyName("accessKeyId")]
  string AccessKeyId,
  [property: JsonPropertyName("secretAccessKey")]
  string SecretAccessKey,
  [property: JsonPropertyName("sessionToken")]
  string SessionToken
);

#endregion
