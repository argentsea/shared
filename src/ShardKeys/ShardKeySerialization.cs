using ArgentSea.ShardKeys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArgentSea;

internal enum KeyDataType : byte
{
    Undefined = 0,
    Guid,
    Blob,
    Int32,
    Byte,
    Int16,
    Int64,
    Char,
    Decimal,
    Double,
    Float,
    DateTime,
    DateOnly,
    String,
    TimeOnly,
    TimeSpan,
    Int32Enum,
    ByteEnum,
    Int16Enum,
    Int64Enum
    // 6 bit range allows max 64 values
}

internal static class ShardKeySerialization
{
    internal static KeyDataType GetKeyDataType(Type candidateType)
    {
        if (candidateType == typeof(Guid))
        {
            return KeyDataType.Guid;
        }
        if (candidateType == typeof(byte[]))
        {
            return KeyDataType.Blob;
        }
        if (candidateType == typeof(int))
        {
            return KeyDataType.Int32;
        }
        if (candidateType == typeof(byte))
        {
            return KeyDataType.Byte;
        }
        if (candidateType == typeof(short))
        {
            return KeyDataType.Int16;
        }
        if (candidateType == typeof(long))
        {
            return KeyDataType.Int64;
        }
        if (candidateType == typeof(Char))
        {
            return KeyDataType.Char;
        }
        if (candidateType == typeof(decimal))
        {
            return KeyDataType.Decimal;
        }
        if (candidateType == typeof(double))
        {
            return KeyDataType.Double;
        }
        if (candidateType == typeof(float))
        {
            return KeyDataType.Float;
        }
        if (candidateType == typeof(DateTime))
        {
            return KeyDataType.DateTime;
        }
        if (candidateType == typeof(DateOnly))
        {
            return KeyDataType.DateOnly;
        }
        if (candidateType == typeof(string))
        {
            return KeyDataType.String;
        }
        if (candidateType == typeof(TimeSpan))
        {
            return KeyDataType.TimeSpan;
        }
        if (candidateType.IsEnum)
        {
            var baseType = candidateType.GetEnumUnderlyingType();
            if (baseType == typeof(int))
            {
                return KeyDataType.Int32Enum;
            }
            if (baseType == typeof(byte))
            {
                return KeyDataType.ByteEnum;
            }
            if (baseType == typeof(short))
            {
                return KeyDataType.Int16Enum;
            }
            if (baseType == typeof(long))
            {
                return KeyDataType.Int64Enum;
            }
        }
        return KeyDataType.Undefined;
    }

    //private static int GetKeyDataSize(Type candidateType)
    //{
    //    if (candidateType == typeof(Guid))
    //    {
    //        return 16;
    //    }
    //    if (candidateType == typeof(byte[]))
    //    {
    //        return 0; // Todo
    //    }
    //    if (candidateType == typeof(int))
    //    {
    //        return 4;
    //    }
    //    if (candidateType == typeof(byte))
    //    {
    //        return 1;
    //    }
    //    if (candidateType == typeof(short))
    //    {
    //        return 2;
    //    }
    //    if (candidateType == typeof(long))
    //    {
    //        return 8;
    //    }
    //    if (candidateType == typeof(Char))
    //    {
    //        return 2;
    //    }
    //    if (candidateType == typeof(decimal))
    //    {
    //        return 16;
    //    }
    //    if (candidateType == typeof(double))
    //    {
    //        return 8;
    //    }
    //    if (candidateType == typeof(float))
    //    {
    //        return 4;
    //    }
    //    if (candidateType == typeof(DateTime))
    //    {
    //        return 8;
    //    }
    //    if (candidateType == typeof(DateOnly))
    //    {
    //        return 4;
    //    }
    //    if (candidateType == typeof(string))
    //    {
    //        return KeyDataType.String;
    //    }
    //    if (candidateType == typeof(TimeSpan))
    //    {
    //        return 8;
    //    }
    //    if (candidateType.IsEnum)
    //    {
    //        var baseType = candidateType.GetEnumUnderlyingType();
    //        if (baseType == typeof(int))
    //        {
    //            return 4;
    //        }
    //        if (baseType == typeof(byte))
    //        {
    //            return 1;
    //        }
    //        if (baseType == typeof(short))
    //        {
    //            return 2;
    //        }
    //        if (baseType == typeof(long))
    //        {
    //            return 8;
    //        }
    //    }
    //    return 0;
    //}

    /// <summary>
    /// Returns an array of type metadata for encoding into a ShardKey&lt;T&gt;.
    /// </summary>
    /// <returns>Byte[1]</returns>
    internal static bool TryEncodeTypeMetadata(Type keyType, out ReadOnlyMemory<byte> result)
    {
        var key1 = GetKeyDataType(keyType);
        var combined = Combiner(key1, KeyDataType.Undefined, KeyDataType.Undefined, KeyDataType.Undefined);
        result = new byte[1] { combined[2] };
        return key1 != KeyDataType.Undefined;
    }

    /// <summary>
    /// Returns an array of type metadata for encoding into a ShardKey&lt;T1, T2&gt;.
    /// </summary>
    /// <returns>Byte[2]</returns>
    internal static bool TryEncodeTypeMetadata(Type keyType, Type childKeyType, out ReadOnlyMemory<byte> result)
    {
        var key1 = GetKeyDataType(keyType);
        var key2 = GetKeyDataType(childKeyType);
        var combined = Combiner(key1, key2, KeyDataType.Undefined, KeyDataType.Undefined);
        result = new byte[2] { combined[2], combined[1] };
        return key1 != KeyDataType.Undefined && key2 != KeyDataType.Undefined;
        ;
    }

    /// <summary>
    /// Returns an array of type metadata for encoding into a ShardKey&lt;T1, T2, T3&gt;.
    /// </summary>
    /// <returns>Byte[3]</returns>
    internal static bool TryEncodeTypeMetadata(Type keyType, Type childKeyType, Type grandchildKeyType, out ReadOnlyMemory<byte> result)
    {
        var key1 = GetKeyDataType(keyType);
        var key2 = GetKeyDataType(childKeyType);
        var key3 = GetKeyDataType(grandchildKeyType);
        var combined = Combiner(key1, key2, key3, KeyDataType.Undefined);
        result = new byte[3] { combined[2], combined[1], combined[0] };
        return key1 != KeyDataType.Undefined && key2 != KeyDataType.Undefined && key3 != KeyDataType.Undefined; ;
    }

    /// <summary>
    /// Returns an array of type metadata for encoding into a ShardKey&lt;T1, T2, T3&gt;.
    /// </summary>
    /// <returns>Byte[3]</returns>
    internal static bool TryEncodeTypeMetadata(Type keyType, Type childKeyType, Type grandchildKeyType, Type greatgrandchildKeyType, out ReadOnlyMemory<byte> result)
    {
        var key1 = GetKeyDataType(keyType);
        var key2 = GetKeyDataType(childKeyType);
        var key3 = GetKeyDataType(grandchildKeyType);
        var key4 = GetKeyDataType(greatgrandchildKeyType);
        var combined = Combiner(key1, key2, key3, key4);
        result = new byte[3] { combined[2], combined[1], combined[0] };
        return key1 != KeyDataType.Undefined && key2 != KeyDataType.Undefined && key3 != KeyDataType.Undefined && key4 != KeyDataType.Undefined;
    }
    private static byte[] Combiner(KeyDataType keyType, KeyDataType childKeyType, KeyDataType grandchildKeyType, KeyDataType greatgrandchildKeyType)
    {
        var value1 = ((uint)keyType) << 18;
        var value2 = ((uint)childKeyType) << 12;
        var value3 = ((uint)grandchildKeyType) << 6;
        var value4 = (uint)greatgrandchildKeyType;
        var intCombined = value1 | value2 | value3 | value4;
        return BitConverter.GetBytes(intCombined);
    }

    internal static void ThrowIfMetadataMismatch(ReadOnlySpan<byte> metadata, Type keyType)
    {
        if (!TryEncodeTypeMetadata(keyType, out var meta))
        {
            throw new InvalidShardKeyMetadataException(keyType);
        }
        var saved = meta.Span;
        if (metadata.Length != 1 && saved.Length != 1)
        {
            throw new InvalidShardKeyMetadataException();
        }
        if (metadata[0] != saved[0])
        {
            throw new InvalidShardKeyMetadataException(keyType);
        }
    }
    internal static void ThrowIfMetadataMismatch(ReadOnlySpan<byte> metadata, Type keyType, Type childType)
    {
        if (!TryEncodeTypeMetadata(keyType, childType, out var meta))
        {
            throw new InvalidShardKeyMetadataException(keyType);
        }
        var saved = meta.Span;
        if (metadata.Length != 2 && saved.Length != 2)
        {
            throw new InvalidShardKeyMetadataException();
        }
        if (metadata[0] != saved[0])
        {
            throw new InvalidShardKeyMetadataException(keyType);
        }
        if (metadata[1] != saved[1])
        {
            throw new InvalidShardKeyMetadataException(childType);
        }
    }
    internal static void ThrowIfMetadataMismatch(ReadOnlySpan<byte> metadata, Type keyType, Type childType, Type grandchildType)
    {
        if (!TryEncodeTypeMetadata(keyType, childType, grandchildType, out var meta))
        {
            throw new InvalidShardKeyMetadataException(keyType);
        }
        var saved = meta.Span;
        if (metadata.Length != 3 && saved.Length != 3)
        {
            throw new InvalidShardKeyMetadataException();
        }
        if (metadata[0] != saved[0])
        {
            throw new InvalidShardKeyMetadataException(keyType);
        }
        if (metadata[1] != saved[1])
        {
            throw new InvalidShardKeyMetadataException(childType);
        }
        if (metadata[2] != saved[2])
        {
            throw new InvalidShardKeyMetadataException(grandchildType);
        }
    }

    internal static void ThrowIfMetadataMismatch(ReadOnlySpan<byte> metadata, Type keyType, Type childType, Type grandchildType, Type greatgrandchildType)
    {
        if (!TryEncodeTypeMetadata(keyType, childType, grandchildType, greatgrandchildType, out var meta))
        {
            throw new InvalidShardKeyMetadataException(keyType);
        }
        var saved = meta.Span;
        if (metadata.Length != 3 && saved.Length != 3)
        {
            throw new InvalidShardKeyMetadataException();
        }
        if (metadata[0] != saved[0])
        {
            throw new InvalidShardKeyMetadataException(keyType);
        }
        if (metadata[1] != saved[1])
        {
            throw new InvalidShardKeyMetadataException(childType);
        }
        if (metadata[2] != saved[2])
        {
            throw new InvalidShardKeyMetadataException(grandchildType);
        }
    }

    internal static Type GetArgType(KeyDataType typeEnum)
    {
        switch (typeEnum)
        {
            case KeyDataType.Guid:
                return typeof(Guid);
            case KeyDataType.Blob:
                return typeof(Array);
            case KeyDataType.Int32:
                return typeof(int);
            case KeyDataType.Byte:
                return typeof(byte);
            case KeyDataType.Int16:
                return typeof(short);
            case KeyDataType.Int64:
                return typeof(long);
            case KeyDataType.Char:
                return typeof(char);
            case KeyDataType.Decimal:
                return typeof(decimal);
            case KeyDataType.Double:
                return typeof(double);
            case KeyDataType.Float:
                return typeof(float);
            case KeyDataType.DateTime:
                return typeof(DateTime);
            case KeyDataType.DateOnly:
                return typeof(DateOnly);
            case KeyDataType.String:
                return typeof(string);
            case KeyDataType.TimeSpan:
                return typeof(TimeSpan);
            case KeyDataType.Int32Enum:
                return typeof(int);
            case KeyDataType.ByteEnum:
                return typeof(byte);
            case KeyDataType.Int16Enum:
                return typeof(short);
            case KeyDataType.Int64Enum:
                return typeof(long);
            default: return null;
        }

    }

    private static bool TryConvertType<T>(ref ReadOnlySpan<byte> data, ref int position, int length, Func<ReadOnlySpan<byte>, T> converter, out T result)
    {
        if (data.Length < length)
        {
            result = default;
            return false;
        }
        position += length;
        result = converter(data.Slice(position - length));
        return true;
    }

    internal static bool TryConvertFromBytes(ref ReadOnlySpan<byte> data, ref int position, Type valueType, out dynamic result)
    {
        var success = false;
        result = default;
        if (valueType == typeof(Guid))
        {
            if (data.Length >= 16)
            {
                result = new Guid(data.Slice(position));
                position += 16;
                success = true;
            }
            else
            {
                result = Guid.Empty;
            }
        }
        else if (valueType == typeof(int))
        {
            success = TryConvertType(ref data, ref position, 4, BitConverter.ToInt32, out int converted);
            result = converted;
        }
        else if (valueType == typeof(long))
        {
            success = TryConvertType(ref data, ref position, 8, BitConverter.ToInt64, out long converted);
            result = converted;
        }
        else if (valueType == typeof(byte))
        {
            position += 1;
            result = data[position - 1];
            result = true;
        }
        else if (valueType == typeof(short))
        {
            success = TryConvertType(ref data, ref position, 2, BitConverter.ToInt16, out short converted);
            result = converted;
        }
        else if (valueType == typeof(char))
        {
            success = TryConvertType(ref data, ref position, 2, BitConverter.ToChar, out char converted);
            result = converted;
        }
        else if (valueType == typeof(decimal))
        {
            if (data.Length >= 16)
            {
                var i0 = BitConverter.ToInt32(data.Slice(position));
                var i1 = BitConverter.ToInt32(data.Slice(position + 4));
                var i2 = BitConverter.ToInt32(data.Slice(position + 8));
                var i3 = BitConverter.ToInt32(data.Slice(position + 12));
                position += 16;
                success = true;
                result = (dynamic)new Decimal(new int[] { i0, i1, i2, i3 });
            }
            else
            {
                result = decimal.Zero;
            }
        }
        else if (valueType == typeof(double))
        {
            success = TryConvertType(ref data, ref position, 8, BitConverter.ToDouble, out double converted);
            result = converted;
        }
        else if (valueType == typeof(float))
        {
            success = TryConvertType(ref data, ref position, 4, BitConverter.ToSingle, out float converted);
        }
        else if (valueType == typeof(DateTime))
        {
            success = TryConvertType(ref data, ref position, 8, BitConverter.ToInt64, out long converted);
            result = new DateTime(converted);
        }
        else if (valueType == typeof(DateOnly))
        {
            success = TryConvertType(ref data, ref position, 4, BitConverter.ToInt32, out int converted);
            result = DateOnly.FromDayNumber(converted);
        }
        else if (valueType == typeof(TimeOnly))
        {
            success = TryConvertType(ref data, ref position, 8, BitConverter.ToInt64, out long converted);
            result = new TimeOnly(converted);
        }
        else if (valueType == typeof(TimeSpan))
        {
            success = TryConvertType(ref data, ref position, 8, BitConverter.ToInt64, out long converted);
            result = new TimeSpan(converted);
        }
        else if (valueType == typeof(string))
        {
            if (data[position] == 0)
            {
                position += 1;
                result = string.Empty;
                return true;
            }
            var len = (int)data[position] >> 1;
            position += len + 1;
            result = System.Text.Encoding.UTF8.GetString(data.Slice(position - len, len));
            success = true;
        }
        else if (valueType == typeof(byte[]))
        {
            if (data[position] == 0)
            {
                position += 1;
                result = (byte[])null;
                return true;
            }
            var len = (int)data[position] >> 1;
            position += len + 1;
            result = data.Slice(position - len, len).ToArray();
            success = true;
        }
        else if (valueType == typeof(Memory<byte>))
        {
            if (data[position] == 0)
            {
                position += 1;
                result = Memory<byte>.Empty;
                return true;
            }
            var len = (int)data[position] >> 1;
            position += len + 1;
            result = new Memory<byte>(data.Slice(position - len, len).ToArray());
            success = true;
        }
        else if (valueType == typeof(ReadOnlyMemory<byte>))
        {
            if (data[position] == 0)
            {
                position += 1;
                result = ReadOnlyMemory<byte>.Empty;
                return true;
            }
            var len = (int)data[position] >> 1;
            position += len + 1;
            result = new ReadOnlyMemory<byte>(data.Slice(position - len, len).ToArray());
            success = true;
        }
        //else if (valueType == typeof(Half))
        //{
        //    success = TryConvertType(ref data, ref position, 1, BitConverter.ToHalf, out Half converted);
        //    result = converted;
        //}
        //else if (valueType == typeof(Int128))
        //{
        //    success = TryConvertType(ref data, ref position, 1, BitConverter.ToInt128, out Int128 converted);
        //    result = converted;
        //}
        //else if (valueType == typeof(UInt128))
        //{
        //    success = TryConvertType(ref data, ref position, 1, BitConverter.ToUInt128, out UInt128 converted);
        //    result = converted;
        //}
        else if (valueType == typeof(Enum))
        {
            var baseType = Enum.GetUnderlyingType(valueType);
            success = TryConvertFromBytes(ref data, ref position, baseType, out result);
        }
        else if (valueType == typeof(Nullable))
        {
            var isNull = BitConverter.ToBoolean(data.Slice(position));
            position += 1;
            if (isNull)
            {
                result = null;
                success = true;
            }
            var baseType = Nullable.GetUnderlyingType(valueType);
            result = TryConvertFromBytes(ref data, ref position, baseType, out result);
        }
        return success;
    }

    internal static byte[] GetValueBytes(IComparable value)
    {
        switch (value)
        {
            case int i:
                return BitConverter.GetBytes(i);
            case byte b:
                return new byte[1] { b };
            case short s:
                return BitConverter.GetBytes(s);
            case long l:
                return BitConverter.GetBytes(l);
            case char c:
                return BitConverter.GetBytes(c);
            case decimal d:
                var aid = Decimal.GetBits(d);
                var i0 = BitConverter.GetBytes(aid[0]);
                var i1 = BitConverter.GetBytes(aid[1]);
                var i2 = BitConverter.GetBytes(aid[2]);
                var i3 = BitConverter.GetBytes(aid[3]);
                return new byte[16] { i0[0], i0[1], i0[2], i0[3], i1[0], i1[1], i1[2], i1[3], i2[0], i2[1], i2[2], i2[3], i3[0], i3[1], i3[2], i3[3] };
            case double o:
                return BitConverter.GetBytes(o);
            case float f:
                return BitConverter.GetBytes(f);
            case uint ui:
                return BitConverter.GetBytes(ui);
            case ulong ul:
                return BitConverter.GetBytes(ul);
            case ushort us:
                return BitConverter.GetBytes(us);
            case sbyte sb:
                return new byte[1] { (byte)((int)sb + 128) };
            //case bool bln:
            //    return BitConverter.GetBytes(bln);
            case DateTime dt:
                return BitConverter.GetBytes(dt.Ticks);
            case string str:
                if (str is null)
                {
                    return new byte[1] { 0 };
                }
                else
                {
                    var aStr = System.Text.Encoding.UTF8.GetBytes(str);
                    if (aStr.Length > 128)
                    {
                        throw new Exception("Shard values cannot serialize strings longer than 128 bytes.");
                    }
                    var aResult = new byte[aStr.Length + 1];
                    aResult[0] = Convert.ToByte((aStr.Length << 1) + 1);
                    aStr.CopyTo(aResult, 1);
                    return aResult;
                }
            case Enum e:
                var type = Enum.GetUnderlyingType(value.GetType());
                var newValue = Convert.ChangeType(value, type);
                return ShardKeySerialization.GetValueBytes(newValue as IComparable);
            case DateTimeOffset dto:
                var adt = BitConverter.GetBytes(dto.Ticks);
                var tsp = BitConverter.GetBytes(dto.Offset.Ticks);
                return new byte[] { adt[0], adt[1], adt[2], adt[3], adt[4], adt[5], adt[6], adt[7], tsp[0], tsp[1], tsp[2], tsp[3], tsp[4], tsp[5], tsp[6], tsp[7] };
            case TimeSpan ts:
                return BitConverter.GetBytes(ts.Ticks);
            case Guid g:
                return g.ToByteArray();
            case Half h:
                return BitConverter.GetBytes(h);
            case Int128 i128:
                return BitConverter.GetBytes(i128);
            case UInt128 u128:
                return BitConverter.GetBytes(u128);
            //case null:
            //    return new byte[0];
            default:
                var tValue = value.GetType();
                if (tValue.IsGenericType && Nullable.GetUnderlyingType(tValue) != null)
                {
                    if (value == null)
                    {
                        return BitConverter.GetBytes(true);
                    }
                    var shdType = Nullable.GetUnderlyingType(value.GetType());
                    var nonNullValue = Convert.ChangeType(value, shdType);
                    var aVal = GetValueBytes(nonNullValue as IComparable);
                    var valResult = new byte[1 + aVal.Length];
                    valResult[0] = BitConverter.GetBytes(false)[0];
                    aVal.CopyTo(valResult, 1);
                    return valResult;
                }
                else
                {
                    throw new Exception("Cannot serialize this type.");
                }
        }
    }
}