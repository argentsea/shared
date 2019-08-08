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
    public abstract partial class ShardSetsBase<TConfiguration> : ICollection where TConfiguration : class, IShardSetsConfigurationOptions, new()
    {
        /// <summary>
        /// This collection represents a complete shard set. Typically all databases within a shard set have nearly identical schemas.
        /// </summary>
        public class ShardSet : ICollection
        {
            private readonly object syncRoot = new object();
            private readonly ImmutableDictionary<short, ShardInstance> dtn;
            private short _defaultShardId;
            public ShardSet(ShardSetsBase<TConfiguration> parent, IShardSetConnectionsConfiguration config)
            {
                var bdr = ImmutableDictionary.CreateBuilder<short, ShardInstance>();
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

            public ShardInstance this[short shardId]
            {
                get { return dtn[shardId]; }
            }
            #region ShardInstance ShardKey overloads
            public ShardInstance this[ShardKey<byte> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<char> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<DateTime> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<DateTimeOffset> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<decimal> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<double> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<float> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<Guid> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<int> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<long> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<sbyte> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<short> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<string> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<TimeSpan> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<uint> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<ulong> shardKey] => this[shardKey.ShardId];
            public ShardInstance this[ShardKey<ushort> shardKey] => this[shardKey.ShardId];
            #endregion
            #region ShardInstance ShardChild overloads
            public ShardInstance this[ShardChild<byte, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<byte, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<char, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<char, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<DateTime, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTime, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<DateTimeOffset, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<DateTimeOffset, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<decimal, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<decimal, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<double, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<double, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<float, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<float, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<Guid, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<Guid, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<int, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<int, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<long, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<long, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<sbyte, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<sbyte, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<short, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<short, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<string, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<string, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<TimeSpan, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<TimeSpan, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<uint, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<uint, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<ulong, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ulong, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance this[ShardChild<ushort, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance this[ShardChild<ushort, ushort> shardChild] => this[shardChild.ShardId];

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

            #region internal ShardSet query methods
            internal async Task<List<TModel>> ReadQueryAllAsync<TArg, TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, QueryResultModelHandler<TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
            {
                var result = new List<TModel>();
                if (query is null || string.IsNullOrEmpty(query.Sql) || string.IsNullOrEmpty(query.Name))
                {
                    throw new ArgumentNullException(nameof(query));
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
                            parameters.SetShardId(shardParameterOrdinal, shardId);
                            tsks.Add(this.dtn[shardId].Read._manager.QueryAsync<TArg, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, resultHandler, false, dataObject, cancellationToken));
                        }
                    }
                    else
                    {
                        if (shardParameterValues.Shards.Count == 0)
                        {
                            return result;
                        }
                        foreach (var shardTuple in shardParameterValues.Shards)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardTuple.Key);
                            tsks.Add(this.dtn[shardTuple.Key].Read._manager.QueryAsync<TArg, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), shardTuple.Value, resultHandler, false, dataObject, cancellationToken));
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

            internal async Task<TModel> ReadQueryFirstAsync<TArg, TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, QueryResultModelHandler<TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
            {
                var result = default(TModel);
                if (query is null || string.IsNullOrEmpty(query.Sql) || string.IsNullOrEmpty(query.Name))
                {
                    throw new ArgumentNullException(nameof(query));
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
                            parameters.SetShardId(shardParameterOrdinal, shardId);
                            tsks.Add(this.dtn[shardId].Read._manager.QueryAsync<TArg, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, resultHandler, false, dataObject, cancellationToken));
                        }
                    }
                    else
                    {
                        if (shardParameterValues.Shards.Count == 0)
                        {
                            return result;
                        }
                        foreach (var shardTuple in shardParameterValues.Shards)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardTuple.Key);
                            tsks.Add(this.dtn[shardTuple.Key].Read._manager.QueryAsync<TArg, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), shardTuple.Value, resultHandler, false, dataObject, cancellationToken));
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

            internal async Task<List<TModel>> MapListAsync<TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
            {
                var result = new List<TModel>();
                if (query is null || string.IsNullOrEmpty(query.Sql) || string.IsNullOrEmpty(query.Name))
                {
                    throw new ArgumentNullException(nameof(query));
                }
                if (parameters == null)
                {
                    throw new ArgumentNullException(nameof(parameters));
                }

                cancellationToken.ThrowIfCancellationRequested();
                var shardParameterOrdinal = parameters.GetParameterOrdinal(shardParameterName);
                var tsks = new List<Task<List<TModel>>>();
                var cancelTokenSource = new CancellationTokenSource();
                using (var queryCancelationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelTokenSource.Token))
                {
                    if (shardParameterValues is null)
                    {
                        foreach (var shardId in dtn.Keys)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardId);
                            tsks.Add(this.dtn[shardId].Read._manager.ListAsync<TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken));
                        }
                    }
                    else
                    {
                        if (shardParameterValues.Shards.Count == 0)
                        {
                            return result;
                        }
                        foreach (var shardTuple in shardParameterValues.Shards)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardTuple.Key);
                            tsks.Add(this.dtn[shardTuple.Key].Read._manager.ListAsync<TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), shardTuple.Value, cancellationToken));
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

            internal  async Task<List<TValue>> ListAsync<TValue>(Query query, DbParameterCollection parameters, string columnName, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            {
                var result = new List<TValue>();
                if (query is null || string.IsNullOrEmpty(query.Sql) || string.IsNullOrEmpty(query.Name))
                {
                    throw new ArgumentNullException(nameof(query));
                }
                if (parameters == null)
                {
                    throw new ArgumentNullException(nameof(parameters));
                }
                cancellationToken.ThrowIfCancellationRequested();
                var shardParameterOrdinal = parameters.GetParameterOrdinal(shardParameterName);
                var tsks = new List<Task<List<(TValue, object, object)>>>();
                var cancelTokenSource = new CancellationTokenSource();
                using (var queryCancelationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelTokenSource.Token))
                {
                    if (shardParameterValues is null)
                    {
                        foreach (var shardId in dtn.Keys)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardId);
                            tsks.Add(this.dtn[shardId].Read._manager.ListAsync<TValue, object, object>(query, columnName, null, null, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken));
                        }
                    }
                    else
                    {
                        if (shardParameterValues.Shards.Count == 0)
                        {
                            return result;
                        }
                        foreach (var shardTuple in shardParameterValues.Shards)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardTuple.Key);
                            tsks.Add(this.dtn[shardTuple.Key].Read._manager.ListAsync<TValue, object, object>(query, columnName, null, null, parameters, parameters.GetParameterOrdinal(shardParameterName), shardTuple.Value, cancellationToken));
                        }
                    }
                    await Task.WhenAll(tsks).ConfigureAwait(false);
                    foreach (var tsk in tsks)
                    {
                        var interim = tsk.Result;
                        if (!(interim is null))
                        {
                            foreach (var itm in interim)
                            {
                                result.Add(itm.Item1);
                            }
                        }
                    }
                }
                return result;
            }

            internal async Task<List<ShardKey<TRecord>>> ListAsync<TRecord>(Query query, char origin, string recordColumnName, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TRecord : IComparable
            {
                var result = new List<ShardKey<TRecord>>();
                if (query is null || string.IsNullOrEmpty(query.Sql) || string.IsNullOrEmpty(query.Name))
                {
                    throw new ArgumentNullException(nameof(query));
                }
                if (parameters == null)
                {
                    throw new ArgumentNullException(nameof(parameters));
                }
                cancellationToken.ThrowIfCancellationRequested();
                var shardParameterOrdinal = parameters.GetParameterOrdinal(shardParameterName);
                var tsks = new List<Task<List<(TRecord, object, object)>>>();
                var shardIds = new Dictionary<Task<List<(TRecord, object, object)>>, short>();
                var cancelTokenSource = new CancellationTokenSource();
                using (var queryCancelationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelTokenSource.Token))
                {
                    if (shardParameterValues is null)
                    {
                        foreach (var shardId in dtn.Keys)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardId);
                            var tsk = this.dtn[shardId].Read._manager.ListAsync<TRecord, object, object>(query, recordColumnName, null, null, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken);
                            tsks.Add(tsk);
                            shardIds.Add(tsk, shardId);
                        }
                    }
                    else
                    {
                        if (shardParameterValues.Shards.Count == 0)
                        {
                            return result;
                        }
                        foreach (var shardTuple in shardParameterValues.Shards)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardTuple.Key);
                            var tsk = this.dtn[shardTuple.Key].Read._manager.ListAsync<TRecord, object, object>(query, recordColumnName, null, null, parameters, parameters.GetParameterOrdinal(shardParameterName), shardTuple.Value, cancellationToken);
                            tsks.Add(tsk);
                            shardIds.Add(tsk, shardTuple.Key);
                        }
                    }
                    await Task.WhenAll(tsks).ConfigureAwait(false);
                    foreach (var tsk in tsks)
                    {
                        var interim = tsk.Result;
                        if (!(interim is null))
                        {
                            foreach (var itm in interim)
                            {
                                result.Add(new ShardKey<TRecord>(origin, shardIds[tsk], itm.Item1));
                            }
                        }
                    }
                }
                return result;
            }

            internal async Task<List<ShardKey<TRecord>>> ListAsync<TRecord>(Query query, char origin, string shardColumnName, string recordColumnName, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TRecord : IComparable
            {
                var result = new List<ShardKey<TRecord>>();
                if (query is null || string.IsNullOrEmpty(query.Sql) || string.IsNullOrEmpty(query.Name))
                {
                    throw new ArgumentNullException(nameof(query));
                }
                if (parameters == null)
                {
                    throw new ArgumentNullException(nameof(parameters));
                }
                cancellationToken.ThrowIfCancellationRequested();
                var shardParameterOrdinal = parameters.GetParameterOrdinal(shardParameterName);
                var tsks = new List<Task<List<(short, TRecord, object)>>>();
                var cancelTokenSource = new CancellationTokenSource();
                using (var queryCancelationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelTokenSource.Token))
                {
                    if (shardParameterValues is null)
                    {
                        foreach (var shardId in dtn.Keys)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardId);
                            var tsk = this.dtn[shardId].Read._manager.ListAsync<short, TRecord, object>(query, shardColumnName, recordColumnName, null, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken);
                            tsks.Add(tsk);
                        }
                    }
                    else
                    {
                        if (shardParameterValues.Shards.Count == 0)
                        {
                            return result;
                        }
                        foreach (var shardTuple in shardParameterValues.Shards)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardTuple.Key);
                            var tsk = this.dtn[shardTuple.Key].Read._manager.ListAsync<short, TRecord, object>(query, shardColumnName, recordColumnName, null, parameters, parameters.GetParameterOrdinal(shardParameterName), shardTuple.Value, cancellationToken);
                            tsks.Add(tsk);
                        }
                    }
                    await Task.WhenAll(tsks).ConfigureAwait(false);
                    foreach (var tsk in tsks)
                    {
                        var interim = tsk.Result;
                        if (!(interim is null))
                        {
                            foreach (var itm in interim)
                            {
                                result.Add(new ShardKey<TRecord>(origin, itm.Item1, itm.Item2));
                            }
                        }
                    }
                }
                return result;
            }

            internal async Task<List<ShardChild<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string recordColumnName, string childColumnName, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TRecord : IComparable
                where TChild : IComparable
            {
                var result = new List<ShardChild<TRecord, TChild>>();
                if (query is null || string.IsNullOrEmpty(query.Sql) || string.IsNullOrEmpty(query.Name))
                {
                    throw new ArgumentNullException(nameof(query));
                }
                if (parameters == null)
                {
                    throw new ArgumentNullException(nameof(parameters));
                }
                cancellationToken.ThrowIfCancellationRequested();
                var shardParameterOrdinal = parameters.GetParameterOrdinal(shardParameterName);
                var tsks = new List<Task<List<(TRecord, TChild, object)>>>();
                var shardIds = new Dictionary<Task<List<(TRecord, TChild, object)>>, short>();
                var cancelTokenSource = new CancellationTokenSource();
                using (var queryCancelationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelTokenSource.Token))
                {
                    if (shardParameterValues is null)
                    {
                        foreach (var shardId in dtn.Keys)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardId);
                            var tsk = this.dtn[shardId].Read._manager.ListAsync<TRecord, TChild, object>(query, recordColumnName, childColumnName, null, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken);
                            tsks.Add(tsk);
                            shardIds.Add(tsk, shardId);
                        }
                    }
                    else
                    {
                        if (shardParameterValues.Shards.Count == 0)
                        {
                            return result;
                        }
                        foreach (var shardTuple in shardParameterValues.Shards)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardTuple.Key);
                            var tsk = this.dtn[shardTuple.Key].Read._manager.ListAsync<TRecord, TChild, object>(query, recordColumnName, childColumnName, null, parameters, parameters.GetParameterOrdinal(shardParameterName), shardTuple.Value, cancellationToken);
                            tsks.Add(tsk);
                            shardIds.Add(tsk, shardTuple.Key);
                        }
                    }
                    await Task.WhenAll(tsks).ConfigureAwait(false);
                    foreach (var tsk in tsks)
                    {
                        var interim = tsk.Result;
                        if (!(interim is null))
                        {
                            foreach (var itm in interim)
                            {
                                result.Add(new ShardChild<TRecord, TChild>(origin, shardIds[tsk], itm.Item1, itm.Item2));
                            }
                        }
                    }
                }
                return result;
            }

            internal async Task<List<ShardChild<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string shardColumnName, string recordColumnName, string childColumnName, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TRecord : IComparable
                where TChild : IComparable
            {
                var result = new List<ShardChild<TRecord, TChild>>();
                if (query is null || string.IsNullOrEmpty(query.Sql) || string.IsNullOrEmpty(query.Name))
                {
                    throw new ArgumentNullException(nameof(query));
                }
                if (parameters == null)
                {
                    throw new ArgumentNullException(nameof(parameters));
                }
                cancellationToken.ThrowIfCancellationRequested();
                var shardParameterOrdinal = parameters.GetParameterOrdinal(shardParameterName);
                var tsks = new List<Task<List<(short, TRecord, TChild)>>>();
                var cancelTokenSource = new CancellationTokenSource();
                using (var queryCancelationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelTokenSource.Token))
                {
                    if (shardParameterValues is null)
                    {
                        foreach (var shardId in dtn.Keys)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardId);
                            var tsk = this.dtn[shardId].Read._manager.ListAsync<short, TRecord, TChild>(query, shardColumnName, recordColumnName, childColumnName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken);
                            tsks.Add(tsk);
                        }
                    }
                    else
                    {
                        if (shardParameterValues.Shards.Count == 0)
                        {
                            return result;
                        }
                        foreach (var shardTuple in shardParameterValues.Shards)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardTuple.Key);
                            var tsk = this.dtn[shardTuple.Key].Read._manager.ListAsync<short, TRecord, TChild>(query, shardColumnName, recordColumnName, childColumnName, parameters, parameters.GetParameterOrdinal(shardParameterName), shardTuple.Value, cancellationToken);
                            tsks.Add(tsk);
                        }
                    }
                    await Task.WhenAll(tsks).ConfigureAwait(false);
                    foreach (var tsk in tsks)
                    {
                        var interim = tsk.Result;
                        if (!(interim is null))
                        {
                            foreach (var itm in interim)
                            {
                                result.Add(new ShardChild<TRecord, TChild>(origin, itm.Item1, itm.Item2, itm.Item3));
                            }
                        }
                    }
                }
                return result;
            }

            internal async Task RunAllAsync(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            {
                if (query is null || string.IsNullOrEmpty(query.Sql) || string.IsNullOrEmpty(query.Name))
                {
                    throw new ArgumentNullException(nameof(query));
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
                            parameters.SetShardId(shardParameterOrdinal, shardId);
                            tsks.Add(this.dtn[shardId].Write._manager.RunAsync(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken));
                        }
                    }
                    else
                    {
                        if (shardParameterValues.Shards.Count == 0)
                        {
                            return;
                        }
                        foreach (var shardTuple in shardParameterValues.Shards)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardTuple.Key);
                            tsks.Add(this.dtn[shardTuple.Key].Write._manager.RunAsync(query, parameters, parameters.GetParameterOrdinal(shardParameterName), shardTuple.Value, cancellationToken));
                        }
                    }
                    await Task.WhenAll(tsks).ConfigureAwait(false);
                }
            }

            internal async Task BatchAllAsync(ShardSetBatch batch, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            {
                if (batch is null)
                {
                    throw new ArgumentNullException(nameof(batch));
                }

                cancellationToken.ThrowIfCancellationRequested();
                var tsks = new List<Task>();
                var cancelTokenSource = new CancellationTokenSource();
                using (var queryCancelationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelTokenSource.Token))
                {
                    if (shardParameterValues is null)
                    {
                        foreach (var shardId in dtn.Keys)
                        {
                            tsks.Add(this.dtn[shardId].Write._manager.RunBatchAsync<object>(batch, cancellationToken));
                        }
                    }
                    else
                    {
                        if (shardParameterValues.Shards.Count == 0)
                        {
                            return;
                        }
                        foreach (var shardTuple in shardParameterValues.Shards)
                        {
                            tsks.Add(this.dtn[shardTuple.Key].Write._manager.RunBatchAsync<object>(batch, cancellationToken));
                        }
                    }
                    await Task.WhenAll(tsks).ConfigureAwait(false);
                }
            }

            internal async Task<List<TModel>> WriteQueryAllAsync<TArg, TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, QueryResultModelHandler<TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
            {
                var result = new List<TModel>();
                if (query is null || string.IsNullOrEmpty(query.Sql) || string.IsNullOrEmpty(query.Name))
                {
                    throw new ArgumentNullException(nameof(query));
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
                            parameters.SetShardId(shardParameterOrdinal, shardId);
                            tsks.Add(this.dtn[shardId].Write._manager.QueryAsync<TArg, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, resultHandler, false, dataObject, cancellationToken));
                        }
                    }
                    else
                    {
                        if (shardParameterValues.Shards.Count == 0)
                        {
                            return result;
                        }
                        foreach (var shardTuple in shardParameterValues.Shards)
                        {
                            parameters.SetShardId(shardParameterOrdinal, shardTuple.Key);
                            tsks.Add(this.dtn[shardTuple.Key].Write._manager.QueryAsync<TArg, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), shardTuple.Value, resultHandler, false, dataObject, cancellationToken));
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
