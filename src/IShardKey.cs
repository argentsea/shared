using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    public interface IShardKey
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
    }
    }
