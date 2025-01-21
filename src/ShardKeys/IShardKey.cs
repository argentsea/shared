using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace ArgentSea
{
    public interface IShardKey : ISerializable
    {
        short ShardId
        {
            get;
        }
        char Origin
        {
            get;
        }
        bool IsEmpty
        {
            get;
        }
        string ToExternalString();

        byte[] ToArray();

        void ThrowIfInvalidOrigin(char expectedOrigin);

    }
}
