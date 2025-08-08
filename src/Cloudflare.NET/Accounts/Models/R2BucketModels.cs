namespace Cloudflare.NET.Accounts.Models;

using System.Text.Json.Serialization;

/// <summary>Defines the request payload for creating an R2 bucket.</summary>
/// <param name="Name">The name of the bucket to create.</param>
public record CreateBucketRequest(
  [property: JsonPropertyName("name")]
  string Name
);

/// <summary>Represents the successful response from a create bucket operation.</summary>
/// <param name="Name">The name of the created bucket.</param>
/// <param name="CreationDate">The date and time the bucket was created.</param>
/// <param name="Location">The location hint for the bucket.</param>
public record CreateBucketResponse(
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
