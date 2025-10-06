using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ArgentSea.ShardKeys;




/// <summary>
/// A GhostShardKey is for situations where you have ShardKey’s serialzied data, but you do not know the original generics used when it was serialize, and therefore cannot create an instance to deserialize it.
/// </summary>
public class GhostShardKey
{
    private ReadOnlyMemory<byte> _serialization;

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
        var span = _serialization.Span;
        var isUtf8 = ((span[0] & 128) != 128);
        if (isUtf8) // utf8 encoding chars do not use high bits, so it's safe to put.
        {
            span = StringExtensions.Decode(span).Span;
        }
        if (_serialization.Length < 5)
        {
            return null;
        }
        var typeSize = ((int)span[0]) & 3;
        uint intValues = 0;
        switch (typeSize)
        {
            case 1:
                intValues = BitConverter.ToUInt32(new byte[4] { (byte)0, (byte)0, span[1], (byte)0 });
                return MakeShardKeyInstance(intValues);
            case 2:
                intValues = BitConverter.ToUInt32(new byte[4] { (byte)0, span[2], span[1], (byte)0 });
                return MakeShardChildKeyInstance(intValues);
            case 3:
                intValues = BitConverter.ToUInt32(new byte[4] { span[3], span[2], span[1], (byte)0 });
                if ((intValues & 63) == 0)
                {
                    return MakeShardGrandChildKeyInstance(intValues);
                }
                else
                {
                    return MakeShardGreatGrandChildKeyInstance(intValues);

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
        return Activator.CreateInstance(shardDefinition, _serialization);
    }
    private dynamic MakeShardChildKeyInstance(uint typeInfo)
    {
        var generic = typeof(ShardKey<,>);
        var shardDataType = (KeyDataType)((typeInfo >> 18) & 63);
        var shardChildDataType = (KeyDataType)((typeInfo >> 12) & 63);
        var types = new Type[2] { ShardKeySerialization.GetArgType(shardDataType), ShardKeySerialization.GetArgType(shardChildDataType) };
        var shardDefinition = generic.MakeGenericType(types);
        return Activator.CreateInstance(shardDefinition, _serialization);
    }
    private dynamic MakeShardGrandChildKeyInstance(uint typeInfo)
    {
        var generic = typeof(ShardKey<,,>);
        var shardDataType = (KeyDataType)((typeInfo >> 18) & 63);
        var shardChildDataType = (KeyDataType)((typeInfo >> 12) & 63);
        var shardGrandChildDataType = (KeyDataType)((typeInfo >> 6) & 63);
        var types = new Type[3] { ShardKeySerialization.GetArgType(shardDataType), ShardKeySerialization.GetArgType(shardChildDataType), ShardKeySerialization.GetArgType(shardGrandChildDataType) };
        var shardDefinition = generic.MakeGenericType(types);
        return Activator.CreateInstance(shardDefinition, _serialization);
    }

    private dynamic MakeShardGreatGrandChildKeyInstance(uint typeInfo)
    {
        var generic = typeof(ShardKey<,,,>);
        var shardDataType = (KeyDataType)((typeInfo >> 18) & 63);
        var shardChildDataType = (KeyDataType)((typeInfo >> 12) & 63);
        var shardGrandChildDataType = (KeyDataType)((typeInfo >> 6) & 63);
        var shardGreatGrandChildDataType = (KeyDataType)(typeInfo & 63);
        var types = new Type[4] { ShardKeySerialization.GetArgType(shardDataType), ShardKeySerialization.GetArgType(shardChildDataType), ShardKeySerialization.GetArgType(shardGrandChildDataType), ShardKeySerialization.GetArgType(shardGreatGrandChildDataType) };
        var shardDefinition = generic.MakeGenericType(types);
        return Activator.CreateInstance(shardDefinition, _serialization);
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

    public short GetShardId()
    {
        if (_serialization.Length < 5)
        {
            throw new InvalidShardKeyMetadataException();
        }
        var span = _serialization.Span;
        var isUtf8 = ((span[0] & 128) != 128);
        if (isUtf8)
        {
            span = StringExtensions.Decode(span).Span;
        }
        var orgnLen = span[0] & 3;
        var pos = orgnLen + 3;
        return BitConverter.ToInt16(span.Slice(pos));
    }
}
