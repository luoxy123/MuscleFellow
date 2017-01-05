using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Sylvanas.Configuration;
using Sylvanas.Extensions;

namespace Sylvanas.Text
{
    public class QueryStringWriter : ITypeSerializer
    {
        public static ITypeSerializer Instance = new QueryStringWriter();

        public QueryStringWriter()
        {
            Setting = new QueryStringWriterSetting();
        }

        public QueryStringWriterSetting Setting { get; set; }
        public bool IncludeNullValues => false;

        public void WriteBuiltIn(TextWriter writer, object value)
        {
            writer.Write(value);
        }

        public void WriteRawString(TextWriter writer, string value)
        {
            writer.Write(value.UrlEncode());
        }

        public void WriteObjectString(TextWriter writer, object value)
        {
            if (value != null)
            {
                var strValue = value as string;
                if (strValue != null)
                    WriteString(writer, strValue);
                else
                    writer.Write(JsonConvert.SerializeObject(value).UrlEncode());
            }
        }

        public void WriteString(TextWriter writer, string value)
        {
            writer.Write(string.IsNullOrEmpty(value) ? "\"\"" : value.UrlEncode());
        }

        public void WriteFormattableObjectString(TextWriter writer, object value)
        {
            var f = (IFormattable) value;
            writer.Write(f.ToString(null, CultureInfo.InvariantCulture).UrlEncode());
        }

        public void WriteDateTime(TextWriter writer, object oDateTime)
        {
            var dateTime = (DateTime) oDateTime;
            switch (Setting.DateHandler)
            {
                case DateHandler.UnixTime:
                    writer.Write(dateTime.ToUnixTime());
                    return;
                case DateHandler.UnixTimeMs:
                    writer.Write(dateTime.ToUnixTimeMs());
                    return;
            }

            writer.Write(DateTimeSerializer.ToShortestXsdDateTimeString(dateTime, Setting.SkipDateTimeConversion));
        }

        public void WriteNullableDateTime(TextWriter writer, object oDateTime)
        {
            if (oDateTime == null)
                return;
            WriteDateTime(writer, oDateTime);
        }

        public void WriteDateTimeOffset(TextWriter writer, object oDateTimeOffset)
        {
            writer.Write(((DateTimeOffset) oDateTimeOffset).ToString("o"));
        }

        public void WriteNullableDateTimeOffset(TextWriter writer, object oDateTimeOffset)
        {
            if (oDateTimeOffset == null)
                return;
            WriteDateTimeOffset(writer, oDateTimeOffset);
        }

        public void WriteTimeSpan(TextWriter writer, object oTimeSpan)
        {
            writer.Write(DateTimeSerializer.ToXsdTimeSpanString((TimeSpan) oTimeSpan));
        }

        public void WriteNullableTimeSpan(TextWriter writer, object oTimeSpan)
        {
            if (oTimeSpan == null)
                return;
            writer.Write(DateTimeSerializer.ToXsdTimeSpanString((TimeSpan?) oTimeSpan));
        }

        public void WriteGuid(TextWriter writer, object oValue)
        {
            if (oValue == null)
                return;
            writer.Write(((Guid) oValue).ToString("N"));
        }

        public void WriteNullableGuid(TextWriter writer, object oValue)
        {
            if (oValue == null)
                return;
            writer.Write(((Guid) oValue).ToString("N"));
        }

        public void WriteBytes(TextWriter writer, object oByteValue)
        {
            if (oByteValue == null)
                return;
            writer.Write(Convert.ToBase64String((byte[]) oByteValue));
        }

        public void WriteChar(TextWriter writer, object charValue)
        {
            if (charValue == null)
                return;
            writer.Write((char) charValue);
        }

        public void WriteByte(TextWriter writer, object byteValue)
        {
            if (byteValue == null)
                return;
            writer.Write((byte) byteValue);
        }

        public void WriteSByte(TextWriter writer, object sbyteValue)
        {
            if (sbyteValue == null)
                return;
            writer.Write((sbyte) sbyteValue);
        }

        public void WriteInt16(TextWriter writer, object intValue)
        {
            if (intValue == null)
                return;
            writer.Write((short) intValue);
        }

        public void WriteUInt16(TextWriter writer, object intValue)
        {
            if (intValue == null)
                return;
            writer.Write((ushort) intValue);
        }

        public void WriteInt32(TextWriter writer, object intValue)
        {
            if (intValue == null)
                return;
            writer.Write((int) intValue);
        }

        public void WriteUInt32(TextWriter writer, object intValue)
        {
            if (intValue == null)
                return;
            writer.Write((uint) intValue);
        }

        public void WriteInt64(TextWriter writer, object longValue)
        {
            if (longValue == null)
                return;
            writer.Write((long) longValue);
        }

        public void WriteUInt64(TextWriter writer, object ulongValue)
        {
            if (ulongValue == null)
                return;
            writer.Write((ulong) ulongValue);
        }

        public void WriteBool(TextWriter writer, object boolValue)
        {
            if (boolValue == null)
                return;
            writer.Write((bool) boolValue);
        }

        public void WriteFloat(TextWriter writer, object floatValue)
        {
            if (floatValue == null)
                return;
            var floatVal = (float) floatValue;

            if (Equals(floatVal, float.MaxValue) || Equals(floatVal, float.MinValue))
                writer.Write(floatVal.ToString("r", CultureInfo.InvariantCulture));
            else
                writer.Write(floatVal.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteDouble(TextWriter writer, object doubleValue)
        {
            if (doubleValue == null)
                return;
            var doubleVal = (double) doubleValue;

            if (Equals(doubleVal, double.MinValue) || Equals(doubleVal, double.MaxValue))
                writer.Write(doubleVal.ToString("r", CultureInfo.InvariantCulture));
            else
                writer.Write(doubleVal.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteDecimal(TextWriter writer, object decimalValue)
        {
            if (decimalValue == null)
                return;
            writer.Write(((decimal) decimalValue).ToString(CultureInfo.InvariantCulture));
        }

        public void WriteEnum(TextWriter writer, object enumValue)
        {
            if (enumValue == null)
                return;
            if (Setting.TreatEnumAsInteger)
                WriteEnumFlags(writer, enumValue);
            else
                writer.Write(enumValue.ToString());
        }

        public void WriteEnumFlags(TextWriter writer, object enumFlagValue)
        {
            if (enumFlagValue == null)
                return;

            var typeCode = Enum.GetUnderlyingType(enumFlagValue.GetType()).GetTypeCode();
            switch (typeCode)
            {
                case TypeCode.SByte:
                    writer.Write((sbyte) enumFlagValue);
                    break;
                case TypeCode.Byte:
                    writer.Write((byte) enumFlagValue);
                    break;
                case TypeCode.Int16:
                    writer.Write((short) enumFlagValue);
                    break;
                case TypeCode.UInt16:
                    writer.Write((ushort) enumFlagValue);
                    break;
                case TypeCode.Int32:
                    writer.Write((int) enumFlagValue);
                    break;
                case TypeCode.UInt32:
                    writer.Write((uint) enumFlagValue);
                    break;
                case TypeCode.Int64:
                    writer.Write((long) enumFlagValue);
                    break;
                case TypeCode.UInt64:
                    writer.Write((ulong) enumFlagValue);
                    break;
                default:
                    writer.Write((int) enumFlagValue);
                    break;
            }
        }
    }
}