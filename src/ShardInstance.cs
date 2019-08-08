// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Data.Common;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Polly;

namespace ArgentSea
{
    // This file contains the nested ShardInstance class. It is nested because it needs to inherit the generic definitions of its parent.
    public abstract partial class ShardSetsBase<TConfiguration> : ICollection where TConfiguration : class, IShardSetsConfigurationOptions, new()
    {
        /// <summary>
        /// This class represents a distinct shard, or database instance, within the shardset.
        /// </summary>
        public class ShardInstance
        {
            public ShardInstance(ShardSetsBase<TConfiguration> parent, short shardId, IShardConnectionConfiguration shardConnection)
            {
                this.ShardId = shardId;
                var readConnection = shardConnection.ReadConnectionInternal;
                var writeConnection = shardConnection.WriteConnectionInternal;
                if (shardConnection.ReadConnectionInternal is null && !(shardConnection.WriteConnectionInternal is null))
                {
                    readConnection = writeConnection;
                }
                else if (writeConnection is null && !(readConnection is null))
                {
                    writeConnection = readConnection;
                }
                this.Read = new ShardDataConnection(parent, shardId, readConnection);
                this.Write = new ShardDataConnection(parent, shardId, writeConnection);
            }
            public short ShardId { get; }
            public ShardDataConnection Read { get; }
            public ShardDataConnection Write { get; }

        }
    }
}
