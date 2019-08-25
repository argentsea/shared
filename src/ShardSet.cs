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
            public ShardInstance<TConfiguration> this[IShardKey shardKey] => this[shardKey.ShardId];

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
