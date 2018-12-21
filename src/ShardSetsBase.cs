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
    /// The ShardSets collection contains any number of ShardSets.
    /// This class is used by provider specific implementations. It is unlikely that you would reference this in consumer code.
    /// Classes that inherit from this class manage sharded database connections.
    /// </summary>
    /// <typeparam name="TShard">The type of the ShardId.</typeparam>
    /// <typeparam name="TConfiguration">A provider-specific implementation of IShardSetConfigurationOptions.</typeparam>
    public abstract partial class ShardSetsBase<TShard, TConfiguration> : ICollection where TShard : IComparable where TConfiguration : class, IShardSetsConfigurationOptions<TShard>, new()
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
            if (configOptions?.Value?.ShardSetsConfigInternal is null)
            {
                logger.LogWarning("The ShardSets collection is missing required data connection information. Your application configuration may be missing a shard configuration section.");
            }
            this._dataProviderServices = dataProviderServices;
            this._globalConfiguration = globalConfiguration;
            var bdr = ImmutableDictionary.CreateBuilder<string, ShardSet>();
            if (!(configOptions?.Value?.ShardSetsConfigInternal is null))
            {
                foreach (var set in configOptions.Value.ShardSetsConfigInternal)
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

    }
}
