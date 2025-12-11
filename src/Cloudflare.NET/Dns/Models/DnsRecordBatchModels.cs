namespace Cloudflare.NET.Dns.Models;

using System.Text.Json.Serialization;
using DnsRecordType = Zones.Models.DnsRecordType;

/// <summary>
///   Request to perform batch DNS record operations in a single API call.
///   <para>
///     <strong>Execution Order:</strong> Deletes → Patches → Puts → Posts.
///     This order ensures that deletions happen first, allowing re-creation
///     of records with the same name in the same batch.
///   </para>
/// </summary>
/// <param name="Deletes">Records to delete (by ID). Executed first.</param>
/// <param name="Patches">Records to partially update (PATCH). Executed second.</param>
/// <param name="Puts">Records to fully replace (PUT). Executed third.</param>
/// <param name="Posts">Records to create (POST). Executed last.</param>
/// <example>
///   <code>
///   // Batch: delete one record, update another, create two new ones
///   var batch = new BatchDnsRecordsRequest(
///     Deletes: [new BatchDeleteOperation("record-id-to-delete")],
///     Patches: [new BatchPatchOperation("record-id-to-update", Content: "192.0.2.100")],
///     Posts: [
///       new CreateDnsRecordRequest(DnsRecordType.A, "new.example.com", "192.0.2.1"),
///       new CreateDnsRecordRequest(DnsRecordType.A, "new2.example.com", "192.0.2.2")
///     ]
///   );
///   var result = await dns.BatchDnsRecordsAsync(zoneId, batch);
///   </code>
/// </example>
/// <seealso href="https://developers.cloudflare.com/api/resources/dns/subresources/records/methods/batch/" />
public record BatchDnsRecordsRequest(
  [property: JsonPropertyName("deletes")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<BatchDeleteOperation>? Deletes = null,

  [property: JsonPropertyName("patches")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<BatchPatchOperation>? Patches = null,

  [property: JsonPropertyName("puts")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<BatchPutOperation>? Puts = null,

  [property: JsonPropertyName("posts")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  IReadOnlyList<CreateDnsRecordRequest>? Posts = null
);


/// <summary>
///   Delete operation for batch - references a record by its ID.
/// </summary>
/// <param name="Id">The unique identifier of the DNS record to delete.</param>
public record BatchDeleteOperation(
  [property: JsonPropertyName("id")]
  string Id
);


/// <summary>
///   Patch operation for batch - includes record ID and fields to update.
///   <para>
///     Only the fields you include will be updated; other fields retain their values.
///   </para>
/// </summary>
/// <param name="Id">The unique identifier of the DNS record to patch.</param>
/// <param name="Type">Optional: new record type.</param>
/// <param name="Name">Optional: new hostname.</param>
/// <param name="Content">Optional: new content.</param>
/// <param name="Ttl">Optional: new TTL in seconds.</param>
/// <param name="Proxied">Optional: new proxied status.</param>
/// <param name="Comment">Optional: new comment.</param>
public record BatchPatchOperation(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("type")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  DnsRecordType? Type = null,

  [property: JsonPropertyName("name")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Name = null,

  [property: JsonPropertyName("content")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Content = null,

  [property: JsonPropertyName("ttl")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  int? Ttl = null,

  [property: JsonPropertyName("proxied")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  bool? Proxied = null,

  [property: JsonPropertyName("comment")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Comment = null
);


/// <summary>
///   Put operation for batch - includes record ID and all fields for full replacement.
///   <para>
///     This performs a complete replacement of the record. All required fields must be provided.
///   </para>
/// </summary>
/// <param name="Id">The unique identifier of the DNS record to replace.</param>
/// <param name="Type">The DNS record type.</param>
/// <param name="Name">The record name (hostname).</param>
/// <param name="Content">The record content.</param>
/// <param name="Ttl">Time to live in seconds.</param>
/// <param name="Proxied">Optional: whether to proxy the record.</param>
/// <param name="Comment">Optional: comment describing the record.</param>
public record BatchPutOperation(
  [property: JsonPropertyName("id")]
  string Id,

  [property: JsonPropertyName("type")]
  DnsRecordType Type,

  [property: JsonPropertyName("name")]
  string Name,

  [property: JsonPropertyName("content")]
  string Content,

  [property: JsonPropertyName("ttl")]
  int Ttl = 1,

  [property: JsonPropertyName("proxied")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  bool? Proxied = null,

  [property: JsonPropertyName("comment")]
  [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  string? Comment = null
);


/// <summary>
///   Result of a batch DNS records operation.
///   <para>
///     Contains the results of each operation type. Each array contains
///     the resulting DNS records (for creates/updates) or the deleted records
///     (for deletes).
///   </para>
/// </summary>
/// <param name="Deletes">Results of delete operations (the deleted records).</param>
/// <param name="Patches">Results of patch operations (the updated records).</param>
/// <param name="Puts">Results of put operations (the replaced records).</param>
/// <param name="Posts">Results of post (create) operations (the new records).</param>
public record BatchDnsRecordsResult(
  [property: JsonPropertyName("deletes")]
  IReadOnlyList<DnsRecord>? Deletes = null,

  [property: JsonPropertyName("patches")]
  IReadOnlyList<DnsRecord>? Patches = null,

  [property: JsonPropertyName("puts")]
  IReadOnlyList<DnsRecord>? Puts = null,

  [property: JsonPropertyName("posts")]
  IReadOnlyList<DnsRecord>? Posts = null
);
