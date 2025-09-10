namespace Cloudflare.NET.Accounts.Models;

using System.Text.Json.Serialization;

/// <summary>Defines the request payload for creating an R2 bucket.</summary>
/// <param name="Name">The name of the bucket to create.</param>
public record CreateBucketRequest(
  [property: JsonPropertyName("name")]
  string Name
);

/// <summary>Represents an R2 bucket returned by the Cloudflare API.</summary>
/// <param name="Name">The name of the created bucket.</param>
/// <param name="CreationDate">The date and time the bucket was created.</param>
/// <param name="Location">The location hint for the bucket.</param>
/// <param name="Jurisdiction">The jurisdiction of the bucket.</param>
/// <param name="StorageClass">The storage class of the bucket.</param>
public record R2Bucket(
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("creation_date")]
  DateTime CreationDate,
  [property: JsonPropertyName("location")]
  string Location,
  [property: JsonPropertyName("jurisdiction")]
  string Jurisdiction,
  [property: JsonPropertyName("storage_class")]
  string StorageClass
);

/// <summary>Defines the filtering and pagination options for listing R2 Buckets.</summary>
/// <param name="PerPage">The number of buckets to return per page.</param>
/// <param name="Cursor">The cursor for the next page of results.</param>
public record ListR2BucketsFilters(
  [property: JsonPropertyName("per_page")]
  int? PerPage = null,
  [property: JsonPropertyName("cursor")]
  string? Cursor = null
);

/// <summary>Represents the nested response structure for listing R2 buckets.</summary>
/// <param name="Buckets">The list of R2 buckets.</param>
public record ListR2BucketsResponse(
  [property: JsonPropertyName("buckets")]
  IReadOnlyList<R2Bucket> Buckets
);
