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
    // This file contains the nested ShardSet class. It is nested because it needs to inherit the generic definitions of its parent.
    public abstract partial class ShardSetsBase<TShard, TConfiguration> : ICollection where TShard : IComparable where TConfiguration : class, IShardSetsConfigurationOptions<TShard>, new()
    {
        /// <summary>
        /// This collection represents a complete shard set. Typically all databases within a shard set have nearly identical schemas.
        /// </summary>
        public class ShardSet : ICollection
        {
            private readonly object syncRoot = new Lazy<object>();
            private readonly ImmutableDictionary<TShard, ShardInstance> dtn;
            private TShard _defaultShardId;
            public ShardSet(ShardSetsBase<TShard, TConfiguration> parent, IShardSetConnectionsConfiguration<TShard> config)
            {
                var bdr = ImmutableDictionary.CreateBuilder<TShard, ShardInstance>();
                _defaultShardId = config.DefaultShardId;
                foreach (var shd in config.ShardsConfigInternal)
                {
                    if (shd is null)
                    {
                        throw new Exception($"A shard set’s connection configuration was not valid; the configuration provider returned null.");
                    }
                    var shardSetsConfig = config as DataConnectionConfigurationBase;
                    var shardConfig = shd as DataConnectionConfigurationBase;
                    var readConfig = config.ReadConfigInternal as DataConnectionConfigurationBase;
                    var writeConfig = config.WriteConfigInternal as DataConnectionConfigurationBase;
                    shd.ReadConnectionInternal.SetAmbientConfiguration(parent._globalConfiguration, shardSetsConfig, readConfig, shardConfig);
                    shd.WriteConnectionInternal.SetAmbientConfiguration(parent._globalConfiguration, shardSetsConfig, writeConfig, shardConfig);
                    bdr.Add(shd.ShardId, new ShardInstance(parent, shd.ShardId, shd));
                }
                this.dtn = bdr.ToImmutable();
                this.ReadAll = new ShardSetReadAll(this);
                this.ReadFirst = new ShardSetReadFirst(this);
                this.Write = new ShardSetWrite(this);
            }

            public string Key { get; set; }

            public ShardInstance this[TShard shardId]
            {
                get { return dtn[shardId]; }
            }
            #region ShardInstance ShardKey overloads
            public ShardInstance this[ShardKey<TShard, byte> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, char> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, DateTime> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, DateTimeOffset> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, decimal> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, double> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, float> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, Guid> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, int> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, long> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, sbyte> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, short> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, string> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, TimeSpan> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, uint> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, ulong> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TShard, ushort> shardKey] => this[shardKey.ShardId];
            #endregion
            #region ShardInstance ShardChild overloads
            public ShardInstance this[ShardChild<TShard, byte, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, byte, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, char, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, char, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, DateTime, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTime, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, DateTimeOffset, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, DateTimeOffset, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, decimal, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, decimal, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, double, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, double, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, float, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, float, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, Guid, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, Guid, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, int, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, int, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, long, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, long, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, sbyte, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, sbyte, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, short, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, short, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, string, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, string, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, TimeSpan, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, TimeSpan, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, uint, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, uint, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, ulong, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ulong, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TShard, ushort, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TShard, ushort, ushort> shardChild] => this[shardChild.ShardId];

            #endregion

            public int Count
            {
                get { return dtn.Count; }
            }
            public ShardInstance DefaultShard => dtn[_defaultShardId];

            public bool IsSynchronized => true;

            public object SyncRoot => syncRoot;

            public void CopyTo(Array array, int index)
                => this.dtn.Values.ToImmutableList().CopyTo((ShardInstance[])array, index);

            public IEnumerator GetEnumerator() => this.dtn.GetEnumerator();

            private Dictionary<TShard, Dictionary<string, object>> ParseShardParameterValues(IEnumerable<ShardParameterValue<TShard>> shardParameterValues)
            {
                var result = new Dictionary<TShard, Dictionary<string, object>>();
                foreach (var spv in shardParameterValues)
                {
                    if (result.TryGetValue(spv.ShardId, out var shardDict))
                    {
                        if (shardDict.ContainsKey(spv.parameterName))
                        {
                            throw new Exception($"Duplicate Shard Parameter value. Parameter {spv.parameterName} already exists on this shard.");
                        }
                        shardDict.Add(spv.parameterName, spv.Value);
                    }
                    else
                    {
                        var newShardDict = new Dictionary<string, object>();
                        newShardDict.Add(spv.parameterName, spv.Value);
                        result.Add(spv.ShardId, newShardDict);
                    }
                }
                return result;
            }
            #region internal ShardSet query methods
            internal async Task<IList<TModel>> ReadQueryAllAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
            {
                var result = new List<TModel>();
                if (string.IsNullOrEmpty(sprocName))
                {
                    throw new ArgumentNullException(nameof(sprocName));
                }
                if (parameters == null)
                {
                    throw new ArgumentNullException(nameof(parameters));
                }
                var shardParameterOrdinal = parameters.GetParameterOrdinal(shardParameterName);
                cancellationToken.ThrowIfCancellationRequested();

                var tsks = new List<Task<TModel>>();
                var cancelTokenSource = new CancellationTokenSource();
                using (var queryCancelationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelTokenSource.Token))
                {
                    if (shardParameterValues is null)
                    {
                        foreach (var shardId in dtn.Keys)
                        {
                            parameters.SetShardId<TShard>(shardParameterOrdinal, shardId);
                            tsks.Add(this.dtn[shardId].Read._manager.QueryAsync<TArg, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, resultHandler, false, dataObject, cancellationToken));
                        }
                    }
                    else
                    {
                        var shardParameters = ParseShardParameterValues(shardParameterValues);
                        foreach (var shardTuple in shardParameters)
                        {
                            parameters.SetShardId<TShard>(shardParameterOrdinal, shardTuple.Key);
                            tsks.Add(this.dtn[shardTuple.Key].Read._manager.QueryAsync<TArg, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), shardTuple.Value, resultHandler, false, dataObject, cancellationToken));
                        }
                    }
                    await Task.WhenAll(tsks).ConfigureAwait(false);
                    foreach (var tsk in tsks)
                    {
                        var tskResult = tsk.Result;
                        if (tskResult != null)
                        {
                            result.Add(tskResult);
                        }
                    }
                }
                return result;
            }

            internal async Task<TModel> ReadQueryFirstAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
            {
                var result = default(TModel);
                if (string.IsNullOrEmpty(sprocName))
                {
                    throw new ArgumentNullException(nameof(sprocName));
                }
                if (parameters == null)
                {
                    throw new ArgumentNullException(nameof(parameters));
                }

                cancellationToken.ThrowIfCancellationRequested();
                var shardParameterOrdinal = parameters.GetParameterOrdinal(shardParameterName);
                var tsks = new List<Task<TModel>>();
                var cancelTokenSource = new CancellationTokenSource();
                using (var queryCancelationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelTokenSource.Token))
                {
                    if (shardParameterValues is null)
                    {
                        foreach (var shardId in dtn.Keys)
                        {
                            parameters.SetShardId<TShard>(shardParameterOrdinal, shardId);
                            tsks.Add(this.dtn[shardId].Read._manager.QueryAsync<TArg, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, resultHandler, false, dataObject, cancellationToken));
                        }
                    }
                    else
                    {
                        var shardParameters = ParseShardParameterValues(shardParameterValues);
                        foreach (var shardTuple in shardParameters)
                        {
                            parameters.SetShardId<TShard>(shardParameterOrdinal, shardTuple.Key);
                            tsks.Add(this.dtn[shardTuple.Key].Read._manager.QueryAsync<TArg, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), shardTuple.Value, resultHandler, false, dataObject, cancellationToken));
                        }
                    }
                    while (tsks.Count > 0)
                    {
                        var tsk = await Task.WhenAny(tsks).ConfigureAwait(false);
                        tsks.Remove(tsk);
                        if (cancellationToken.IsCancellationRequested)
                        {
                            cancelTokenSource.Cancel();
                            break;
                        }
                        try
                        {
                            var tskResult = await tsk.ConfigureAwait(false);
                            if (tskResult != null)
                            {
                                result = tskResult;
                                cancelTokenSource.Cancel();
                                break;
                            }
                        }
                        catch (OperationCanceledException) { }
                        catch (AggregateException) { }
                        catch (Exception err)
                        {
                            throw err;
                        }
                    }
                }
                return result;
            }

            internal async Task<IList<TModel>> MapListAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
            {
                var result = new List<TModel>();
                if (string.IsNullOrEmpty(sprocName))
                {
                    throw new ArgumentNullException(nameof(sprocName));
                }
                if (parameters == null)
                {
                    throw new ArgumentNullException(nameof(parameters));
                }

                cancellationToken.ThrowIfCancellationRequested();
                var shardParameterOrdinal = parameters.GetParameterOrdinal(shardParameterName);
                var tsks = new List<Task<IList<TModel>>>();
                var cancelTokenSource = new CancellationTokenSource();
                using (var queryCancelationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelTokenSource.Token))
                {
                    if (shardParameterValues is null)
                    {
                        foreach (var shardId in dtn.Keys)
                        {
                            parameters.SetShardId<TShard>(shardParameterOrdinal, shardId);
                            tsks.Add(this.dtn[shardId].Read._manager.ListAsync<TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken));
                        }
                    }
                    else
                    {
                        var shardParameters = ParseShardParameterValues(shardParameterValues);
                        foreach (var shardTuple in shardParameters)
                        {
                            parameters.SetShardId<TShard>(shardParameterOrdinal, shardTuple.Key);
                            tsks.Add(this.dtn[shardTuple.Key].Read._manager.ListAsync<TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), shardTuple.Value, cancellationToken));
                        }
                    }
                    await Task.WhenAll(tsks).ConfigureAwait(false);
                    foreach (var tsk in tsks)
                    {
                        var interim = tsk.Result;
                        if (!(interim is null))
                        {
                            result.AddRange(interim);
                        }
                    }
                }
                return result;
            }

            internal async Task RunAllAsync(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(sprocName))
                {
                    throw new ArgumentNullException(nameof(sprocName));
                }
                if (parameters == null)
                {
                    throw new ArgumentNullException(nameof(parameters));
                }

                cancellationToken.ThrowIfCancellationRequested();
                var shardParameterOrdinal = parameters.GetParameterOrdinal(shardParameterName);
                var tsks = new List<Task>();
                var cancelTokenSource = new CancellationTokenSource();
                using (var queryCancelationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelTokenSource.Token))
                {
                    if (shardParameterValues is null)
                    {
                        foreach (var shardId in dtn.Keys)
                        {
                            parameters.SetShardId<TShard>(shardParameterOrdinal, shardId);
                            tsks.Add(this.dtn[shardId].Write._manager.RunAsync(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken));
                        }
                    }
                    else
                    {
                        var shardParameters = ParseShardParameterValues(shardParameterValues);
                        foreach (var shardTuple in shardParameters)
                        {
                            parameters.SetShardId<TShard>(shardParameterOrdinal, shardTuple.Key);
                            tsks.Add(this.dtn[shardTuple.Key].Write._manager.RunAsync(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), shardTuple.Value, cancellationToken));
                        }
                    }
                    await Task.WhenAll(tsks).ConfigureAwait(false);
                }
            }

            internal async Task<IList<TModel>> WriteQueryAllAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
            {
                var result = new List<TModel>();
                if (string.IsNullOrEmpty(sprocName))
                {
                    throw new ArgumentNullException(nameof(sprocName));
                }
                if (parameters == null)
                {
                    throw new ArgumentNullException(nameof(parameters));
                }
                var shardParameterOrdinal = parameters.GetParameterOrdinal(shardParameterName);
                cancellationToken.ThrowIfCancellationRequested();

                var tsks = new List<Task<TModel>>();
                var cancelTokenSource = new CancellationTokenSource();
                using (var queryCancelationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelTokenSource.Token))
                {
                    if (shardParameterValues is null)
                    {
                        foreach (var shardId in dtn.Keys)
                        {
                            parameters.SetShardId<TShard>(shardParameterOrdinal, shardId);
                            tsks.Add(this.dtn[shardId].Write._manager.QueryAsync<TArg, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, resultHandler, false, dataObject, cancellationToken));
                        }
                    }
                    else
                    {
                        var shardParameters = ParseShardParameterValues(shardParameterValues);
                        foreach (var shardTuple in shardParameters)
                        {
                            parameters.SetShardId<TShard>(shardParameterOrdinal, shardTuple.Key);
                            tsks.Add(this.dtn[shardTuple.Key].Write._manager.QueryAsync<TArg, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), shardTuple.Value, resultHandler, false, dataObject, cancellationToken));
                        }
                    }
                    await Task.WhenAll(tsks).ConfigureAwait(false);
                    foreach (var tsk in tsks)
                    {
                        var tskResult = tsk.Result;
                        if (tskResult != null)
                        {
                            result.Add(tskResult);
                        }
                    }
                }
                return result;
            }

            #endregion

            public ShardSetReadAll ReadAll { get; }
            public ShardSetReadFirst ReadFirst { get; }
            public ShardSetWrite Write { get; }
        }
    }
}
