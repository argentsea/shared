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
    public abstract partial class ShardSetsBase<TConfiguration> where TConfiguration : class, IShardSetsConfigurationOptions, new()
    {
        /// <summary>
        /// This collection represents a complete shard set. Typically all databases within a shard set have nearly identical schemas.
        /// </summary>
        public class ShardSet : ICollection
        {
            private readonly object syncRoot = new object();
            private readonly ImmutableDictionary<short, ShardInstance<TConfiguration>> dtn;
            private readonly short _defaultShardId;
            public ShardSet(ShardSetsBase<TConfiguration> parent, IShardSetConnectionsConfiguration config)
            {
                var bdr = ImmutableDictionary.CreateBuilder<short, ShardInstance<TConfiguration>>();
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
                    bdr.Add(shd.ShardId, new ShardInstance<TConfiguration>(parent, shd.ShardId, shd));
                }
                this.dtn = bdr.ToImmutable();
                this.ReadAll = new ShardSetReadAll(this);
                this.ReadFirst = new ShardSetReadFirst(this);
                this.Write = new ShardSetWrite(this);
            }

            public string Key { get; set; }

            public ShardInstance<TConfiguration> this[short shardId]
            {
                get { return dtn[shardId]; }
            }
            #region ShardInstance ShardKey overloads
            public ShardInstance<TConfiguration> this[ShardKey<byte> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong> shardKey] => this[shardKey.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort> shardKey] => this[shardKey.ShardId];
            #endregion
            #region ShardInstance<TConfiguration> ShardChild overloads
            public ShardInstance<TConfiguration> this[ShardKey<byte, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<byte, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<char, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<char, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<DateTime, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTime, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<DateTimeOffset, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<decimal, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<decimal, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<double, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<double, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<float, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<float, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<Guid, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<Guid, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<int, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<int, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<long, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<long, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<sbyte, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<sbyte, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<short, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<short, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<string, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<string, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<TimeSpan, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<uint, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<uint, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<ulong, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ulong, ushort> shardChild] => this[shardChild.ShardId];

            public ShardInstance<TConfiguration> this[ShardKey<ushort, byte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, char> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, DateTime> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, DateTimeOffset> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, decimal> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, double> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, float> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, Guid> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, int> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, long> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, sbyte> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, short> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, string> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, TimeSpan> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, uint> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, ulong> shardChild] => this[shardChild.ShardId];
            public ShardInstance<TConfiguration> this[ShardKey<ushort, ushort> shardChild] => this[shardChild.ShardId];

            #endregion

            public int Count
            {
                get { return dtn.Count; }
            }
            public ShardInstance<TConfiguration> DefaultShard => dtn[_defaultShardId];

            public bool IsSynchronized => true;

            public object SyncRoot => syncRoot;

            public void CopyTo(Array array, int index)
                => this.dtn.Values.ToImmutableList().CopyTo((ShardInstance<TConfiguration>[])array, index);

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

            internal async Task<List<ShardKey<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string recordColumnName, string childColumnName, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TRecord : IComparable
                where TChild : IComparable
            {
                var result = new List<ShardKey<TRecord, TChild>>();
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
                                result.Add(new ShardKey<TRecord, TChild>(origin, shardIds[tsk], itm.Item1, itm.Item2));
                            }
                        }
                    }
                }
                return result;
            }

            internal async Task<List<ShardKey<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string shardColumnName, string recordColumnName, string childColumnName, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TRecord : IComparable
                where TChild : IComparable
            {
                var result = new List<ShardKey<TRecord, TChild>>();
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
                                result.Add(new ShardKey<TRecord, TChild>(origin, itm.Item1, itm.Item2, itm.Item3));
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
