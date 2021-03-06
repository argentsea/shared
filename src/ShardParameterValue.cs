﻿// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace ArgentSea
{
    /// <summary>
    /// The class enables passing different parameters to specific shards. Only distinct members of the shard Id list are queried.
    /// </summary>
    public class ShardParameterValue
    {
        public ShardParameterValue()
        {
            ShardId = 0;
            ParameterName = null;
            ParameterValue = null;
        }
        public ShardParameterValue(short shardId, string parameterName, object parameterValue)
        {
            ShardId = shardId;
            ParameterName = parameterName;
            ParameterValue = parameterValue;
        }

        public short ShardId { get; set; }

        public string ParameterName { get; set; }

        public object ParameterValue { get; set; }
    }
}
