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
    /// <summary>
    /// This collection represents a complete shard set. Typically all databases within a shard set have nearly identical schemas.
    /// </summary>
    public class ShardSet<TConfiguration> : ICollection where TConfiguration : class, IShardSetsConfigurationOptions, new()
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
            this.ReadAll = new ShardSetReadAll<TConfiguration>(this);
            this.ReadFirst = new ShardSetReadFirst<TConfiguration>(this);
            this.Write = new ShardSetWrite<TConfiguration>(this);
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
        public ShardInstance<TConfiguration> this[ShardChild<byte, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<byte, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<char, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<char, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<DateTime, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTime, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<DateTimeOffset, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<decimal, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<decimal, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<double, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<double, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<float, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<float, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<Guid, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<Guid, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<int, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<int, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<long, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<long, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<sbyte, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<sbyte, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<short, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<short, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<string, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<string, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<TimeSpan, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<uint, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<uint, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<ulong, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ulong, ushort> shardChild] => this[shardChild.ShardId];

        public ShardInstance<TConfiguration> this[ShardChild<ushort, byte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, char> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, DateTime> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, DateTimeOffset> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, decimal> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, double> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, float> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, Guid> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, int> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, long> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, sbyte> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, short> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, string> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, TimeSpan> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, uint> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, ulong> shardChild] => this[shardChild.ShardId];
        public ShardInstance<TConfiguration> this[ShardChild<ushort, ushort> shardChild] => this[shardChild.ShardId];

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

        public ShardSetReadAll<TConfiguration> ReadAll { get; }
        public ShardSetReadFirst<TConfiguration> ReadFirst { get; }
        public ShardSetWrite<TConfiguration> Write { get; }
    }
}
