namespace TaiwanUtilities;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonConverter))]
[TypeConverter(typeof(TypeConverter))]
partial struct ChineseNumeric : ISerializable
{
    private sealed class JsonConverter : JsonConverter<ChineseNumeric>
    {
        public override void Write(Utf8JsonWriter writer, ChineseNumeric value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }

        public override ChineseNumeric Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is JsonTokenType.String)
            {
                return reader.GetString() is string v ? decimal.Parse(v) : 0m;
            }

            if (reader.TokenType is JsonTokenType.Number)
            {
                return reader.GetDecimal();
            }

            return default;
        }
    }

    private sealed class TypeConverter : System.ComponentModel.TypeConverter
    {
        private static readonly Type s_stringType = typeof(string);
        private static readonly Type s_decimalType = typeof(decimal);
        private static readonly Type s_chineseNumericType = typeof(ChineseNumeric);

        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == s_stringType ||
                sourceType == s_decimalType ||
                sourceType == s_chineseNumericType)
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            if (destinationType == s_stringType ||
                destinationType == s_decimalType ||
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
                ChineseNumeric cn => cn,
                decimal dec => new ChineseNumeric(dec),
                string str => Parse(str),
                _ => base.ConvertFrom(context, culture, value)
            };
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (value is ChineseNumeric sd)
            {
                if (destinationType == s_chineseNumericType)
                {
                    return sd;
                }

                if (destinationType == s_stringType)
                {
                    return sd.ToString();
                }

                if (destinationType == s_decimalType)
                {
                    return (decimal)sd;
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

    }

    private ChineseNumeric(SerializationInfo info, StreamingContext context)
        :this(info.GetDecimal("VALUE"))
    {
        
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        Guard.ThrowIfNull(info);
        info.AddValue("VALUE", GetRawValue());
    }
}
