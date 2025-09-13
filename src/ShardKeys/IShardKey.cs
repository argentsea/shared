using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace ArgentSea
{
    public interface IShardKey
    {
        short ShardId
        {
            get;
        }
        bool IsEmpty
        {
            get;
        }

        string ToExternalString();

        ReadOnlyMemory<byte> ToArray();

        ReadOnlyMemory<byte> ToUtf8();

    }
}
