namespace Cloudflare.NET.Core.Json;

using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
///   A JsonConverterFactory that creates a converter to properly handle enums decorated
///   with EnumMemberAttribute. This allows for serialization and deserialization based on the
///   attribute's value rather than the enum member's name.
/// </summary>
public class JsonStringEnumMemberConverter : JsonConverterFactory
{
  #region Methods Impl

  /// <inheritdoc />
  public override bool CanConvert(Type typeToConvert)
  {
    return typeToConvert.IsEnum;
  }

  /// <inheritdoc />
  public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
  {
    return (JsonConverter)Activator.CreateInstance(
      typeof(EnumMemberConverter<>).MakeGenericType(typeToConvert),
      BindingFlags.Instance | BindingFlags.Public,
      null,
      null,
      null)!;
  }

  #endregion

  private class EnumMemberConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
  {
    #region Properties & Fields - Non-Public

    private readonly Dictionary<TEnum, string> _enumToString = new();
    private readonly Dictionary<string, TEnum> _stringToEnum = new();

    #endregion

    #region Constructors

    public EnumMemberConverter()
    {
      var type = typeof(TEnum);
      foreach (var memberName in Enum.GetNames<TEnum>())
      {
        var enumMember = type.GetMember(memberName).First();
        var attr       = enumMember.GetCustomAttribute<EnumMemberAttribute>();
        var name       = attr?.Value ?? memberName;

        var value = Enum.Parse<TEnum>(memberName);
        _enumToString[value] = name;
        _stringToEnum[name]  = value;
      }
    }

    #endregion

    #region Methods Impl

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      var value = reader.GetString();
      if (value is null || !_stringToEnum.TryGetValue(value, out var enumValue))
        throw new JsonException($"Unable to convert \"{value}\" to enum {typeof(TEnum)}.");

      return enumValue;
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
      writer.WriteStringValue(_enumToString[value]);
    }

    #endregion
  }
}
