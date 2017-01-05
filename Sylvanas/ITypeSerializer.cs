using System.IO;
using Sylvanas.Text;

namespace Sylvanas
{
    public interface ITypeSerializer
    {
        bool IncludeNullValues { get; }
        QueryStringWriterSetting Setting { get; set; }

        void WriteBuiltIn(TextWriter writer, object value);
        void WriteRawString(TextWriter writer, string value);
        void WriteObjectString(TextWriter writer, object value);
        void WriteString(TextWriter writer, string value);
        void WriteFormattableObjectString(TextWriter writer, object value);
        void WriteDateTime(TextWriter writer, object oDateTime);
        void WriteNullableDateTime(TextWriter writer, object oDateTime);
        void WriteDateTimeOffset(TextWriter writer, object oDateTimeOffset);
        void WriteNullableDateTimeOffset(TextWriter writer, object oDateTimeOffset);
        void WriteTimeSpan(TextWriter writer, object oTimeSpan);
        void WriteNullableTimeSpan(TextWriter writer, object oTimeSpan);
        void WriteGuid(TextWriter writer, object oValue);
        void WriteNullableGuid(TextWriter writer, object oValue);
        void WriteBytes(TextWriter writer, object oByteValue);
        void WriteChar(TextWriter writer, object charValue);
        void WriteByte(TextWriter writer, object byteValue);
        void WriteSByte(TextWriter writer, object sbyteValue);
        void WriteInt16(TextWriter writer, object intValue);
        void WriteUInt16(TextWriter writer, object intValue);
        void WriteInt32(TextWriter writer, object intValue);
        void WriteUInt32(TextWriter writer, object intValue);
        void WriteInt64(TextWriter writer, object longValue);
        void WriteUInt64(TextWriter writer, object ulongValue);
        void WriteBool(TextWriter writer, object boolValue);
        void WriteFloat(TextWriter writer, object floatValue);
        void WriteDouble(TextWriter writer, object doubleValue);
        void WriteDecimal(TextWriter writer, object decimalValue);
        void WriteEnum(TextWriter writer, object enumValue);
        void WriteEnumFlags(TextWriter writer, object enumFlagValue);
    }
}