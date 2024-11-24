namespace TaiwanUtilities;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;

using System.Text.Json;
using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonConverter))]
[TypeConverter(typeof(TypeConverter))]
partial struct RocDateTime : ISerializable, IConvertible
{
    private sealed class JsonConverter : JsonConverter<RocDateTime>
    {
        public override void Write(Utf8JsonWriter writer, RocDateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }

        public override RocDateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType is JsonTokenType.String)
            {
                if (reader.TryGetDateTimeOffset(out var dto))
                {
                    return new RocDateTime(dto);
                }

                if (reader.TryGetDateTime(out var dt))
                {
                    return new RocDateTime(dt);
                }

                if (TryParse(reader.GetString(), out var rdt))
                {
                    return rdt;
                }
            }

            return default;
        }
    }

    private sealed class TypeConverter : System.ComponentModel.TypeConverter
    {
        private static readonly Type s_stringType = typeof(string);

        private static readonly Type s_rocDateTimeType = typeof(RocDateTime);
        private static readonly Type s_dateTimeType = typeof(DateTime);
        private static readonly Type s_dateTimeOffsetType = typeof(DateTimeOffset);

        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == s_stringType ||
                sourceType == s_rocDateTimeType ||
                sourceType == s_dateTimeType ||
                sourceType == s_dateTimeOffsetType)
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            if (destinationType == s_stringType ||
                destinationType == s_rocDateTimeType ||
                destinationType == s_dateTimeType ||
                destinationType == s_dateTimeOffsetType)
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
        {
            return value switch
            {
                DateTimeOffset dto => new RocDateTime(dto),
                DateTime dt => new RocDateTime(dt),
                RocDateTime rdt => rdt,
                string str => Parse(str),
                _ => base.ConvertFrom(context, culture, value)
            };
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (value is RocDateTime sd)
            {
                if (destinationType == s_stringType)
                {
                    return sd.ToString();
                }

                if (destinationType == s_rocDateTimeType)
                {
                    return sd;
                }
                if (destinationType == s_dateTimeType)
                {
                    return (DateTime)sd;
                }
                if (destinationType == s_dateTimeOffsetType)
                {
                    return (DateTimeOffset)sd;
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("TIME", GetRawValue().UtcDateTime);
    }

    private RocDateTime(SerializationInfo info, StreamingContext context)
        : this(info.GetDateTime("TIME"))
    {
    }

    public TypeCode GetTypeCode() => TypeCode.DateTime;

    bool IConvertible.ToBoolean(IFormatProvider? provider) => throw InvalidCast(nameof(RocDateTime), nameof(Boolean));
    char IConvertible.ToChar(IFormatProvider? provider) => throw InvalidCast(nameof(RocDateTime), nameof(Char));
    sbyte IConvertible.ToSByte(IFormatProvider? provider) => throw InvalidCast(nameof(RocDateTime), nameof(SByte));
    byte IConvertible.ToByte(IFormatProvider? provider) => throw InvalidCast(nameof(RocDateTime), nameof(Byte));
    short IConvertible.ToInt16(IFormatProvider? provider) => throw InvalidCast(nameof(RocDateTime), nameof(Int16));
    ushort IConvertible.ToUInt16(IFormatProvider? provider) => throw InvalidCast(nameof(RocDateTime), nameof(UInt16));
    int IConvertible.ToInt32(IFormatProvider? provider) => throw InvalidCast(nameof(RocDateTime), nameof(Int32));
    uint IConvertible.ToUInt32(IFormatProvider? provider) => throw InvalidCast(nameof(RocDateTime), nameof(UInt32));
    long IConvertible.ToInt64(IFormatProvider? provider) => throw InvalidCast(nameof(RocDateTime), nameof(Int64));
    ulong IConvertible.ToUInt64(IFormatProvider? provider) => throw InvalidCast(nameof(RocDateTime), nameof(UInt64));
    float IConvertible.ToSingle(IFormatProvider? provider) => throw InvalidCast(nameof(RocDateTime), nameof(Single));
    double IConvertible.ToDouble(IFormatProvider? provider) => throw InvalidCast(nameof(RocDateTime), nameof(Double));
    decimal IConvertible.ToDecimal(IFormatProvider? provider) => throw InvalidCast(nameof(RocDateTime), nameof(Decimal));

    private static InvalidCastException InvalidCast(string from, string to) => new InvalidCastException($"Cannot convert {from} to {to}");

    DateTime IConvertible.ToDateTime(IFormatProvider? provider) => this;

    object IConvertible.ToType(Type type, IFormatProvider? provider)
    {
        if (type == typeof(DateTime))
        {
            return GetRawValue().DateTime;
        }

        if (type == typeof(RocDateTime))
        {
            return this;
        }

        if (type == typeof(DateTimeOffset))
        {
            return GetRawValue();
        }

        throw InvalidCast(nameof(RocDateTime), type.Name);
    }

}
