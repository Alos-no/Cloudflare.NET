namespace Cloudflare.NET.Accounts.Models;

using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Core.Json;


#region Bucket Lock Models

/// <summary>Defines the condition type for bucket lock rules.</summary>
[JsonConverter(typeof(JsonStringEnumMemberConverter))]
public enum BucketLockConditionType
{
  /// <summary>Retention based on object age in seconds.</summary>
  [EnumMember(Value = "Age")] Age,

  /// <summary>Retention until a specific date.</summary>
  [EnumMember(Value = "Date")] Date,

  /// <summary>Indefinite retention (permanent lock).</summary>
  [EnumMember(Value = "Indefinite")] Indefinite
}

/// <summary>Represents the condition that determines how long objects are locked.</summary>
/// <param name="Type">The type of lock condition (Age, Date, or Indefinite).</param>
/// <param name="MaxAgeSeconds">
///   The retention duration in seconds. Used when <see cref="Type" /> is <see cref="BucketLockConditionType.Age" />.
/// </param>
/// <param name="Date">
///   The date until which objects are locked. Used when <see cref="Type" /> is <see cref="BucketLockConditionType.Date" />.
/// </param>
public record BucketLockCondition(
  [property: JsonPropertyName("type")]
  BucketLockConditionType Type,
  [property: JsonPropertyName("maxAgeSeconds")]
  int? MaxAgeSeconds = null,
  [property: JsonPropertyName("date")]
  DateTime? Date = null
)
{
  /// <summary>The number of seconds in a day (86400).</summary>
  private const int SecondsPerDay = 86400;

  /// <summary>Creates an age-based lock condition using days.</summary>
  /// <param name="days">The number of days to retain objects.</param>
  /// <returns>A new <see cref="BucketLockCondition" /> with Age type and MaxAgeSeconds in seconds.</returns>
  public static BucketLockCondition ForDays(int days) =>
    new(BucketLockConditionType.Age, days * SecondsPerDay);

  /// <summary>Creates an age-based lock condition using seconds.</summary>
  /// <param name="seconds">The number of seconds to retain objects.</param>
  /// <returns>A new <see cref="BucketLockCondition" /> with Age type and MaxAgeSeconds.</returns>
  public static BucketLockCondition ForSeconds(int seconds) =>
    new(BucketLockConditionType.Age, seconds);

  /// <summary>Creates a date-based lock condition.</summary>
  /// <param name="date">The date until which objects should be locked.</param>
  /// <returns>A new <see cref="BucketLockCondition" /> with Date type.</returns>
  public static BucketLockCondition UntilDate(DateTime date) =>
    new(BucketLockConditionType.Date, Date: date);

  /// <summary>Creates an indefinite lock condition (permanent retention).</summary>
  /// <returns>A new <see cref="BucketLockCondition" /> with Indefinite type.</returns>
  public static BucketLockCondition Indefinitely() =>
    new(BucketLockConditionType.Indefinite);
}

/// <summary>Represents a single bucket lock rule.</summary>
/// <param name="Id">A unique identifier for this lock rule.</param>
/// <param name="Enabled">Whether this rule is currently active.</param>
/// <param name="Prefix">
///   Optional object key prefix. If specified, only objects with keys starting with this prefix are affected.
///   If not specified (null or empty), the rule applies to all objects in the bucket.
/// </param>
/// <param name="Condition">The condition that determines the retention period.</param>
public record BucketLockRule(
  [property: JsonPropertyName("id")]
  string? Id = null,
  [property: JsonPropertyName("enabled")]
  bool Enabled = true,
  [property: JsonPropertyName("prefix")]
  string? Prefix = null,
  [property: JsonPropertyName("condition")]
  BucketLockCondition? Condition = null
);

/// <summary>Represents the bucket lock policy containing all lock rules for a bucket.</summary>
/// <param name="Rules">
///   The list of lock rules applied to the bucket. Maximum of 1000 rules.
///   If multiple rules apply to the same prefix or object key, the strictest (longest) retention takes precedence.
/// </param>
public record BucketLockPolicy(
  [property: JsonPropertyName("rules")]
  IReadOnlyList<BucketLockRule> Rules
);

#endregion
