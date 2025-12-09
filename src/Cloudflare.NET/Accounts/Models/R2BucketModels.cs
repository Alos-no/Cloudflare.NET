namespace Cloudflare.NET.Accounts.Models;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Core.Json;

#region Bucket Models

/// <summary>Defines the request payload for creating an R2 bucket.</summary>
/// <param name="Name">The name of the bucket to create.</param>
/// <param name="LocationHint">
///   Optional location hint suggesting where the bucket's data should be stored.
///   Cloudflare will attempt to honor this hint, but data may be placed in a nearby region if unavailable.
/// </param>
/// <param name="StorageClass">
///   Optional default storage class for new objects in the bucket.
///   If not specified, defaults to <see cref="R2StorageClass.Standard" />.
/// </param>
/// <remarks>
///   The jurisdiction parameter is not included in this request body because it must be passed
///   as an HTTP header (<c>cf-r2-jurisdiction</c>) according to the Cloudflare API specification.
/// </remarks>
public record CreateBucketRequest(
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("locationHint")]
  R2LocationHint? LocationHint = null,
  [property: JsonPropertyName("storageClass")]
  R2StorageClass? StorageClass = null
);

/// <summary>Represents an R2 bucket returned by the Cloudflare API.</summary>
/// <param name="Name">The name of the created bucket.</param>
/// <param name="CreationDate">The date and time the bucket was created.</param>
/// <param name="Location">The location hint for the bucket indicating geographic placement.</param>
/// <param name="Jurisdiction">The jurisdiction of the bucket guaranteeing data residency.</param>
/// <param name="StorageClass">The default storage class for new objects in the bucket.</param>
public record R2Bucket(
  [property: JsonPropertyName("name")]
  string Name,
  [property: JsonPropertyName("creation_date")]
  DateTime CreationDate,
  [property: JsonPropertyName("location")]
  R2LocationHint? Location,
  [property: JsonPropertyName("jurisdiction")]
  R2Jurisdiction? Jurisdiction,
  [property: JsonPropertyName("storage_class")]
  R2StorageClass? StorageClass
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

#endregion

#region CORS Models

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

#endregion

#region Lifecycle Models

/// <summary>Defines the condition type for lifecycle rule transitions.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum LifecycleConditionType
{
  /// <summary>Condition based on object age in days.</summary>
  [EnumMember(Value = "Age")] Age,

  /// <summary>Condition based on a specific date.</summary>
  [EnumMember(Value = "Date")] Date
}

/// <summary>Represents a condition that triggers a lifecycle transition.</summary>
/// <param name="Type">The type of condition (Age or Date).</param>
/// <param name="MaxAge">The age in seconds after which the transition occurs. Used when Type is Age.</param>
/// <param name="Date">The date on which the transition occurs. Used when Type is Date.</param>
public record LifecycleCondition(
  [property: JsonPropertyName("type")]
  LifecycleConditionType Type,
  [property: JsonPropertyName("maxAge")]
  int? MaxAge = null,
  [property: JsonPropertyName("date")]
  DateTime? Date = null
)
{
  /// <summary>The number of seconds in a day (86400).</summary>
  private const int SecondsPerDay = 86400;

  /// <summary>Creates an age-based lifecycle condition using days.</summary>
  /// <param name="days">The number of days after object creation.</param>
  /// <returns>A new <see cref="LifecycleCondition" /> with Age type and MaxAge in seconds.</returns>
  public static LifecycleCondition AfterDays(int days) =>
    new(LifecycleConditionType.Age, days * SecondsPerDay);

  /// <summary>Creates an age-based lifecycle condition using seconds.</summary>
  /// <param name="seconds">The number of seconds after object creation.</param>
  /// <returns>A new <see cref="LifecycleCondition" /> with Age type and MaxAge in seconds.</returns>
  public static LifecycleCondition AfterSeconds(int seconds) =>
    new(LifecycleConditionType.Age, seconds);

  /// <summary>Creates a date-based lifecycle condition.</summary>
  /// <param name="date">The date on which the transition should occur.</param>
  /// <returns>A new <see cref="LifecycleCondition" /> with Date type.</returns>
  public static LifecycleCondition OnDate(DateTime date) =>
    new(LifecycleConditionType.Date, Date: date);
}

/// <summary>Represents the filtering conditions for which objects a lifecycle rule applies to.</summary>
/// <param name="Prefix">
///   The object key prefix to filter objects. Only objects with keys starting with this prefix are
///   affected.
/// </param>
public record LifecycleRuleConditions(
  [property: JsonPropertyName("prefix")]
  string? Prefix = null
);

/// <summary>Represents a transition to abort incomplete multipart uploads.</summary>
/// <param name="Condition">The condition that triggers the abort operation.</param>
public record AbortMultipartUploadsTransition(
  [property: JsonPropertyName("condition")]
  LifecycleCondition Condition
);

/// <summary>Represents a transition to delete objects.</summary>
/// <param name="Condition">The condition that triggers the delete operation.</param>
public record DeleteObjectsTransition(
  [property: JsonPropertyName("condition")]
  LifecycleCondition Condition
);

/// <summary>Represents a transition to change the storage class of objects.</summary>
/// <param name="Condition">The condition that triggers the storage class transition.</param>
/// <param name="StorageClass">The target storage class (e.g., <see cref="R2StorageClass.InfrequentAccess" />).</param>
public record StorageClassTransition(
  [property: JsonPropertyName("condition")]
  LifecycleCondition Condition,
  [property: JsonPropertyName("storageClass")]
  R2StorageClass StorageClass
);

/// <summary>Represents a single lifecycle rule for an R2 bucket.</summary>
/// <param name="Id">A unique identifier for this lifecycle rule.</param>
/// <param name="Enabled">Whether this rule is currently active.</param>
/// <param name="Conditions">The filtering conditions that determine which objects this rule applies to.</param>
/// <param name="AbortMultipartUploadsTransition">The transition to abort incomplete multipart uploads.</param>
/// <param name="DeleteObjectsTransition">The transition to delete objects.</param>
/// <param name="StorageClassTransitions">The transitions to change object storage classes.</param>
public record LifecycleRule(
  [property: JsonPropertyName("id")]
  string? Id = null,
  [property: JsonPropertyName("enabled")]
  bool Enabled = true,
  [property: JsonPropertyName("conditions")]
  LifecycleRuleConditions? Conditions = null,
  [property: JsonPropertyName("abortMultipartUploadsTransition")]
  AbortMultipartUploadsTransition? AbortMultipartUploadsTransition = null,
  [property: JsonPropertyName("deleteObjectsTransition")]
  DeleteObjectsTransition? DeleteObjectsTransition = null,
  [property: JsonPropertyName("storageClassTransitions")]
  IReadOnlyList<StorageClassTransition>? StorageClassTransitions = null
);

/// <summary>Represents the lifecycle policy for an R2 bucket.</summary>
/// <param name="Rules">The list of lifecycle rules applied to the bucket. Maximum of 1000 rules.</param>
public record BucketLifecyclePolicy(
  [property: JsonPropertyName("rules")]
  IReadOnlyList<LifecycleRule> Rules
);

#endregion
