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
    /// This class is used by provider specific implementations. It is unlikely that you would reference this in consumer code.
    /// Classes that inherit from this class manage sharded database connections.
    /// </summary>
    /// <typeparam name="TShard">The type of the ShardId.</typeparam>
    /// <typeparam name="TConfiguration">A provider-specific implementation of IShardSetConfigurationOptions.</typeparam>
    public abstract class ShardSetsBase<TShard, TConfiguration> : ICollection where TShard : IComparable where TConfiguration : class, IShardSetsConfigurationOptions<TShard>, new()
    {
        private readonly object syncRoot = new Lazy<object>();
        private readonly ImmutableDictionary<string, ShardSet> dtn;
        private readonly IDataProviderServiceFactory _dataProviderServices;
        private readonly DataConnectionConfigurationBase _globalConfiguration;
        private readonly ILogger _logger;

        public  ShardSetsBase(
                IOptions<TConfiguration> configOptions,
                IDataProviderServiceFactory dataProviderServices, 
                DataConnectionConfigurationBase globalConfiguration,
                ILogger<ShardSetsBase<TShard, TConfiguration>> logger)
        {
            this._logger = logger;
            if (configOptions?.Value?.ShardSetsInternal is null)
            {
                logger.LogWarning("The ShardSets collection is missing required data connection information. Your application configuration may be missing a shard configuration section.");
            }
            this._dataProviderServices = dataProviderServices;
            this._globalConfiguration = globalConfiguration;
            var bdr = ImmutableDictionary.CreateBuilder<string, ShardSet>();
            if (!(configOptions?.Value?.ShardSetsInternal is null))
            {
                foreach (var set in configOptions.Value.ShardSetsInternal)
                {
                    if (set is null)
                    {
                        throw new Exception($"A shard set configuration is not valid; the configuration provider returned null.");
                    }
                    bdr.Add(set.ShardSetName, new ShardSet(this, set));
                }
                this.dtn = bdr.ToImmutable();
            }
            else
            {
                this.dtn = ImmutableDictionary<string, ShardSet>.Empty;
            }
        }
        public ShardSet this[string key]
        {
            get => dtn[key];
        }

        public int Count
        {
            get => dtn.Count;
        }

        public bool IsSynchronized => true;

        public object SyncRoot => syncRoot;

        public void CopyTo(Array array, int index)
            => this.dtn.Values.ToImmutableList().CopyTo((ShardSet[])array, index);

        public IEnumerator GetEnumerator() => this.dtn.GetEnumerator();

        #region Nested classes

        public class ShardSet : ICollection
        {
            private readonly object syncRoot = new Lazy<object>();
            private readonly ImmutableDictionary<TShard, ShardInstance> dtn;
            private TShard _defaultShardId;
            public ShardSet(ShardSetsBase<TShard, TConfiguration> parent, IShardSetConnectionsConfiguration<TShard> config)
            {
                var bdr = ImmutableDictionary.CreateBuilder<TShard, ShardInstance>();
                _defaultShardId = config.DefaultShardId;
                foreach (var shd in config.ShardsInternal)
                {
                    if (shd is null)
                    {
                        throw new Exception($"A shard set’s connection configuration was not valid; the configuration provider returned null.");
                    }
                    var shardSetsConfig = config as DataConnectionConfigurationBase;
                    var shardConfig = shd as DataConnectionConfigurationBase;
                    shd.ReadConnectionInternal.SetAmbientConfiguration(parent._globalConfiguration, shardSetsConfig, shardConfig);
                    shd.WriteConnectionInternal.SetAmbientConfiguration(parent._globalConfiguration, shardSetsConfig, shardConfig);
                    bdr.Add(shd.ShardId, new ShardInstance(parent, shd.ShardId, shd));
                }
                this.dtn = bdr.ToImmutable();
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
            #region Query All Shards

            /// <summary>
            /// Query across all shards in the shard set using a handler delegate.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> QueryAllAsync<TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<TShard, object, TModel> resultHandler, CancellationToken cancellationToken)
                => QueryAllAsync<object, TModel>(sprocName, parameters, null, shardParameterName, resultHandler, null, cancellationToken);

            /// <summary>
            /// Query across the specified shards, generating results using a handler delegate.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> QueryAllAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, QueryResultModelHandler<TShard, object, TModel> resultHandler, CancellationToken cancellationToken)
                => QueryAllAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, resultHandler, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set using a handler delegate.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> QueryAllAsync<TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<TShard, object, TModel> resultHandler, object dataObject, CancellationToken cancellationToken)
                => QueryAllAsync<object, TModel>(sprocName, parameters, null, null, resultHandler, null, cancellationToken);

            /// <summary>
            /// Query across the specified shards, generating results using a handler delegate.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> QueryAllAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, QueryResultModelHandler<TShard, object, TModel> resultHandler, CancellationToken cancellationToken)
                => QueryAllAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, resultHandler, null, cancellationToken);


            /// <summary>
            /// Query across all shards in the shard set using a handler delegate.
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> QueryAllAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
                => QueryAllAsync<TArg, TModel>(sprocName, parameters, null, shardParameterName, resultHandler, dataObject, cancellationToken);

            /// <summary>
            /// Query across the specified shards, generating results using a handler delegate.
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> QueryAllAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
                => QueryAllAsync<TArg, TModel>(sprocName, parameters, shardParameterValues, null, resultHandler, dataObject, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set using a handler delegate.
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> QueryAllAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
                => QueryAllAsync<TArg, TModel>(sprocName, parameters, null, null, resultHandler, dataObject, cancellationToken);


            /// <summary>
            /// Query across the specified shards, generating results using a handler delegate.
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public async Task<IList<TModel>> QueryAllAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
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
            #endregion
            #region Query First

            /// <summary>
            /// Query across all shards in the shard set, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public Task<TModel> QueryFirstAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
                => QueryFirstAsync<TArg, TModel>(sprocName, parameters, null, null, resultHandler, dataObject, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public Task<TModel> QueryFirstAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
                => QueryFirstAsync<TArg, TModel>(sprocName, parameters, null, shardParameterName, resultHandler, dataObject, cancellationToken);

            /// <summary>
            /// Query across the specified shards, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public Task<TModel> QueryFirstAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
                => QueryFirstAsync<TArg, TModel>(sprocName, parameters, shardParameterValues, null, resultHandler, dataObject, cancellationToken);

            /// <summary>
            /// Query across the specified shards, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public Task<TModel> QueryFirstAsync<TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<TShard, object, TModel> resultHandler, CancellationToken cancellationToken)
                => QueryFirstAsync<object, TModel>(sprocName, parameters, null, null, resultHandler, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public Task<TModel> QueryFirstAsync<TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<TShard, object, TModel> resultHandler, CancellationToken cancellationToken)
                => QueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, resultHandler, null, cancellationToken);

            /// <summary>
            /// Query across the specified shards, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public Task<TModel> QueryFirstAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, QueryResultModelHandler<TShard, object, TModel> resultHandler, CancellationToken cancellationToken)
                => QueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, resultHandler, null, cancellationToken);


            /// <summary>
            /// Query across the specified shards, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public Task<TModel> QueryFirstAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, QueryResultModelHandler<TShard, object, TModel> resultHandler, CancellationToken cancellationToken)
                => QueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, resultHandler, null, cancellationToken);

            /// <summary>
            /// Query across the specified shards, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public async Task<TModel> QueryFirstAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
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
            #endregion
            #region List

            /// <summary>
            /// Returns a list of objects created by the specified delegate.
            /// </summary>
            /// <typeparam name="TModel">The data type of the objects in the list result.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of TModel objects, built by the delegate from the data results.</returns>
            public Task<IList<TModel>> MapListAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : class, new()
                => MapListAsync<TModel>(sprocName, parameters, null, null, cancellationToken);

            /// <summary>
            /// Returns a list of objects created by the specified delegate.
            /// </summary>
            /// <typeparam name="TModel">The data type of the objects in the list result.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of TModel objects, built by the delegate from the data results.</returns>
            public Task<IList<TModel>> MapListAsync<TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
                => MapListAsync<TModel>(sprocName, parameters, null, shardParameterName, cancellationToken);

            /// <summary>
            /// Returns a list of objects created by the specified delegate.
            /// </summary>
            /// <typeparam name="TModel">The data type of the objects in the list result.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of TModel objects, built by the delegate from the data results.</returns>
            public Task<IList<TModel>> MapListAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken) where TModel : class, new()
                => MapListAsync<TModel>(sprocName, parameters, shardParameterValues, null, cancellationToken);

            /// <summary>
            /// Returns a list of objects created by the specified delegate.
            /// </summary>
            /// <typeparam name="TModel">The data type of the objects in the list result.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of TModel objects, built by the delegate from the data results.</returns>
            public async Task<IList<TModel>> MapListAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
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
            #endregion
            #region Run
            /// <summary>
            /// Runs the specified stored procedure on the Write connecton on all shards.
            /// </summary>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns></returns>
            public Task RunAllAsync(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                => RunAllAsync(sprocName, parameters, null, null, cancellationToken);

            /// <summary>
            /// Runs the specified stored procedure on the Write connecton on all shards.
            /// </summary>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns></returns>
            public Task RunAllAsync(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                => RunAllAsync(sprocName, parameters, null, shardParameterName, cancellationToken);

            /// <summary>
            /// Runs the specified stored procedure on the Write connecton on all shards.
            /// </summary>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns></returns>
            public Task RunAllAsync(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                => RunAllAsync(sprocName, parameters, shardParameterValues, null, cancellationToken);

            /// <summary>
            /// Runs the specified stored procedure on the Write connecton on all shards.
            /// </summary>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            public async Task RunAllAsync(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
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

            #endregion
            #region MapReaderAllAsync
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken) where TModel : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);

            #endregion
            #region MapReaderFirst
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken) where TModel : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);

            #endregion
            #region MapOutputAllAsync
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken) where TModel : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel>, cancellationToken);




            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);


            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAllAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryAllAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);

            #endregion
            #region MapOutputFirstAsync
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputFirstAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => this.QueryFirstAsync<TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, cancellationToken);

            #endregion
        }

        public class ShardInstance
		{
            public ShardInstance(ShardSetsBase<TShard, TConfiguration> parent, TShard shardId, IShardConnectionConfiguration<TShard> shardConnection)
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
                this.Read = new DataConnection(parent, shardId, readConnection);
				this.Write = new DataConnection(parent, shardId, writeConnection);
			}
			public TShard ShardId { get; }
			public DataConnection Read { get; }
			public DataConnection Write { get; }

		}

        public class DataConnection
        {
            internal readonly DataConnectionManager<TShard> _manager;

            internal DataConnection(ShardSetsBase<TShard, TConfiguration> parent, TShard shardId, IDataConnection config)
            {
                _manager = new DataConnectionManager<TShard>(shardId,
                    parent._dataProviderServices, config,
                    $"shard number { shardId.ToString() } on connection { config.ConnectionDescription }",
                    parent._logger);
            }

            public string ConnectionString { get => _manager.ConnectionString; }

            #region Public data fetch methods
            /// <summary>
            /// Connect to the database and return a single value.
            /// </summary>
            /// <typeparam name="TValue">The expected type of the return value.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the value.</param>
            /// <param name="parameters">A parameters collction. Input parameters may be used to find the parameter; will return the value of the first output (or input/output) parameter. If TValue is an int, will also return the sproc return value.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>The retrieved value.</returns>
            public Task<TValue> LookupAsync<TValue>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                => _manager.LookupAsync<TValue>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), cancellationToken);

            /// <summary>
            /// Connect to the database and return a single value.
            /// </summary>
            /// <typeparam name="TValue">The expected type of the return value.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the value.</param>
            /// <param name="parameters">A parameters collction. Input parameters may be used to find the parameter; will return the value of the first output (or input/output) parameter. If TValue is an int, will also return the sproc return value.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>The retrieved value.</returns>
            public Task<TValue> LookupAsync<TValue>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                => _manager.LookupAsync<TValue>(sprocName, parameters, -1, cancellationToken);

            /// <summary>
            /// Connect to the database and return the values as a list of objects.
            /// </summary>
            /// <typeparam name="TModel">The type of object to be listed.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>A list containing an object for each data row.</returns>
            public Task<IList<TModel>> MapListAsync<TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
                => _manager.ListAsync<TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken);

            /// <summary>
            /// Connect to the database and return the values as a list of objects.
            /// </summary>
            /// <typeparam name="TModel">The type of object to be listed.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>A list containing an object for each data row.</returns>
            public Task<IList<TModel>> MapListAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : class, new()
                => _manager.ListAsync<TModel>(sprocName, parameters, -1, null, cancellationToken);


            /// <summary>
            /// Connect to the database and return the TModel object returned by the delegate.
            /// </summary>
            /// <typeparam name="TModel">The type of the object to be returned.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="resultHandler">A method with a signature that corresponds to the QueryResultModelHandler delegate, which converts the provided DataReader and output parameters and returns an object of type TModel.</param>
            /// <param name="isTopOne">If the procedure or function is expected to return only one record, setting this to True provides a minor optimization.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>An object of type TModel, as created and populated by the provided delegate.</returns>
            public Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<TShard, object, TModel> resultHandler, bool isTopOne, CancellationToken cancellationToken) where TModel : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, resultHandler, isTopOne, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return the TModel object returned by the delegate.
            /// </summary>
            /// <typeparam name="TModel">The type of the object to be returned.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="resultHandler">A method with a signature that corresponds to the QueryResultModelHandler delegate, which converts the provided DataReader and output parameters and returns an object of type TModel.</param>
            /// <param name="isTopOne">If the procedure or function is expected to return only one record, setting this to True provides a minor optimization.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>An object of type TModel, as created and populated by the provided delegate.</returns>
			public Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<TShard, object, TModel> resultHandler, bool isTopOne, CancellationToken cancellationToken) where TModel : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, resultHandler, isTopOne, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return the TModel object returned by the delegate.
            /// </summary>
            /// <typeparam name="TArg"></typeparam>
            /// <typeparam name="TModel">The type of the object to be returned.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="resultHandler">A method with a signature that corresponds to the QueryResultModelHandler delegate, which converts the provided DataReader and output parameters and returns an object of type TModel.</param>
            /// <param name="isTopOne">If the procedure or function is expected to return only one record, setting this to True provides a minor optimization.</param>
            /// <param name="optionalArgument">An object of type TArg which can be used to pass non-datatabase data to the result-generating delegate.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>An object of type TModel, as created and populated by the provided delegate.</returns>
			public Task<TModel> QueryAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) where TModel : class, new()
                => _manager.QueryAsync<TArg, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, resultHandler, isTopOne, optionalArgument, cancellationToken);

            /// <summary>
            /// Connect to the database and return the TModel object returned by the delegate.
            /// </summary>
            /// <typeparam name="TArg"></typeparam>
            /// <typeparam name="TModel">The type of the object to be returned.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="resultHandler">A method with a signature that corresponds to the QueryResultModelHandler delegate, which converts the provided DataReader and output parameters and returns an object of type TModel.</param>
            /// <param name="isTopOne">If the procedure or function is expected to return only one record, setting this to True provides a minor optimization.</param>
            /// <param name="optionalArgument">An object of type TArg which can be used to pass non-datatabase data to the result-generating delegate.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>An object of type TModel, as created and populated by the provided delegate.</returns>
			public Task<TModel> QueryAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) where TModel : class, new()
                => _manager.QueryAsync<TArg, TModel>(sprocName, parameters, -1, null, resultHandler, isTopOne, optionalArgument, cancellationToken);


            /// <summary>
            /// Executes a database procedure or function that does not return a data result.
            /// </summary>
            /// <param name="sprocName">The stored procedure or function to call.</param>
            /// <param name="parameters">The query parameters with values set.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>Throws an error if not successful.</returns>
            public Task RunAsync(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                => _manager.RunAsync(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken);

            /// <summary>
            /// Executes a database procedure or function that does not return a data result.
            /// </summary>
            /// <param name="sprocName">The stored procedure or function to call.</param>
            /// <param name="parameters">The query parameters with values set.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>Throws an error if not successful.</returns>
			public Task RunAsync(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                => _manager.RunAsync(sprocName, parameters, -1, null, cancellationToken);
            #endregion
            #region GetOut overloads
            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, Mapper.DummyType, TModel>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult>
                (string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>
                (string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
                (string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
                (string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
                (string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
                (string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
                (string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
                (string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, Mapper.DummyType, TModel>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult>
                (string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>
                (string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
                (string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
                (string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
                (string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
                (string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
                (string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
                (string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, false, null, cancellationToken);

            #endregion
            #region Read overloads
            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>
                (string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
                (string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
                (string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
                (string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
                (string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>
                (string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
                (string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
                (string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
                (string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
                (string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
                (string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
                (string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _manager.QueryAsync<object, TModel>(sprocName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, false, null, cancellationToken);
            #endregion
        }

        #endregion
    }
}
