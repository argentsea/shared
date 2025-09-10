//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using static ArgentSea.ShardKeys.ShardKeyComponent;
//using static System.Runtime.InteropServices.JavaScript.JSType;

//namespace ArgentSea.ShardKeys
//{
//    public enum KeyDataType 
//    {
//        Undefined = 0,
//        Guid,
//        Array,
//        Int32,
//        Byte,
//        Int16,
//        Int64,
//        Char,
//        Booliean,
//        Decimal,
//        Double,
//        Float,
//        DateTime,
//        DateOnly,
//        String,
//        DateTimeOffset,
//        TimeSpan,
//        Int32Enum,
//        ByteEnum,
//        Int16Enum,
//        Int64Enum
//        // 6 bit range allows max 64 values
//    }


//    public static class ShardKeyComponent
//    {
//        public static object Parse()
//        {
//            Type shardKey = typeof(ShardKey<>);
//            Type shardChild = typeof(ShardKey<,>);
//            Type shardGrandChild = typeof(ShardKey<,,>);
//            Type shardGreatGrandChild = typeof(ShardKey<,,,>);
//            Type[] typeArgs = { typeof(Guid), typeof(int), typeof(string), typeof(long) };

//            Type constructed = shardGreatGrandChild.MakeGenericType(typeArgs);
//            return Activator.CreateInstance(constructed);


//        }
//        public static byte[] EncodeTypeMeta(Type type1)
//        {
//        }

//        public static Type[] DecodeTypeMeta(byte[] value)
//        {
//            int result1 = 0;
//            int result2 = 0;
//            int result3 = 0;
//            int result4 = 0;
//            switch (value.Length)
//            {
//                case 0:
//                    throw new ArgumentException();
//                case 1:
//                    result1 = (int)value[0] >> 2;

//                    break;
//                case 2:
//                    result1 = (int)value[0] >> 2;
//                    result2 = ((int)(value[0] & 3) << 4) | ((int)value[1] >> 4);
//                    break;
//                case 3:
//                    result1 = (int)value[0] >> 2;
//                    result2 = ((int)(value[0] & 3) << 4) | ((int)value[1] >> 4);
//                    result3 = (((int)value[1]) & 15 << 2) | ((int)value[2] >> 6);
//                    result4 = (int)value[2] & 63;
//                    break;
//                default:
//                    throw new ArgumentException();
//            }
//        }


//        public static KeyDataType GetKeyDataType(Type candidateType)
//        {
//            if (candidateType == typeof(Guid))
//            {
//                return KeyDataType.Guid;
//            }
//            if (candidateType == typeof(byte[]))
//            {
//                return KeyDataType.Array;
//            }
//            if (candidateType == typeof(int))
//            {
//                return KeyDataType.Int32;
//            }
//            if (candidateType == typeof(byte))
//            {
//                return KeyDataType.Byte;
//            }
//            if (candidateType == typeof(short))
//            {
//                return KeyDataType.Int16;
//            }
//            if (candidateType == typeof(long))
//            {
//                return KeyDataType.Int64;
//            }
//            if (candidateType == typeof(Char))
//            {
//                return KeyDataType.Char;
//            }
//            if (candidateType == typeof(bool))
//            {
//                return KeyDataType.Booliean;
//            }
//            if (candidateType == typeof(decimal))
//            {
//                return KeyDataType.Decimal;
//            }
//            if (candidateType == typeof(double))
//            {
//                return KeyDataType.Double;
//            }
//            if (candidateType == typeof(float))
//            {
//                return KeyDataType.Float;
//            }
//            if (candidateType == typeof(DateTime))
//            {
//                return KeyDataType.DateTime;
//            }
//            if (candidateType == typeof(DateOnly))
//            {
//                return KeyDataType.DateOnly;
//            }
//            if (candidateType == typeof(string))
//            {
//                return KeyDataType.String;
//            }
//            if (candidateType == typeof(DateTimeOffset))
//            {
//                return KeyDataType.DateTimeOffset;
//            }
//            if (candidateType == typeof(TimeSpan))
//            {
//                return KeyDataType.TimeSpan;
//            }
//            if (candidateType.IsEnum)
//            {
//                var baseType = candidateType.GetEnumUnderlyingType();
//                if (baseType == typeof(int))
//                {
//                    return KeyDataType.Int32Enum;
//                }
//                if (baseType == typeof(byte))
//                {
//                    return KeyDataType.ByteEnum;
//                }
//                if (baseType == typeof(short))
//                {
//                    return KeyDataType.Int16Enum;
//                }
//                if (baseType == typeof(long))
//                {
//                    return KeyDataType.Int64Enum;
//                }
//            }
//            return KeyDataType.Undefined;
//        }

//        public static bool IsValidKeyDataType(Type candidateType)
//        {
//            if (candidateType == typeof(Guid))
//            {
//                return true;
//            }
//            if (candidateType == typeof(byte[]))
//            {
//                return true;
//            }
//            if (candidateType == typeof(int))
//            {
//                return true;
//            }
//            if (candidateType == typeof(byte))
//            {
//                return true;
//            }
//            if (candidateType == typeof(short))
//            {
//                return true;
//            }
//            if (candidateType == typeof(long))
//            {
//                return true;
//            }
//            if (candidateType == typeof(Char))
//            {
//                return true;
//            }
//            if (candidateType == typeof(bool))
//            {
//                return true;
//            }
//            if (candidateType == typeof(decimal))
//            {
//                return true;
//            }
//            if (candidateType == typeof(double))
//            {
//                return true;
//            }
//            if (candidateType == typeof(float))
//            {
//                return true;
//            }
//            if (candidateType == typeof(DateTime))
//            {
//                return true;
//            }
//            if (candidateType == typeof(DateOnly))
//            {
//                return true;
//            }
//            if (candidateType == typeof(string))
//            {
//                return true;
//            }
//            if (candidateType == typeof(DateTimeOffset))
//            {
//                return true;
//            }
//            if (candidateType == typeof(TimeSpan))
//            {
//                return true;
//            }
//            if (candidateType.IsEnum)
//            {
//                var baseType = candidateType.GetEnumUnderlyingType();
//                if (baseType == typeof(int))
//                {
//                    return true;
//                }
//                if (baseType == typeof(byte))
//                {
//                    return true;
//                }
//                if (baseType == typeof(short))
//                {
//                    return true;
//                }
//                if (baseType == typeof(long))
//                {
//                    return true;
//                }
//            }
//            return false;
//        }


//    }
//    public enum KeyDateType2
//    {
//        Int32,
//        Byte,
//        Int16,
//        Int64,
//        //Int128,
//        Char,
//        Booliean,
//        Decimal,
//        Double,
//        Float,
//        //UInt32,
//        //UInt16,
//        //UInt64,
//        //UInt128,
//        //SByte,
//        DateTime,
//        DateOnly,
//        //           TimeOnly,
//        String,
//        DateTimeOffset,
//        TimeSpan,
//        Guid,
//        //Half,
//        //Span?
//        JsonDocument
//        //array?
//    }
//    //public static bool IsValidShardKeyType(object candidate)
//    //{
//    //    switch (candidate)
//    //    {
//    //        case int _:
//    //            return true;
//    //        case long _:
//    //            return true;
//    //        default:
//    //            return false;
//    //    }

//    //    return true;
//    //}
//}
