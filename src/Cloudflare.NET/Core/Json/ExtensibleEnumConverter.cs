namespace Cloudflare.NET.Core.Json;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
///   A JSON converter for extensible enum types that implement <see cref="IExtensibleEnum{TSelf}" />.
///   <para>
///     This converter serializes extensible enums as their underlying string value and deserializes
///     JSON strings back into the appropriate extensible enum type.
///   </para>
/// </summary>
/// <typeparam name="T">The extensible enum type to convert.</typeparam>
/// <remarks>
///   <para>
///     When deserializing, null JSON values result in default(<typeparamref name="T" />).
///     For nullable properties, consider using <see cref="Nullable{T}" /> with this converter.
///   </para>
///   <para>
///     This converter supports both direct application via <see cref="JsonConverterAttribute" />
///     and registration through <see cref="JsonSerializerOptions.Converters" />.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   // Direct attribute application
///   [JsonConverter(typeof(ExtensibleEnumConverter&lt;R2LocationHint&gt;))]
///   public R2LocationHint Location { get; set; }
///   </code>
/// </example>
public class ExtensibleEnumConverter<T> : JsonConverter<T> where T : struct, IExtensibleEnum<T>
{
  #region Methods Impl

  /// <inheritdoc />
  public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    var value = reader.GetString();

    // Return default for null values - nullable handling is done at the property level
    if (value is null)
      return default;

    return T.Create(value);
  }

  /// <inheritdoc />
  public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
  {
    var stringValue = value.Value;

    if (stringValue is null)
      writer.WriteNullValue();
    else
      writer.WriteStringValue(stringValue);
  }

  #endregion
}

/// <summary>
///   A JSON converter factory that automatically creates converters for any type implementing
///   <see cref="IExtensibleEnum{TSelf}" />.
///   <para>
///     Register this factory with <see cref="JsonSerializerOptions.Converters" /> to enable
///     automatic serialization of all extensible enum types without individual converter attributes.
///   </para>
/// </summary>
/// <remarks>
///   <para>
///     This factory uses reflection to detect types implementing the extensible enum interface
///     and creates the appropriate generic converter instance.
///   </para>
/// </remarks>
/// <example>
///   <code>
///   var options = new JsonSerializerOptions();
///   options.Converters.Add(new ExtensibleEnumConverterFactory());
///   </code>
/// </example>
public class ExtensibleEnumConverterFactory : JsonConverterFactory
{
  #region Methods Impl

  /// <inheritdoc />
  public override bool CanConvert(Type typeToConvert)
  {
    // Check if the type implements IExtensibleEnum<TSelf>
    if (!typeToConvert.IsValueType)
      return false;

    // Look for IExtensibleEnum<T> interface where T is the type itself
    var extensibleEnumInterface = typeToConvert
      .GetInterfaces()
      .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IExtensibleEnum<>));

    if (extensibleEnumInterface is null)
      return false;

    // Ensure TSelf matches the type (self-referencing constraint)
    var selfType = extensibleEnumInterface.GetGenericArguments()[0];

    return selfType == typeToConvert;
  }

  /// <inheritdoc />
  public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
  {
    var converterType = typeof(ExtensibleEnumConverter<>).MakeGenericType(typeToConvert);

    return (JsonConverter)Activator.CreateInstance(converterType)!;
  }

  #endregion
}
