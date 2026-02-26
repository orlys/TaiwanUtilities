namespace TaiwanUtilities;

using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonConverter(typeof(PostalAddress.JsonConverter))]
[TypeConverter(typeof(PostalAddress.PostalAddressTypeConverter))]
partial class PostalAddress
{
    private sealed class JsonConverter : JsonConverter<PostalAddress>
    {
        public override void Write(Utf8JsonWriter writer, PostalAddress value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }
            writer.WriteStringValue(value.ToAddressString());
        }

        public override PostalAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType is JsonTokenType.String)
            {
                var str = reader.GetString();
                if (string.IsNullOrWhiteSpace(str))
                {
                    return null;
                }
                return Parse(str);
            }

            return null;
        }
    }

    public static implicit operator string?(PostalAddress? address)
    {
        return address?.ToAddressString();
    }

    public static implicit operator PostalAddress?(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }
        return Parse(address);
    }

    private sealed class PostalAddressTypeConverter : System.ComponentModel.TypeConverter
    {
        private static readonly Type s_stringType = typeof(string);
        private static readonly Type s_postalAddressType = typeof(PostalAddress);

        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == s_stringType ||
                sourceType == s_postalAddressType)
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            if (destinationType == s_stringType ||
                destinationType == s_postalAddressType)
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
            if (value is PostalAddress pa)
            {
                if (destinationType == s_postalAddressType)
                {
                    return pa;
                }

                if (destinationType == s_stringType)
                {
                    return pa.ToAddressString();
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}