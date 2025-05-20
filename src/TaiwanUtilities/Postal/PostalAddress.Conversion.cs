namespace TaiwanUtilities;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonConverter))]
[TypeConverter(typeof(TypeConverter))]
partial class PostalAddress : ISerializable
{
    private sealed class JsonConverter : JsonConverter<PostalAddress>
    {
        public override void Write(Utf8JsonWriter writer, PostalAddress value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }

        public override PostalAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return TryParse(reader.GetString(), out var pa)
                ? pa
                : null
                ;
        }
    }

    private sealed class TypeConverter : System.ComponentModel.TypeConverter
    {
        private static readonly Type s_stringType = typeof(string);
        private static readonly Type s_chineseNumericType = typeof(PostalAddress);

        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == s_stringType ||
                sourceType == s_chineseNumericType)
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            if (destinationType == s_stringType ||
                destinationType == s_chineseNumericType)
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
        {
            return value switch
            {
                PostalAddress pa => pa,
                string str => Parse(str),
                _ => base.ConvertFrom(context, culture, value)
            };
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (value is PostalAddress sd)
            {
                if (destinationType == s_chineseNumericType)
                {
                    return sd;
                }

                if (destinationType == s_stringType)
                {
                    return sd.ToString();
                }

            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

    }

    private PostalAddress(SerializationInfo info, StreamingContext context) : this(
        info.GetString("COUNTY"),
        info.GetString("TOWN"),
        info.GetString("VILLAGE"),
        info.GetString("NEIGHBOR"),
        info.GetString("AREA"),
        info.GetString("ROAD"),
        info.GetString("LANE"),
        info.GetString("ALLEY"),
        info.GetString("SUB_ALLEY"),
        info.GetString("NUMBER"),
        info.GetString("FLOOR"),
        info.GetString("ROOM"),
        info.GetString("ADDRESS"),
        info.GetBoolean("IS_TEMPORARY"))
    {
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        Guard.ThrowIfNull(info);

        info.AddValue("COUNTY", County);
        info.AddValue("TOWN", Town);
        info.AddValue("VILLAGE", Village);
        info.AddValue("NEIGHBOR", Neighbor);
        info.AddValue("AREA", Area);

        info.AddValue("ROAD", Road);
        info.AddValue("LANE", Lane);
        info.AddValue("ALLEY", Alley);
        info.AddValue("SUB_ALLEY", SubAlley);
        info.AddValue("NUMBER", Number);
        info.AddValue("FLOOR", Floor);
        info.AddValue("ROOM", Room);
        info.AddValue("ADDRESS", Address);
        info.AddValue("IS_TEMPORARY", IsTemporary);
    }
}
