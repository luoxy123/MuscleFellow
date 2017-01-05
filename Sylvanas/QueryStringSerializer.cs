using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Sylvanas.Extensions;
using Sylvanas.Text;
using Sylvanas.Utility;

namespace Sylvanas
{
    public static class QueryStringSerializer
    {
        private static readonly ITypeSerializer Serializer = QueryStringWriter.Instance;

        public static string SerializeToString(IDictionary value, QueryStringWriterSetting setting = null)
        {
            var writer = StringWriterThreadStatic.Allocate();
            WriteIDictionary(writer, value, setting);
            return StringWriterThreadStatic.ReturnAndFree(writer);
        }

        private static void WriteIDictionary(TextWriter writer, IDictionary map, QueryStringWriterSetting setting)
        {
            if (setting != null)
                Serializer.Setting = setting;

            var ranOnce = false;
            foreach (var key in map.Keys)
            {
                var dictionaryValue = map[key];
                if (dictionaryValue == null)
                    continue;

                var wirteKeyFn = GetWriteFn(key.GetType());
                var writeValueFn = GetWriteFn(dictionaryValue.GetType());
                if (ranOnce)
                    writer.Write("&");
                else
                    ranOnce = true;

                wirteKeyFn(writer, key);
                writer.Write("=");
                writeValueFn(writer, dictionaryValue);
            }
        }

        private static Action<TextWriter, object> GetWriteFn(Type type)
        {
            if (type == typeof(string))
                return Serializer.WriteObjectString;

            return GetCoreWriteFn(type);
        }

        private static Action<TextWriter, object> GetCoreWriteFn(Type type)
        {
            if (type.GetTypeInfo().IsValueType)
                return GetValueTypeToStringMethod(type);

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (!elementType.GetTypeInfo().IsValueType)
                    throw new NotSupportedException("数组的类型包含了非值类型，当前不受支持");

                if (type == typeof(byte[]))
                    return Serializer.WriteBytes;

                if (type == typeof(string[]))
                    return (w, x) => WriteStringArray(Serializer, w, x);

                return WriteGenericArrayValueType;
            }

            return Serializer.WriteBuiltIn;
        }

        private static void WriteGenericArrayValueType(TextWriter writer, object oArray)
        {
            var enumerable = oArray as IEnumerable;
            if (enumerable != null)
            {
                writer.Write(StringUtility.ListStartChar);
                var ranOnce = false;
                var writeFn = GetValueTypeToStringMethod(enumerable.GetType().GetElementType());

                foreach (var item in enumerable)
                {
                    WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
                    writeFn(writer, item);
                }

                writer.Write(StringUtility.ListEndChar);
            }
        }

        private static void WriteStringArray(ITypeSerializer serializer, TextWriter writer, object oList)
        {
            writer.Write(StringUtility.ListStartChar);

            var list = (string[]) oList;
            var ranOnce = false;
            var listLength = list.Length;
            for (var i = 0; i < listLength; i++)
            {
                WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
                serializer.WriteString(writer, list[i]);
            }

            writer.Write(StringUtility.ListEndChar);
        }

        private static Action<TextWriter, object> GetValueTypeToStringMethod(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            var isNullable = underlyingType != null;
            if (underlyingType == null)
                underlyingType = type;

            if (!underlyingType.GetTypeInfo().IsEnum)
            {
                var typeCode = underlyingType.GetTypeCode();

                if (typeCode == TypeCode.Char)
                    return Serializer.WriteChar;
                if (typeCode == TypeCode.Int32)
                    return Serializer.WriteInt32;
                if (typeCode == TypeCode.Int64)
                    return Serializer.WriteInt64;
                if (typeCode == TypeCode.UInt64)
                    return Serializer.WriteUInt64;
                if (typeCode == TypeCode.UInt32)
                    return Serializer.WriteUInt32;

                if (typeCode == TypeCode.Byte)
                    return Serializer.WriteByte;
                if (typeCode == TypeCode.SByte)
                    return Serializer.WriteSByte;

                if (typeCode == TypeCode.Int16)
                    return Serializer.WriteInt16;
                if (typeCode == TypeCode.UInt16)
                    return Serializer.WriteUInt16;

                if (typeCode == TypeCode.Boolean)
                    return Serializer.WriteBool;

                if (typeCode == TypeCode.Single)
                    return Serializer.WriteFloat;
                if (typeCode == TypeCode.Double)
                    return Serializer.WriteDouble;

                if (typeCode == TypeCode.Decimal)
                    return Serializer.WriteDecimal;

                if (typeCode == TypeCode.DateTime)
                {
                    if (isNullable)
                        return Serializer.WriteNullableDateTime;
                    return Serializer.WriteDateTime;
                }

                if (type == typeof(DateTimeOffset))
                    return Serializer.WriteDateTimeOffset;
                if (type == typeof(DateTimeOffset?))
                    return Serializer.WriteNullableDateTimeOffset;

                if (type == typeof(TimeSpan))
                    return Serializer.WriteTimeSpan;
                if (type == typeof(TimeSpan?))
                    return Serializer.WriteNullableTimeSpan;

                if (type == typeof(Guid))
                    return Serializer.WriteGuid;
                if (type == typeof(Guid?))
                    return Serializer.WriteNullableGuid;
            }
            else
            {
                if (underlyingType.GetTypeInfo().IsEnum)
                    return type.FirstAttribute<FlagsAttribute>() != null
                        ? (Action<TextWriter, object>) Serializer.WriteEnumFlags
                        : Serializer.WriteEnum;
            }

            if (type.HasInterface(typeof(IFormattable)))
                return Serializer.WriteFormattableObjectString;

            return Serializer.WriteObjectString;
        }

        private static void WriteItemSeperatorIfRanOnce(TextWriter writer, ref bool ranOnce)
        {
            if (ranOnce)
                writer.Write(StringUtility.ItemSeperator);
            else
                ranOnce = true;
        }

        public static string ToFormUrlEncoded(this IDictionary map)
        {
            var writer = StringWriterThreadStatic.Allocate();
            var ranOnce = false;

            foreach (var key in map.Keys)
            {
                if (map[key] == null)
                    continue;
                if (ranOnce)
                    writer.Write("&");
                else
                    ranOnce = true;

                var keyType = key.GetType();
                var writeKeyFn = GetWriteFn(keyType);

                var valueType = map[key].GetType();
                var writeFn = GetWriteFn(valueType);

                writeKeyFn(writer, key);
                writer.Write("=");
                writeFn(writer, map[key]);
            }

            return StringWriterThreadStatic.ReturnAndFree(writer);
        }
    }
}