namespace Cloudflare.NET.Accounts.Kv;

using System.Text.Json;
using Core.Exceptions;
using Core.Models;
using Models;

/// <summary>
///   Provides access to Cloudflare Workers KV operations.
///   Workers KV is a global, low-latency key-value data store.
/// </summary>
/// <remarks>
///   <para>
///     All operations are account-scoped. The account ID is configured
///     via <see cref="Core.CloudflareApiOptions.AccountId" />.
///   </para>
///   <para>
///     <strong>API Limits:</strong>
///     <list type="bullet">
///       <item><description>Key name max length: 512 bytes</description></item>
///       <item><description>Value max size: 25 MiB</description></item>
///       <item><description>Metadata max size: 1024 bytes (serialized JSON)</description></item>
///       <item><description>Minimum TTL: 60 seconds</description></item>
///       <item><description>Write rate limit: 1 per second per key</description></item>
///     </list>
///   </para>
/// </remarks>
/// <seealso href="https://developers.cloudflare.com/api/resources/kv/" />
public interface IKvApi
{
  #region Namespace Operations

  /// <summary>Lists KV namespaces in the account.</summary>
  /// <param name="filters">Optional filters for pagination and ordering.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>A page of namespaces with pagination info.</returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/methods/list/" />
  Task<PagePaginatedResult<KvNamespace>> ListAsync(
    ListKvNamespacesFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>Lists all KV namespaces, automatically handling pagination.</summary>
  /// <param name="filters">Optional filters for ordering.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>An async enumerable of all namespaces.</returns>
  IAsyncEnumerable<KvNamespace> ListAllAsync(
    ListKvNamespacesFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>Creates a new KV namespace.</summary>
  /// <param name="title">The title for the new namespace. Must be unique per account.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The created namespace.</returns>
  /// <exception cref="CloudflareApiException">Thrown if title already exists (code 10014).</exception>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/methods/create/" />
  Task<KvNamespace> CreateAsync(
    string title,
    CancellationToken cancellationToken = default);

  /// <summary>Gets a specific KV namespace by ID.</summary>
  /// <param name="namespaceId">The namespace ID.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The namespace details.</returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/methods/get/" />
  Task<KvNamespace> GetAsync(
    string namespaceId,
    CancellationToken cancellationToken = default);

  /// <summary>Renames a KV namespace.</summary>
  /// <param name="namespaceId">The namespace ID to rename.</param>
  /// <param name="title">The new title for the namespace. Must be unique per account.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The updated namespace with id, title, and supports_url_encoding.</returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/methods/update/" />
  Task<KvNamespace> RenameAsync(
    string namespaceId,
    string title,
    CancellationToken cancellationToken = default);

  /// <summary>Deletes a KV namespace and all its keys.</summary>
  /// <param name="namespaceId">The namespace ID to delete.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/methods/delete/" />
  Task DeleteAsync(
    string namespaceId,
    CancellationToken cancellationToken = default);

  #endregion


  #region Key Operations

  /// <summary>Lists keys in a namespace with cursor-based pagination.</summary>
  /// <param name="namespaceId">The namespace ID.</param>
  /// <param name="filters">Optional filters for prefix, limit, and cursor.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Keys and pagination info (use CursorInfo.Cursor for next page).</returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/keys/methods/list/" />
  Task<CursorPaginatedResult<KvKey>> ListKeysAsync(
    string namespaceId,
    ListKvKeysFilters? filters = null,
    CancellationToken cancellationToken = default);

  /// <summary>Lists all keys in a namespace, automatically handling pagination.</summary>
  /// <param name="namespaceId">The namespace ID.</param>
  /// <param name="prefix">Optional prefix filter.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>An async enumerable of all keys.</returns>
  IAsyncEnumerable<KvKey> ListAllKeysAsync(
    string namespaceId,
    string? prefix = null,
    CancellationToken cancellationToken = default);

  #endregion


  #region Value Operations

  /// <summary>Reads a value by key as a string.</summary>
  /// <param name="namespaceId">The namespace ID.</param>
  /// <param name="key">The key name.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The value as a string, or null if the key doesn't exist.</returns>
  /// <remarks>Use <see cref="GetValueWithExpirationAsync" /> if you need the expiration timestamp.</remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/values/methods/get/" />
  Task<string?> GetValueAsync(
    string namespaceId,
    string key,
    CancellationToken cancellationToken = default);

  /// <summary>Reads a value by key as a string, including expiration info.</summary>
  /// <param name="namespaceId">The namespace ID.</param>
  /// <param name="key">The key name.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The value and expiration, or null if the key doesn't exist.</returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/values/methods/get/" />
  Task<KvStringValueResult?> GetValueWithExpirationAsync(
    string namespaceId,
    string key,
    CancellationToken cancellationToken = default);

  /// <summary>Reads a value by key as bytes.</summary>
  /// <param name="namespaceId">The namespace ID.</param>
  /// <param name="key">The key name.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The value as bytes, or null if the key doesn't exist.</returns>
  /// <remarks>Use <see cref="GetValueBytesWithExpirationAsync" /> if you need the expiration timestamp.</remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/values/methods/get/" />
  Task<byte[]?> GetValueBytesAsync(
    string namespaceId,
    string key,
    CancellationToken cancellationToken = default);

  /// <summary>Reads a value by key as bytes, including expiration info.</summary>
  /// <param name="namespaceId">The namespace ID.</param>
  /// <param name="key">The key name.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The value and expiration, or null if the key doesn't exist.</returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/values/methods/get/" />
  Task<KvValueResult?> GetValueBytesWithExpirationAsync(
    string namespaceId,
    string key,
    CancellationToken cancellationToken = default);

  /// <summary>Reads only the metadata for a key (without retrieving the value).</summary>
  /// <param name="namespaceId">The namespace ID.</param>
  /// <param name="key">The key name.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The metadata as JSON, or null if no metadata exists.</returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/metadata/methods/get/" />
  Task<JsonElement?> GetMetadataAsync(
    string namespaceId,
    string key,
    CancellationToken cancellationToken = default);

  /// <summary>Writes a string value.</summary>
  /// <param name="namespaceId">The namespace ID.</param>
  /// <param name="key">The key name (max 512 bytes).</param>
  /// <param name="value">The value to store (max 25 MiB).</param>
  /// <param name="options">Optional expiration and metadata settings.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/values/methods/update/" />
  Task WriteValueAsync(
    string namespaceId,
    string key,
    string value,
    KvWriteOptions? options = null,
    CancellationToken cancellationToken = default);

  /// <summary>Writes a binary value.</summary>
  /// <param name="namespaceId">The namespace ID.</param>
  /// <param name="key">The key name (max 512 bytes).</param>
  /// <param name="value">The value to store (max 25 MiB).</param>
  /// <param name="options">Optional expiration and metadata settings.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/values/methods/update/" />
  Task WriteValueAsync(
    string namespaceId,
    string key,
    byte[] value,
    KvWriteOptions? options = null,
    CancellationToken cancellationToken = default);

  /// <summary>Deletes a key-value pair.</summary>
  /// <param name="namespaceId">The namespace ID.</param>
  /// <param name="key">The key name to delete.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/values/methods/delete/" />
  Task DeleteValueAsync(
    string namespaceId,
    string key,
    CancellationToken cancellationToken = default);

  #endregion


  #region Bulk Operations

  /// <summary>Writes multiple key-value pairs in a single request.</summary>
  /// <param name="namespaceId">The namespace ID.</param>
  /// <param name="items">Key-value pairs to write (max 10,000 items, 100MB total).</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Result indicating success/failure counts.</returns>
  /// <remarks>
  ///   Existing values and expirations will be overwritten.
  ///   Check <see cref="KvBulkWriteResult.UnsuccessfulKeys" /> and retry if needed.
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/methods/bulk_update/" />
  Task<KvBulkWriteResult> BulkWriteAsync(
    string namespaceId,
    IEnumerable<KvBulkWriteItem> items,
    CancellationToken cancellationToken = default);

  /// <summary>Deletes multiple keys in a single request.</summary>
  /// <param name="namespaceId">The namespace ID.</param>
  /// <param name="keys">Keys to delete (max 10,000 keys).</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Result indicating success/failure counts.</returns>
  /// <remarks>The request body is a simple JSON array of key names: ["key1", "key2"].</remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/methods/bulk_delete/" />
  Task<KvBulkDeleteResult> BulkDeleteAsync(
    string namespaceId,
    IEnumerable<string> keys,
    CancellationToken cancellationToken = default);

  /// <summary>Retrieves multiple values in a single request.</summary>
  /// <param name="namespaceId">The namespace ID.</param>
  /// <param name="keys">Keys to retrieve (max 100 keys).</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Dictionary of key-value pairs (value is null for non-existent keys).</returns>
  /// <remarks>
  ///   The API returns values as a dictionary: { "key1": "value1", "key2": "value2" }.
  ///   Use <see cref="BulkGetWithMetadataAsync" /> if you need metadata for each key.
  /// </remarks>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/keys/methods/bulk_get/" />
  Task<IReadOnlyDictionary<string, string?>> BulkGetAsync(
    string namespaceId,
    IEnumerable<string> keys,
    CancellationToken cancellationToken = default);

  /// <summary>Retrieves multiple values with metadata in a single request.</summary>
  /// <param name="namespaceId">The namespace ID.</param>
  /// <param name="keys">Keys to retrieve (max 100 keys).</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Dictionary of key-value pairs with metadata.</returns>
  /// <seealso href="https://developers.cloudflare.com/api/resources/kv/subresources/namespaces/subresources/keys/methods/bulk_get/" />
  Task<IReadOnlyDictionary<string, KvBulkGetItemWithMetadata?>> BulkGetWithMetadataAsync(
    string namespaceId,
    IEnumerable<string> keys,
    CancellationToken cancellationToken = default);

  #endregion
}
