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
  string? Location,
  [property: JsonPropertyName("jurisdiction")]
  string? Jurisdiction,
  [property: JsonPropertyName("storage_class")]
  string? StorageClass
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

/// <summary>Defines the allowed HTTP methods, origins, and headers for a CORS rule.</summary>
/// <param name="Methods">The HTTP methods allowed for CORS requests (e.g., GET, PUT, POST, DELETE, HEAD).</param>
/// <param name="Origins">The origins allowed to make CORS requests (e.g., "https://example.com", "*").</param>
/// <param name="Headers">The headers that are allowed in CORS requests (e.g., "Content-Type", "Authorization").</param>
public record CorsAllowed(
  [property: JsonPropertyName("methods")]
  IReadOnlyList<string> Methods,
  [property: JsonPropertyName("origins")]
  IReadOnlyList<string> Origins,
  [property: JsonPropertyName("headers")]
  IReadOnlyList<string>? Headers = null
);

/// <summary>Represents a single CORS rule for an R2 bucket.</summary>
/// <param name="Allowed">The allowed methods, origins, and headers for this rule.</param>
/// <param name="Id">An optional identifier for this CORS rule.</param>
/// <param name="ExposeHeaders">Headers that browsers are allowed to access in the response.</param>
/// <param name="MaxAgeSeconds">How long the browser should cache the preflight response, in seconds.</param>
public record CorsRule(
  [property: JsonPropertyName("allowed")]
  CorsAllowed Allowed,
  [property: JsonPropertyName("id")]
  string? Id = null,
  [property: JsonPropertyName("exposeHeaders")]
  IReadOnlyList<string>? ExposeHeaders = null,
  [property: JsonPropertyName("maxAgeSeconds")]
  int? MaxAgeSeconds = null
);

/// <summary>Represents the CORS policy for an R2 bucket.</summary>
/// <param name="Rules">The list of CORS rules applied to the bucket.</param>
public record BucketCorsPolicy(
  [property: JsonPropertyName("rules")]
  IReadOnlyList<CorsRule> Rules
);
