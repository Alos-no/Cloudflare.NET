namespace Cloudflare.NET.Analytics.Tests.IntegrationTests.Models;

using System.Text.Json.Serialization;

// Top-level response structure
/// <summary>Represents the top-level structure of the GraphQL analytics response.</summary>
/// <param name="Viewer">The root viewer object containing account data.</param>
public record GraphQLResponse(
  [property: JsonPropertyName("viewer")]
  Viewer Viewer
);

/// <summary>Represents the viewer object in the GraphQL response.</summary>
/// <param name="Accounts">A list of accounts matching the query filter.</param>
public record Viewer(
  [property: JsonPropertyName("accounts")]
  List<Account> Accounts
);

/// <summary>Represents the analytics data for a single Cloudflare account.</summary>
public record Account(
  [property: JsonPropertyName("storage")]
  List<StorageGroup> Storage,
  [property: JsonPropertyName("bucketClassA")]
  List<OperationsGroup> BucketClassA,
  [property: JsonPropertyName("bucketClassB")]
  List<OperationsGroup> BucketClassB,
  [property: JsonPropertyName("coldClassA")]
  List<OperationsGroup> ColdClassA,
  [property: JsonPropertyName("coldClassB")]
  List<OperationsGroup> ColdClassB,
  [property: JsonPropertyName("hotClassA")]
  List<OperationsGroup> HotClassA,
  [property: JsonPropertyName("hotClassB")]
  List<OperationsGroup> HotClassB
);

/// <summary>Represents a group of operations metrics (e.g., Class A or Class B requests).</summary>
/// <param name="Dimensions">The dimensions by which the data is grouped.</param>
/// <param name="Sum">The summary of metrics for the group.</param>
public record OperationsGroup(
  [property: JsonPropertyName("dimensions")]
  Dimensions Dimensions,
  [property: JsonPropertyName("sum")]
  OperationsSummary? Sum
);

/// <summary>Represents a single storage snapshot at a specific point in time.</summary>
/// <param name="Dimensions">The dimensions for the snapshot (bucket name and time).</param>
/// <param name="Max">The maximum storage metrics observed in the time interval.</param>
public record StorageGroup(
  [property: JsonPropertyName("dimensions")]
  Dimensions Dimensions,
  [property: JsonPropertyName("max")]
  StorageMetrics? Max
);

/// <summary>Represents the dimensions for an analytics data point.</summary>
/// <param name="BucketName">The name of the R2 bucket.</param>
/// <param name="Datetime">The timestamp for the data point.</param>
public record Dimensions(
  [property: JsonPropertyName("bucketName")]
  string BucketName,
  [property: JsonPropertyName("datetime")]
  DateTime? Datetime
);

/// <summary>Represents the summary of operation metrics.</summary>
/// <param name="Requests">The total number of requests.</param>
/// <param name="ResponseObjectSize">The total size of response objects, if applicable.</param>
public record OperationsSummary(
  [property: JsonPropertyName("requests")]
  long Requests,
  [property: JsonPropertyName("responseObjectSize")]
  long? ResponseObjectSize
);

/// <summary>Represents the metrics for storage size.</summary>
/// <param name="PayloadSize">The total size of object payloads in bytes.</param>
/// <param name="MetadataSize">The total size of object metadata in bytes.</param>
/// <param name="ObjectCount">The total number of objects.</param>
public record StorageMetrics(
  [property: JsonPropertyName("payloadSize")]
  long PayloadSize,
  [property: JsonPropertyName("metadataSize")]
  long MetadataSize,
  [property: JsonPropertyName("objectCount")]
  long ObjectCount
);
