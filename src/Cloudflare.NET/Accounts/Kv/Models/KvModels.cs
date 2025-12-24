namespace Cloudflare.NET.Accounts.Kv.Models;

using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Json;
using Security.Firewall.Models;

#region Namespace Types

/// <summary>Represents a Workers KV namespace.</summary>
/// <param name="Id">The unique identifier for the namespace.</param>
/// <param name="Title">The human-readable name of the namespace.</param>
/// <param name="SupportsUrlEncoding">Whether the namespace supports URL-encoded keys.</param>
public record KvNamespace(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("title")]
  string Title,

  [property: JsonPropertyName("supports_url_encoding")]
  bool? SupportsUrlEncoding = null
);

/// <summary>Filters for listing KV namespaces.</summary>
/// <param name="Page">Page number of results to return (1-based).</param>
/// <param name="PerPage">Number of results per page (max 100).</param>
/// <param name="Order">Field to order results by.</param>
/// <param name="Direction">Sort direction (asc or desc).</param>
public record ListKvNamespacesFilters(
  int? Page = null,
  int? PerPage = null,
  KvNamespaceOrderField? Order = null,
  ListOrderDirection? Direction = null
);

/// <summary>Fields available for ordering KV namespace lists.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum KvNamespaceOrderField
{
  /// <summary>Order by namespace ID.</summary>
  [EnumMember(Value = "id")]
  Id,

  /// <summary>Order by namespace title.</summary>
  [EnumMember(Value = "title")]
  Title
}

#endregion


#region Key Types

/// <summary>Represents a key in a KV namespace (from list keys response).</summary>
/// <param name="Name">The key name (max 512 bytes).</param>
/// <param name="Expiration">Unix timestamp when the key expires (if set).</param>
/// <param name="Metadata">Optional JSON metadata associated with the key.</param>
public record KvKey(
  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("expiration")]
  long? Expiration = null,

  [property: JsonPropertyName("metadata")]
  JsonElement? Metadata = null
);

/// <summary>Filters for listing keys in a KV namespace.</summary>
/// <param name="Prefix">Filter keys by prefix.</param>
/// <param name="Limit">Maximum number of keys to return (max 1000).</param>
/// <param name="Cursor">Cursor for pagination (from previous result_info.cursor).</param>
public record ListKvKeysFilters(
  string? Prefix = null,
  int? Limit = null,
  string? Cursor = null
);

#endregion


#region Value Read Types

/// <summary>Result of reading a key-value pair, including expiration from HTTP header.</summary>
/// <param name="Value">The raw value bytes.</param>
/// <param name="Expiration">Unix timestamp when the key expires (from 'expiration' header), or null if no expiration.</param>
/// <remarks>
///   Single key reads return raw value directly (not JSON envelope).
///   The expiration is provided in the HTTP 'expiration' response header.
/// </remarks>
public record KvValueResult(
  byte[] Value,
  long? Expiration = null
);

/// <summary>Result of reading a key-value pair as string, including expiration.</summary>
/// <param name="Value">The value as a string.</param>
/// <param name="Expiration">Unix timestamp when the key expires (from 'expiration' header), or null if no expiration.</param>
public record KvStringValueResult(
  string Value,
  long? Expiration = null
);

#endregion


#region Value Write & Bulk Types

/// <summary>Options for writing a key-value pair.</summary>
/// <param name="Expiration">Unix timestamp when the key should expire.</param>
/// <param name="ExpirationTtl">Time-to-live in seconds (min 60). Ignored if Expiration is set.</param>
/// <param name="Metadata">Optional JSON metadata to associate with the key (max 1024 bytes serialized).</param>
public record KvWriteOptions(
  long? Expiration = null,
  int? ExpirationTtl = null,
  JsonElement? Metadata = null
);

/// <summary>A key-value pair for bulk write operations.</summary>
/// <param name="Key">The key name (max 512 bytes).</param>
/// <param name="Value">The value to store (base64 encoded for binary).</param>
/// <param name="Base64">Whether the value is base64 encoded.</param>
/// <param name="Expiration">Unix timestamp when the key should expire.</param>
/// <param name="ExpirationTtl">Time-to-live in seconds (min 60).</param>
/// <param name="Metadata">Optional JSON metadata (max 1024 bytes serialized).</param>
public record KvBulkWriteItem(
  [property: JsonPropertyName("key")]
  string Key,

  [property: JsonPropertyName("value")]
  string Value,

  [property: JsonPropertyName("base64")]
  bool? Base64 = null,

  [property: JsonPropertyName("expiration")]
  long? Expiration = null,

  [property: JsonPropertyName("expiration_ttl")]
  int? ExpirationTtl = null,

  [property: JsonPropertyName("metadata")]
  JsonElement? Metadata = null
);

/// <summary>Result of a bulk write operation.</summary>
/// <param name="SuccessfulKeyCount">Number of keys successfully written.</param>
/// <param name="UnsuccessfulKeys">Keys that failed to write (should be retried).</param>
public record KvBulkWriteResult(
  [property: JsonPropertyName("successful_key_count")]
  int SuccessfulKeyCount,

  [property: JsonPropertyName("unsuccessful_keys")]
  IReadOnlyList<string>? UnsuccessfulKeys = null
);

/// <summary>Result of a bulk delete operation.</summary>
/// <param name="SuccessfulKeyCount">Number of keys successfully deleted.</param>
/// <param name="UnsuccessfulKeys">Keys that failed to delete (should be retried).</param>
public record KvBulkDeleteResult(
  [property: JsonPropertyName("successful_key_count")]
  int SuccessfulKeyCount,

  [property: JsonPropertyName("unsuccessful_keys")]
  IReadOnlyList<string>? UnsuccessfulKeys = null
);

/// <summary>Request for bulk get operation.</summary>
/// <param name="Keys">Array of key names to retrieve (max 100).</param>
/// <param name="Type">Value type: "text" or "json".</param>
/// <param name="WithMetadata">Whether to include metadata in response.</param>
/// <remarks>
///   Note: The Cloudflare Bulk Get endpoint uses camelCase for the request body properties.
///   This differs from most other Cloudflare API endpoints which use snake_case.
/// </remarks>
internal record KvBulkGetRequest(
  [property: JsonPropertyName("keys")]
  IReadOnlyList<string> Keys,

  [property: JsonPropertyName("type")]
  string? Type = null,

  [property: JsonPropertyName("withMetadata")]
  bool? WithMetadata = null
);

/// <summary>Response from bulk get operation (without metadata).</summary>
/// <param name="Values">Dictionary of key-value pairs. Keys not found will have null values.</param>
/// <remarks>
///   The API returns values as a dictionary: { "key1": "value1", "key2": "value2" }.
///   Keys that were not found will have null values in the dictionary.
/// </remarks>
internal record KvBulkGetResult(
  [property: JsonPropertyName("values")]
  IReadOnlyDictionary<string, string?> Values
);

/// <summary>Response from bulk get operation (with metadata).</summary>
/// <param name="Values">Dictionary of key-value pairs with metadata.</param>
/// <remarks>
///   When withMetadata=true, each value is an object containing value and metadata.
/// </remarks>
internal record KvBulkGetResultWithMetadata(
  [property: JsonPropertyName("values")]
  IReadOnlyDictionary<string, KvBulkGetItemWithMetadata?> Values
);

/// <summary>A key-value pair with metadata from bulk get.</summary>
/// <param name="Value">The value (null if key not found).</param>
/// <param name="Metadata">The metadata associated with the key.</param>
public record KvBulkGetItemWithMetadata(
  [property: JsonPropertyName("value")]
  string? Value,

  [property: JsonPropertyName("metadata")]
  JsonElement? Metadata = null
);

#endregion


#region Internal Request Types

/// <summary>Internal request body for creating a KV namespace.</summary>
/// <param name="Title">The title for the new namespace.</param>
internal record CreateKvNamespaceRequest(
  [property: JsonPropertyName("title")]
  string Title
);

/// <summary>Internal request body for renaming a KV namespace.</summary>
/// <param name="Title">The new title for the namespace.</param>
internal record RenameKvNamespaceRequest(
  [property: JsonPropertyName("title")]
  string Title
);

#endregion
