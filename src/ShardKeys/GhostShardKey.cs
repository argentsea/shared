using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ArgentSea.ShardKeys
{


    /// <summary>
    /// A GhostShardKey is for situations where you have ShardKey’s serialzied data, but you do not know the original generics used when it was serialize, and therefore cannot create an instance to deserialize it.
    /// </summary>
    public class GhostShardKey
    {
        private readonly ReadOnlyMemory<byte> _serialization;

        /// <summary>
        /// Delibertately lightweight constructor.
        /// Enables high-performance logging with very limited overhead if template is not invoked.
        /// </summary>
        /// <param name="serialization"></param>
        public GhostShardKey(ReadOnlyMemory<byte> serialization)
        {
            _serialization = serialization;
        }

        /// <summary>
        /// Create an instance of the ShareKey from the serialization or null.
        /// </summary>
        /// <returns>The ShardKey,ShardChild, ShardGrandChild, ShardGreatGrandChild, or null.</returns>
        public dynamic GetInstance()
        {
            if (_serialization.Length < 5)
            {
                return null;
            }
            var span = _serialization.Span;
            var typeSize = ((int)span[0]) >> 6;
            uint intValues = 0;
            switch (typeSize)
            {
                case 1:
                    intValues = BitConverter.ToUInt32(new byte[4] { (byte)0, (byte)0, span[1], (byte)0 });
                    var sk = MakeShardKeyInstance(intValues);
                    return sk?.FromArray(_serialization);
                case 2:
                    intValues = BitConverter.ToUInt32(new byte[4] { (byte)0, span[2], span[1], (byte)0 });
                    var sc = MakeShardChildKeyInstance(intValues);
                    return sc?.FromArray(_serialization);
                case 3:
                    intValues = BitConverter.ToUInt32(new byte[4] { span[3], span[2], span[1], (byte)0 });
                    if ((intValues & 63) == 0)
                    {
                        var sgc = MakeShardGrandChildKeyInstance(intValues);
                        return sgc?.FromArray(_serialization);
                    }
                    else
                    {
                        var sggc = MakeShardGreatGrandChildKeyInstance(intValues);
                        return sggc?.FromArray(_serialization);

                    }
                default:
                    return null;
            }
        }
        #region Make Instances
        private dynamic MakeShardKeyInstance(uint typeInfo)
        {
            var generic = typeof(ShardKey<>);
            var shardDataType = (KeyDataType)((typeInfo >> 18) & 63);
            var types = new Type[1] { ShardKeySerialization.GetArgType(shardDataType) };
            var shardDefinition = generic.MakeGenericType(types);
            return Activator.CreateInstance(shardDefinition);
        }
        private dynamic MakeShardChildKeyInstance(uint typeInfo)
        {
            var generic = typeof(ShardKey<>);
            var shardDataType = (KeyDataType)((typeInfo >> 18) & 63);
            var shardChildDataType = (KeyDataType)((typeInfo >> 12) & 63);
            var types = new Type[2] { ShardKeySerialization.GetArgType(shardDataType), ShardKeySerialization.GetArgType(shardChildDataType) };
            var shardDefinition = generic.MakeGenericType(types);
            return Activator.CreateInstance(shardDefinition);
        }
        private dynamic MakeShardGrandChildKeyInstance(uint typeInfo)
        {
            var generic = typeof(ShardKey<>);
            var shardDataType = (KeyDataType)((typeInfo >> 18) & 63);
            var shardChildDataType = (KeyDataType)((typeInfo >> 12) & 63);
            var shardGrandChildDataType = (KeyDataType)((typeInfo >> 6) & 63);
            var types = new Type[3] { ShardKeySerialization.GetArgType(shardDataType), ShardKeySerialization.GetArgType(shardChildDataType), ShardKeySerialization.GetArgType(shardGrandChildDataType) };
            var shardDefinition = generic.MakeGenericType(types);
            return Activator.CreateInstance(shardDefinition);
        }

        private dynamic MakeShardGreatGrandChildKeyInstance(uint typeInfo)
        {
            var generic = typeof(ShardKey<>);
            var shardDataType = (KeyDataType)((typeInfo >> 18) & 63);
            var shardChildDataType = (KeyDataType)((typeInfo >> 12) & 63);
            var shardGrandChildDataType = (KeyDataType)((typeInfo >> 6) & 63);
            var shardGreatGrandChildDataType = (KeyDataType)(typeInfo & 63);
            var types = new Type[4] { ShardKeySerialization.GetArgType(shardDataType), ShardKeySerialization.GetArgType(shardChildDataType), ShardKeySerialization.GetArgType(shardGrandChildDataType), ShardKeySerialization.GetArgType(shardGreatGrandChildDataType) };
            var shardDefinition = generic.MakeGenericType(types);
            return Activator.CreateInstance(shardDefinition);
        }

        #endregion
        public override string ToString()
        {
            var obj = GetInstance();
            if (obj is null)
            {
                return "{ Not a ShardKey }";
            }
            return obj.ToString();
        }



        //private static KeyDataType[] Uncombiner(byte[] value)
        //{
        //    uint intValues = 0;
        //    if (value.Length == 1)
        //    {
        //        intValues = BitConverter.ToUInt32(new byte[4] { (byte)0, (byte)0, value[0], (byte)0 });
        //    }
        //    if (value.Length == 2)
        //    {
        //        intValues = BitConverter.ToUInt32(new byte[4] { (byte)0, value[1], value[0], (byte)0 });
        //    }
        //    if (value.Length == 3)
        //    {
        //        intValues = BitConverter.ToUInt32(new byte[4] { value[2], value[1], value[0], (byte)0 });
        //    }
        //    var result = new KeyDataType[4];
        //    result[0] = (KeyDataType)((intValues >> 18) & 63);
        //    result[1] = (KeyDataType)((intValues >> 12) & 63);
        //    result[2] = (KeyDataType)((intValues >> 6) & 63);
        //    result[3] = (KeyDataType)(intValues & 63);
        //    return result;
        //}

        //public static Type[] DecodeTypeMeta(byte[] value)
        //{
        //    int result1 = 0;
        //    int result2 = 0;
        //    int result3 = 0;
        //    int result4 = 0;
        //    switch (value.Length)
        //    {
        //        case 0:
        //            throw new ArgumentException();
        //        case 1:
        //            result1 = (int)value[0] >> 2;

        //            break;
        //        case 2:
        //            result1 = (int)value[0] >> 2;
        //            result2 = ((int)(value[0] & 3) << 4) | ((int)value[1] >> 4);
        //            break;
        //        case 3:
        //            result1 = (int)value[0] >> 2;
        //            result2 = ((int)(value[0] & 3) << 4) | ((int)value[1] >> 4);
        //            result3 = (((int)value[1]) & 15 << 2) | ((int)value[2] >> 6);
        //            result4 = (int)value[2] & 63;
        //            break;
        //        default:
        //            throw new ArgumentException();
        //    }
        //}

        //public static bool IsValidKeyDataType(Type candidateType)
        //{
        //    if (candidateType == typeof(Guid))
        //    {
        //        return true;
        //    }
        //    if (candidateType == typeof(byte[]))
        //    {
        //        return true;
        //    }
        //    if (candidateType == typeof(int))
        //    {
        //        return true;
        //    }
        //    if (candidateType == typeof(byte))
        //    {
        //        return true;
        //    }
        //    if (candidateType == typeof(short))
        //    {
        //        return true;
        //    }
        //    if (candidateType == typeof(long))
        //    {
        //        return true;
        //    }
        //    if (candidateType == typeof(Char))
        //    {
        //        return true;
        //    }
        //    if (candidateType == typeof(bool))
        //    {
        //        return true;
        //    }
        //    if (candidateType == typeof(decimal))
        //    {
        //        return true;
        //    }
        //    if (candidateType == typeof(double))
        //    {
        //        return true;
        //    }
        //    if (candidateType == typeof(float))
        //    {
        //        return true;
        //    }
        //    if (candidateType == typeof(DateTime))
        //    {
        //        return true;
        //    }
        //    if (candidateType == typeof(DateOnly))
        //    {
        //        return true;
        //    }
        //    if (candidateType == typeof(string))
        //    {
        //        return true;
        //    }
        //    if (candidateType == typeof(DateTimeOffset))
        //    {
        //        return true;
        //    }
        //    if (candidateType == typeof(TimeSpan))
        //    {
        //        return true;
        //    }
        //    if (candidateType.IsEnum)
        //    {
        //        var baseType = candidateType.GetEnumUnderlyingType();
        //        if (baseType == typeof(int))
        //        {
        //            return true;
        //        }
        //        if (baseType == typeof(byte))
        //        {
        //            return true;
        //        }
        //        if (baseType == typeof(short))
        //        {
        //            return true;
        //        }
        //        if (baseType == typeof(long))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}


    }
    //public enum KeyDateType2
    //{
    //    Int32,
    //    Byte,
    //    Int16,
    //    Int64,
    //    //Int128,
    //    Char,
    //    Booliean,
    //    Decimal,
    //    Double,
    //    Float,
    //    //UInt32,
    //    //UInt16,
    //    //UInt64,
    //    //UInt128,
    //    //SByte,
    //    DateTime,
    //    DateOnly,
    //    //           TimeOnly,
    //    String,
    //    DateTimeOffset,
    //    TimeSpan,
    //    Guid,
    //    //Half,
    //    //Span?
    //    JsonDocument
    //    //array?
    //}
    ////public static bool IsValidShardKeyType(object candidate)
    ////{
    ////    switch (candidate)
    ////    {
    ////        case int _:
    ////            return true;
    ////        case long _:
    ////            return true;
    ////        default:
    ////            return false;
    ////    }

    ////    return true;
    ////}
}
