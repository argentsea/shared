﻿using System;
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
    public abstract class ShardSetsBase<TShard, TConfiguration> : ICollection where TShard : IComparable where TConfiguration : class, IShardSetConfigurationOptions<TShard>, new()
    {
        private readonly object syncRoot = new Lazy<object>();
        private readonly ImmutableDictionary<string, ShardSet> dtn;

        private readonly DataSecurityOptions _securityOptions;
        private readonly DataResilienceOptions _resilienceStrategiesOptions;
        private readonly IDataProviderServiceFactory _dataProviderServices;
        private readonly ILogger _logger;

        public ShardSetsBase(
                IOptions<TConfiguration> configOptions,
                IOptions<DataSecurityOptions> securityOptions,
                IOptions<DataResilienceOptions> resilienceStrategiesOptions,
                IDataProviderServiceFactory dataProviderServices, 
                ILogger<ShardSetsBase<TShard, TConfiguration>> logger)
        {
            this._logger = logger;
            if (configOptions?.Value?.ShardSetsInternal is null)
            {
                logger.LogWarning("The ShardSets collection is missing required data connection information. Your application configuration may be missing a shard configuration section.");
            }
            this._securityOptions = securityOptions?.Value;
            this._resilienceStrategiesOptions = resilienceStrategiesOptions?.Value;
            this._dataProviderServices = dataProviderServices;
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

			public ShardSet(ShardSetsBase<TShard, TConfiguration> parent, IShardConnectionsConfiguration<TShard> config)
            {
                var bdr = ImmutableDictionary.CreateBuilder<TShard, ShardInstance>();
                foreach (var shd in config.ShardsInternal)
                {
                    if (shd is null)
                    {
                        throw new Exception($"A shard set’s connection configuration was not valid; the configuration provider returned null.");
                    }
                    shd.ReadConnectionInternal.SetConfigurationOptions(parent._securityOptions, parent._resilienceStrategiesOptions);
                    shd.WriteConnectionInternal.SetConfigurationOptions(parent._securityOptions, parent._resilienceStrategiesOptions);
                    bdr.Add(shd.ShardId, new ShardInstance(parent, shd.ShardId, shd.ReadConnectionInternal, shd.WriteConnectionInternal));
                }
                this.dtn = bdr.ToImmutable();
            }

            public string Key { get; set; }

            public ShardInstance this[TShard shardId]
            {
                get { return dtn[shardId]; }
            }

            public int Count
            {
                get { return dtn.Count; }
            }

            public bool IsSynchronized => true;

            public object SyncRoot => syncRoot;

            public void CopyTo(Array array, int index)
                => this.dtn.Values.ToImmutableList().CopyTo((ShardInstance[])array, index);

            public IEnumerator GetEnumerator() => this.dtn.GetEnumerator();

            #region Query All Shards


            /// <summary>
            /// Query across all shards in the shard set, except for those exlicitly excluded, using the Mapper.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="exclude">A list of shards not to be called.</param>
            /// <param name="shardParameterOrdinal">The index of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public async Task<IList<TModel>> QueryAllAsync<TModel>(string sprocName, DbParameterCollection parameters, TShard[] exclude, int shardParameterOrdinal, CancellationToken cancellationToken) where TModel : class, new()
                => await QueryAllAsync<object, TModel>(sprocName, parameters, exclude, shardParameterOrdinal, Mapper.QueryResultsHandler<TShard, TModel>, null, cancellationToken).ConfigureAwait(false);

            /// <summary>
            /// Query across all shards in the shard set using a handler delegate.
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterOrdinal">The index of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public async Task<IList<TModel>> QueryAllAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken) where TModel : class, new()
                => await QueryAllAsync<TArg, TModel>(sprocName, parameters, null, shardParameterOrdinal, resultHandler, dataObject, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set using the Mapper.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterOrdinal">The index of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public async Task<IList<TModel>> QueryAllAsync<TModel>(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken) where TModel : class, new()
                => await QueryAllAsync<object, TModel>(sprocName, parameters, shardParameterOrdinal, Mapper.QueryResultsHandler<TShard, TModel>, null, cancellationToken).ConfigureAwait(false);

            /// <summary>
            /// Query across all shards in the shard set using a handler delegate.
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="exclude">A list of shards not to be called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public async Task<IList<TModel>> QueryAllAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, TShard[] exclude, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken) where TModel : class, new()
                => await QueryAllAsync<TArg, TModel>(sprocName, parameters, exclude, -1, resultHandler, dataObject, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set using the Mapper.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="exclude">A list of shards not to be called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public async Task<IList<TModel>> QueryAllAsync<TModel>(string sprocName, DbParameterCollection parameters, TShard[] exclude, CancellationToken cancellationToken) where TModel : class, new()
                => await QueryAllAsync<object, TModel>(sprocName, parameters, exclude, -1, Mapper.QueryResultsHandler<TShard, TModel>, null, cancellationToken).ConfigureAwait(false);

            /// <summary>
            /// Query across all shards in the shard set, except for those exlicitly excluded, using a handler delegate.
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public async Task<IList<TModel>> QueryAllAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken) where TModel : class, new()
                => await QueryAllAsync<TArg, TModel>(sprocName, parameters, null, -1, resultHandler, dataObject, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set using the Mapper.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public async Task<IList<TModel>> QueryAllAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : class, new()
                => await QueryAllAsync<object, TModel>(sprocName, parameters, null, -1, Mapper.QueryResultsHandler<TShard, TModel>, null, cancellationToken).ConfigureAwait(false);


            /// <summary>
            /// Query across all shards in the shard set, except for those exlicitly excluded, using a handler delegate.
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="exclude">A list of shards not to be called.</param>
            /// <param name="shardParameterOrdinal">The index of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>

            public async Task<IList<TModel>> QueryAllAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, TShard[] exclude, int shardParameterOrdinal, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken) where TModel: class, new()

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

                var tsks = new List<Task<TModel>>();
                foreach (var shardId in dtn.Keys)
                {
                    if (shardParameterOrdinal >= 0 && shardParameterOrdinal < parameters.Count) // && cmd.Parameters[shardParameterOrdinal].DbType == System.Data.DbType.Byte)
                    {
                        parameters[shardParameterOrdinal].Value = shardId;
                    }
                    tsks.Add(this.dtn[shardId].Read.QueryAsync<TArg, TModel>(sprocName, parameters, shardParameterOrdinal, resultHandler, false, dataObject, cancellationToken));
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
                return result;
            }
            #endregion

            #region Query First
            public async Task<TResult> QueryFirstAsync<TArg, TResult>(string sprocName, DbParameterCollection parameters, TShard[] exclude, int shardParameterOrdinal, QueryResultModelHandler<TShard, TArg, TResult> resultHandler, TArg dataObject, CancellationToken cancellationToken) where TResult : class, new()
            {
                var result = default(TResult);
                if (string.IsNullOrEmpty(sprocName))
                {
                    throw new ArgumentNullException(nameof(sprocName));
                }
                if (parameters == null)
                {
                    throw new ArgumentNullException(nameof(parameters));
                }

                cancellationToken.ThrowIfCancellationRequested();

                var tsks = new List<Task<TResult>>();
                var cancelTokenSource = new CancellationTokenSource();
                using (var queryCancelationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancelTokenSource.Token))
                {
                    foreach (var shardId in dtn.Keys)
                    {
                        if (shardParameterOrdinal >= 0 && shardParameterOrdinal < parameters.Count) // && cmd.Parameters[shardParameterOrdinal].DbType == System.Data.DbType.Byte)
                        {
                            parameters[shardParameterOrdinal].Value = shardId;
                        }
                        tsks.Add(this.dtn[shardId].Read.QueryAsync<TArg, TResult>(sprocName, parameters, shardParameterOrdinal, resultHandler, true, dataObject, queryCancelationToken.Token));
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
        }

        public class ShardInstance
		{
            //public ShardInstance(ShardDataStores<TShard, TConfiguration> parent, TShard shardId, string resilienceStrategyKey, IConnectionConfiguration readConnection, IConnectionConfiguration writeConnection)
            public ShardInstance(ShardSetsBase<TShard, TConfiguration> parent, TShard shardId, IConnectionConfiguration readConnection, IConnectionConfiguration writeConnection)
            {
				this.ShardId = shardId;
                if (readConnection is null && !(writeConnection is null))
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
			private readonly DataConnectionManager<TShard> _manager;

            internal DataConnection(ShardSetsBase<TShard, TConfiguration> parent, TShard shardId, IConnectionConfiguration config)
            {
                var resilienceStrategies = parent?._resilienceStrategiesOptions?.DataResilienceStrategies;
                DataResilienceConfiguration drc = null;
                if (!(resilienceStrategies is null))
                {
                    foreach (var rs in resilienceStrategies)
                    {
                        if (rs.ResilienceKey == config.ResilienceKey)
                        {
                            drc = rs;
                            break;
                        }
                    }
                }
                if (drc is null)
                {
                    drc = new DataResilienceConfiguration();
                }

                _manager = new DataConnectionManager<TShard>(shardId, 
					parent._dataProviderServices, 
                    drc,
					config.GetConnectionString(), 
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
			/// <param name="shardParameterOrdinal">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns>The retrieved value.</returns>
			public Task<TValue> LookupAsync<TValue>(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken)
				=> _manager.LookupAsync<TValue>(sprocName, parameters, shardParameterOrdinal, cancellationToken);

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
			/// <typeparam name="TResult">The type of object to be listed.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="shardParameterOrdinal">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns>A list containing an object for each data row.</returns>
			public Task<IList<TResult>> ListAsync<TResult>(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken) where TResult : class, new()
				=> _manager.ListAsync<TResult>(sprocName, parameters, shardParameterOrdinal, cancellationToken);

			/// <summary>
			/// Connect to the database and return the values as a list of objects.
			/// </summary>
			/// <typeparam name="TResult">The type of object to be listed.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns>A list containing an object for each data row.</returns>
			public Task<IList<TResult>> ListAsync<TResult>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken) where TResult : class, new()
				=> _manager.ListAsync<TResult>(sprocName, parameters, -1, cancellationToken);


			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding output parameters.
			/// </summary>
			/// <typeparam name="TModel">The type of the object to be returned.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="shardParameterOrdinal">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken)
				where TModel : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, shardParameterOrdinal, Mapper.QueryResultsHandler<TShard, TModel, Mapper.DummyType, TModel>, false, null, cancellationToken);

			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding output parameters.
			/// </summary>
			/// <typeparam name="TModel">The type of the object to be returned.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="shardParameterOrdinal">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
				where TModel : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<TShard, TModel, Mapper.DummyType, TModel>, false, null, cancellationToken);


			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <typeparam name="TReaderResult">The data reader result set will be mapped an object or property of this type. If TOutParmaters is set to Mapper.DummyType then this must be a single row result of type TModel.</typeparam>
			/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="shardParameterOrdinal">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel, TReaderResult, TOutParameters>(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken)
				where TModel : class, new()
				where TReaderResult : class, new()
				where TOutParameters : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, shardParameterOrdinal, Mapper.QueryResultsHandler<TShard, TModel, TReaderResult, TOutParameters>, false, null, cancellationToken);

			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <typeparam name="TReaderResult">The data reader result set will be mapped an object or property of this type. If TOutParmaters is set to Mapper.DummyType then this must be a single row result of type TModel.</typeparam>
			/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="shardParameterOrdinal">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel, TReaderResult, TOutParameters>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
				where TModel : class, new()
				where TReaderResult : class, new()
				where TOutParameters : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<TShard, TModel, TReaderResult, TOutParameters>, false, null, cancellationToken);

			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
			/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="shardParameterOrdinal">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TOutParameters>
				(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken)
				where TModel : class, new()
				where TReaderResult0 : class, new()
				where TReaderResult1 : class, new()
				where TReaderResult2 : class, new()
				where TOutParameters : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, shardParameterOrdinal, Mapper.QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken);

			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
			/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TOutParameters>
				(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
				where TModel : class, new()
				where TReaderResult0 : class, new()
				where TReaderResult1 : class, new()
				where TReaderResult2 : class, new()
				where TOutParameters : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken);

			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
			/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="shardParameterOrdinal">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TOutParameters>
				(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken)
				where TModel : class, new()
				where TReaderResult0 : class, new()
				where TReaderResult1 : class, new()
				where TReaderResult2 : class, new()
				where TReaderResult3 : class, new()
				where TOutParameters : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, shardParameterOrdinal, Mapper.QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken);

			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
			/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TOutParameters>
				(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
				where TModel : class, new()
				where TReaderResult0 : class, new()
				where TReaderResult1 : class, new()
				where TReaderResult2 : class, new()
				where TReaderResult3 : class, new()
				where TOutParameters : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken);

			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
			/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult4">The fifth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="shardParameterOrdinal">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TOutParameters>
				(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken)
				where TModel : class, new()
				where TReaderResult0 : class, new()
				where TReaderResult1 : class, new()
				where TReaderResult2 : class, new()
				where TReaderResult3 : class, new()
				where TReaderResult4 : class, new()
				where TOutParameters : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, shardParameterOrdinal, Mapper.QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken);

			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
			/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult4">The fifth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TOutParameters>
				(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
				where TModel : class, new()
				where TReaderResult0 : class, new()
				where TReaderResult1 : class, new()
				where TReaderResult2 : class, new()
				where TReaderResult3 : class, new()
				where TReaderResult4 : class, new()
				where TOutParameters : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken);

			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
			/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult4">The fifth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult5">The sixth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="shardParameterOrdinal">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TOutParameters>
				(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken)
				where TModel : class, new()
				where TReaderResult0 : class, new()
				where TReaderResult1 : class, new()
				where TReaderResult2 : class, new()
				where TReaderResult3 : class, new()
				where TReaderResult4 : class, new()
				where TReaderResult5 : class, new()
				where TOutParameters : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, shardParameterOrdinal, Mapper.QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken);

			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
			/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult4">The fifth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult5">The sixth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TOutParameters>
				(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
				where TModel : class, new()
				where TReaderResult0 : class, new()
				where TReaderResult1 : class, new()
				where TReaderResult2 : class, new()
				where TReaderResult3 : class, new()
				where TReaderResult4 : class, new()
				where TReaderResult5 : class, new()
				where TOutParameters : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken);

			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
			/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult4">The fifth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult5">The sixth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult6">The seventh result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="shardParameterOrdinal">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TOutParameters>
				(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken)
				where TModel : class, new()
				where TReaderResult0 : class, new()
				where TReaderResult1 : class, new()
				where TReaderResult2 : class, new()
				where TReaderResult3 : class, new()
				where TReaderResult4 : class, new()
				where TReaderResult5 : class, new()
				where TReaderResult6 : class, new()
				where TOutParameters : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, shardParameterOrdinal, Mapper.QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, Mapper.DummyType, TOutParameters>, false, null, cancellationToken);

			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
			/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult4">The fifth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult5">The sixth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult6">The seventh result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TOutParameters>
				(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
				where TModel : class, new()
				where TReaderResult0 : class, new()
				where TReaderResult1 : class, new()
				where TReaderResult2 : class, new()
				where TReaderResult3 : class, new()
				where TReaderResult4 : class, new()
				where TReaderResult5 : class, new()
				where TReaderResult6 : class, new()
				where TOutParameters : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, Mapper.DummyType, TOutParameters>, false, null, cancellationToken);

			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
			/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult4">The fifth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult5">The sixth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult6">The seventh result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult7">The eighth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="shardParameterOrdinal">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutParameters>
				(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken)
				where TModel : class, new()
				where TReaderResult0 : class, new()
				where TReaderResult1 : class, new()
				where TReaderResult2 : class, new()
				where TReaderResult3 : class, new()
				where TReaderResult4 : class, new()
				where TReaderResult5 : class, new()
				where TReaderResult6 : class, new()
				where TReaderResult7 : class, new()
				where TOutParameters : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, shardParameterOrdinal, Mapper.QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutParameters>, false, null, cancellationToken);

			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
			/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult4">The fifth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult5">The sixth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult6">The seventh result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TReaderResult7">The eighth result set from data reader will be mapped an object or property of this type.</typeparam>
			/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutParameters>
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
				where TOutParameters : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutParameters>, false, null, cancellationToken);


			/// <summary>
			/// Connect to the database and send the result to a custom handler for processing.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="shardParameterOrdinal">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
			/// <param name="resultHandler"></param>
			/// <param name="isTopOne">If only one result is expected from the data ready, set to true. This is a mild optimization.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns>The object created by the delegate handler.</returns>
			public Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, QueryResultModelHandler<TShard, object, TModel> resultHandler, bool isTopOne, CancellationToken cancellationToken) where TModel : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, shardParameterOrdinal, resultHandler, isTopOne, null, cancellationToken);

			/// <summary>
			/// 
			/// </summary>
			/// <typeparam name="TModel"></typeparam>
			/// <param name="sprocName"></param>
			/// <param name="parameters"></param>
			/// <param name="resultHandler"></param>
			/// <param name="isTopOne"></param>
			/// <param name="cancellationToken"></param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<TShard, object, TModel> resultHandler, bool isTopOne, CancellationToken cancellationToken) where TModel : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, resultHandler, isTopOne, null, cancellationToken);

			public Task<TModel> QueryAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) where TModel : class, new()
				=> _manager.QueryAsync<TArg, TModel>(sprocName, parameters, shardParameterOrdinal, resultHandler, isTopOne, optionalArgument, cancellationToken);

			public Task<TModel> QueryAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) where TModel : class, new()
				=> _manager.QueryAsync<TArg, TModel>(sprocName, parameters, -1, resultHandler, isTopOne, optionalArgument, cancellationToken);

			public Task RunAsync(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken)
				=> _manager.RunAsync(sprocName, parameters, shardParameterOrdinal, cancellationToken);

			public Task RunAsync(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
				=> _manager.RunAsync(sprocName, parameters, -1, cancellationToken);
			#endregion

		}
    #endregion
    }
}
