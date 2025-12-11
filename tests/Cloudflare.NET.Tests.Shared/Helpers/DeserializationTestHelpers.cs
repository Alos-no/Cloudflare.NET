namespace Cloudflare.NET.Tests.Shared.Helpers;

using Xunit;

/// <summary>
///   Helpers for testing JSON deserialization of Cloudflare API responses.
/// </summary>
public static class DeserializationTestHelpers
{
  #region DateTime Assertions

  /// <summary>
  ///   Asserts that a DateTime property parses ISO 8601 format correctly.
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="dateTimeSelector">Selector for the DateTime property.</param>
  /// <param name="expectedUtc">Expected UTC DateTime value.</param>
  public static void AssertDateTimeParsedCorrectly<T>(
    T entity,
    Func<T, DateTime> dateTimeSelector,
    DateTime expectedUtc)
  {
    var actual = dateTimeSelector(entity);

    Assert.Equal(expectedUtc, actual.ToUniversalTime());
    Assert.Equal(DateTimeKind.Utc, actual.Kind);
  }

  /// <summary>
  ///   Asserts that a nullable DateTime property is null when missing from response.
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="dateTimeSelector">Selector for the nullable DateTime property.</param>
  public static void AssertDateTimeIsNull<T>(
    T entity,
    Func<T, DateTime?> dateTimeSelector)
  {
    Assert.Null(dateTimeSelector(entity));
  }

  /// <summary>
  ///   Asserts that a nullable DateTime property has a value.
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="dateTimeSelector">Selector for the nullable DateTime property.</param>
  /// <param name="expectedUtc">Expected UTC DateTime value.</param>
  public static void AssertNullableDateTimeParsedCorrectly<T>(
    T entity,
    Func<T, DateTime?> dateTimeSelector,
    DateTime expectedUtc)
  {
    var actual = dateTimeSelector(entity);

    Assert.NotNull(actual);
    Assert.Equal(expectedUtc, actual.Value.ToUniversalTime());
    Assert.Equal(DateTimeKind.Utc, actual.Value.Kind);
  }

  /// <summary>
  ///   Asserts that a DateTimeOffset property parses correctly.
  /// </summary>
  /// <typeparam name="T">Entity type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="dateTimeSelector">Selector for the DateTimeOffset property.</param>
  /// <param name="expectedUtc">Expected UTC DateTimeOffset value.</param>
  public static void AssertDateTimeOffsetParsedCorrectly<T>(
    T entity,
    Func<T, DateTimeOffset> dateTimeSelector,
    DateTimeOffset expectedUtc)
  {
    var actual = dateTimeSelector(entity);

    Assert.Equal(expectedUtc, actual);
    Assert.Equal(TimeSpan.Zero, actual.Offset); // Should be UTC
  }

  #endregion


  #region Extensible Enum Assertions

  /// <summary>
  ///   Asserts that an extensible enum handles unknown values gracefully by preserving the raw value.
  /// </summary>
  /// <typeparam name="TEntity">Entity type.</typeparam>
  /// <typeparam name="TEnum">Extensible enum type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="enumSelector">Selector for the enum property.</param>
  /// <param name="expectedRawValue">Expected raw string value.</param>
  public static void AssertExtensibleEnumPreservesUnknown<TEntity, TEnum>(
    TEntity entity,
    Func<TEntity, TEnum> enumSelector,
    string expectedRawValue)
  {
    var enumValue = enumSelector(entity);

    Assert.Equal(expectedRawValue, enumValue?.ToString());
  }

  /// <summary>
  ///   Asserts that an extensible enum equals the expected known value.
  /// </summary>
  /// <typeparam name="TEntity">Entity type.</typeparam>
  /// <typeparam name="TEnum">Extensible enum type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="enumSelector">Selector for the enum property.</param>
  /// <param name="expectedValue">Expected enum value.</param>
  public static void AssertExtensibleEnumEquals<TEntity, TEnum>(
    TEntity entity,
    Func<TEntity, TEnum> enumSelector,
    TEnum expectedValue)
  {
    var actualValue = enumSelector(entity);

    Assert.Equal(expectedValue, actualValue);
  }

  #endregion


  #region Collection Assertions

  /// <summary>
  ///   Asserts that an array property deserializes to a non-empty list.
  /// </summary>
  /// <typeparam name="TEntity">Entity type.</typeparam>
  /// <typeparam name="TItem">List item type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="listSelector">Selector for the list property.</param>
  /// <param name="expectedCount">Expected item count.</param>
  public static void AssertListHasItems<TEntity, TItem>(
    TEntity entity,
    Func<TEntity, IReadOnlyList<TItem>?> listSelector,
    int expectedCount)
  {
    var list = listSelector(entity);

    Assert.NotNull(list);
    Assert.Equal(expectedCount, list!.Count);
  }

  /// <summary>
  ///   Asserts that an array property deserializes to an empty list (not null).
  /// </summary>
  /// <typeparam name="TEntity">Entity type.</typeparam>
  /// <typeparam name="TItem">List item type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="listSelector">Selector for the list property.</param>
  public static void AssertListIsEmpty<TEntity, TItem>(
    TEntity entity,
    Func<TEntity, IReadOnlyList<TItem>?> listSelector)
  {
    var list = listSelector(entity);

    Assert.NotNull(list);
    Assert.Empty(list!);
  }

  /// <summary>
  ///   Asserts that a nullable array property is null when missing from response.
  /// </summary>
  /// <typeparam name="TEntity">Entity type.</typeparam>
  /// <typeparam name="TItem">List item type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="listSelector">Selector for the nullable list property.</param>
  public static void AssertListIsNull<TEntity, TItem>(
    TEntity entity,
    Func<TEntity, IReadOnlyList<TItem>?> listSelector)
  {
    Assert.Null(listSelector(entity));
  }

  /// <summary>
  ///   Asserts that an enumerable property contains expected items.
  /// </summary>
  /// <typeparam name="TEntity">Entity type.</typeparam>
  /// <typeparam name="TItem">Item type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="listSelector">Selector for the enumerable property.</param>
  /// <param name="expectedItems">Expected items.</param>
  public static void AssertListContains<TEntity, TItem>(
    TEntity entity,
    Func<TEntity, IEnumerable<TItem>?> listSelector,
    params TItem[] expectedItems)
  {
    var list = listSelector(entity);

    Assert.NotNull(list);

    foreach (var expected in expectedItems)
    {
      Assert.Contains(expected, list!);
    }
  }

  #endregion


  #region Nullable Property Assertions

  /// <summary>
  ///   Asserts that a nullable property has the expected value.
  /// </summary>
  /// <typeparam name="TEntity">Entity type.</typeparam>
  /// <typeparam name="TValue">Value type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="propertySelector">Selector for the nullable property.</param>
  /// <param name="expectedValue">Expected value.</param>
  public static void AssertNullablePropertyEquals<TEntity, TValue>(
    TEntity entity,
    Func<TEntity, TValue?> propertySelector,
    TValue expectedValue)
    where TValue : struct
  {
    var actual = propertySelector(entity);

    Assert.NotNull(actual);
    Assert.Equal(expectedValue, actual.Value);
  }

  /// <summary>
  ///   Asserts that a nullable reference property has the expected value.
  /// </summary>
  /// <typeparam name="TEntity">Entity type.</typeparam>
  /// <typeparam name="TValue">Value type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="propertySelector">Selector for the nullable property.</param>
  /// <param name="expectedValue">Expected value.</param>
  public static void AssertNullableRefPropertyEquals<TEntity, TValue>(
    TEntity entity,
    Func<TEntity, TValue?> propertySelector,
    TValue expectedValue)
    where TValue : class
  {
    var actual = propertySelector(entity);

    Assert.NotNull(actual);
    Assert.Equal(expectedValue, actual);
  }

  /// <summary>
  ///   Asserts that a nullable property is null.
  /// </summary>
  /// <typeparam name="TEntity">Entity type.</typeparam>
  /// <typeparam name="TValue">Value type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="propertySelector">Selector for the nullable property.</param>
  public static void AssertNullablePropertyIsNull<TEntity, TValue>(
    TEntity entity,
    Func<TEntity, TValue?> propertySelector)
    where TValue : struct
  {
    Assert.Null(propertySelector(entity));
  }

  /// <summary>
  ///   Asserts that a nullable reference property is null.
  /// </summary>
  /// <typeparam name="TEntity">Entity type.</typeparam>
  /// <typeparam name="TValue">Value type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="propertySelector">Selector for the nullable property.</param>
  public static void AssertNullableRefPropertyIsNull<TEntity, TValue>(
    TEntity entity,
    Func<TEntity, TValue?> propertySelector)
    where TValue : class
  {
    Assert.Null(propertySelector(entity));
  }

  #endregion


  #region Nested Object Assertions

  /// <summary>
  ///   Asserts that a nested object property is not null and can be inspected.
  /// </summary>
  /// <typeparam name="TEntity">Entity type.</typeparam>
  /// <typeparam name="TNested">Nested object type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="nestedSelector">Selector for the nested object property.</param>
  /// <param name="assertions">Action to perform assertions on the nested object.</param>
  public static void AssertNestedObject<TEntity, TNested>(
    TEntity entity,
    Func<TEntity, TNested?> nestedSelector,
    Action<TNested> assertions)
    where TNested : class
  {
    var nested = nestedSelector(entity);

    Assert.NotNull(nested);
    assertions(nested!);
  }

  /// <summary>
  ///   Asserts that a nested object property is null.
  /// </summary>
  /// <typeparam name="TEntity">Entity type.</typeparam>
  /// <typeparam name="TNested">Nested object type.</typeparam>
  /// <param name="entity">The deserialized entity.</param>
  /// <param name="nestedSelector">Selector for the nested object property.</param>
  public static void AssertNestedObjectIsNull<TEntity, TNested>(
    TEntity entity,
    Func<TEntity, TNested?> nestedSelector)
    where TNested : class
  {
    Assert.Null(nestedSelector(entity));
  }

  #endregion
}
