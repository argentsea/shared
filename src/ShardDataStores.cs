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
    /// This class is used by provider specific implementations. It is unlikely that you would call this in consumer code.
    /// This is the generic class that defines connections for sharded data sets.
    /// </summary>
    /// <typeparam name="TShard"></typeparam>
    /// <typeparam name="TConfiguration"></typeparam>
    public class ShardDataStores<TShard, TConfiguration> where TShard : IComparable where TConfiguration : class, IShardDataConfigurationOptions<TShard>, new()

	{
        private readonly ImmutableDictionary<string, SecurityConfiguration> _credentials;
        private readonly ImmutableDictionary<string, DataResilienceConfiguration> _resilienceStrategies;
        private readonly IDataProviderServiceFactory _dataProviderServices;
        private readonly ILogger _logger;

		#region ShardDAtaStores Constructor
		public ShardDataStores(
			IOptions<TConfiguration> configOptions, 
            IOptions<DataSecurityOptions> securityOptions,
			IOptions<DataResilienceOptions> resilienceStrategiesOptions,
            IDataProviderServiceFactory dataProviderServices, 
            ILogger<ShardDataStores<TShard, TConfiguration>> logger)
        {
            this._logger = logger;
			if (configOptions?.Value?.ShardSetsInternal is null)
			{
				throw new Exception("The ShardDataStore object is missing required data connection information. Your application configuration may be missing a data configuration section.");
			}

			this._credentials = DataStoreHelper.BuildCredentialDictionary(securityOptions);
			this._resilienceStrategies = DataStoreHelper.BuildResilienceStrategies(resilienceStrategiesOptions);
			this._dataProviderServices = dataProviderServices;
            this.ShardSets = new ShardDataSets(this, configOptions.Value.ShardSetsInternal);

        }
		#endregion
		public ShardDataSets ShardSets { get; }

        #region Nested classes
        //public class ShardQueryResult
        //{
        //    public ShardQueryResult(TShard shardId, string parameterName, object result)
        //    {
        //        this.ShardId = shardId;
        //        this.ParameterName = parameterName;
        //        this.Result = result;
        //    }
        //    public TShard ShardId { get; private set; }
        //    public string ParameterName { get; private set; }
        //    public object Result { get; private set; }
        //}

        public class ShardDataSets : ICollection
        {
            private readonly object syncRoot = new Lazy<object>();
            private readonly ImmutableDictionary<string, ShardDataSet> dtn;

            private ShardDataSets()
            {
                //hide ctor
            }

            public ShardDataSets(ShardDataStores<TShard, TConfiguration> parent, IShardConnectionsConfiguration<TShard>[] config)
            {
                var bdr = ImmutableDictionary.CreateBuilder<string, ShardDataSet>();
                foreach (var set in config)
                {
                    if (set is null)
                    {
                        throw new Exception($"A shard set configuration is not valid; the configuration provider returned null.");
                    }
                    bdr.Add(set.ShardSetKey, new ShardDataSet(parent, set));
                }
                this.dtn = bdr.ToImmutable();
            }
            public ShardDataSet this[string key]
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
                => this.dtn.Values.ToImmutableList().CopyTo((ShardDataSet[])array, index);

            public IEnumerator GetEnumerator() => this.dtn.GetEnumerator();
        }

        public class ShardDataSet : ICollection
        {
            private readonly object syncRoot = new Lazy<object>();
            private readonly ImmutableDictionary<TShard, ShardInstance> dtn;

            private ShardDataSet() { } //hide ctor 

			public ShardDataSet(ShardDataStores<TShard, TConfiguration> parent, IShardConnectionsConfiguration<TShard> config)
            {
                var bdr = ImmutableDictionary.CreateBuilder<TShard, ShardInstance>();
                foreach (var shd in config.ShardsInternal)
                {
                    if (shd is null)
                    {
                        throw new Exception($"A shard set’s connection configuration was not valid; the configuration provider returned null.");
                    }
                    shd.ReadConnectionInternal.SetSecurity(parent._credentials[config.SecurityKey]);
					shd.WriteConnectionInternal.SetSecurity(parent._credentials[config.SecurityKey]);
					bdr.Add(shd.ShardId, new ShardInstance(parent, shd.ShardId, config.DataResilienceKey, shd.ReadConnectionInternal, shd.WriteConnectionInternal));
                }
                this.dtn = bdr.ToImmutable();
            }

            public TShard ShardId { get; set; }


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

			//public async Task<List<ShardQueryResult>> QueryFirstAsync(string sprocName, TShard[] exclude, IImmutableList<DbParameter> parameters, int shardIdParameterIndex, CancellationToken cancellationToken)
			//{
			//    return null;
			//}

			#region ShardSet (Multiple connection) Queries
			public async Task<TResult> QueryFirstAsync<TArg, TResult>(string sprocName, TShard[] exclude, DbParameterCollection parameters, int shardIdParameterIndex, QueryResultModelHandler<TShard, TArg, TResult> resultHandler, TArg dataObject, CancellationToken cancellationToken) where TResult: class, new()
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
                        if (shardIdParameterIndex >= 0 && shardIdParameterIndex < parameters.Count) // && cmd.Parameters[shardIdParameterIndex].DbType == System.Data.DbType.Byte)
                        {
                            parameters[shardIdParameterIndex].Value = this.ShardId;
                        }

                        tsks.Add(this.dtn[shardId].ReadConnection.QueryAsync<TArg, TResult>(sprocName, parameters, shardIdParameterIndex, resultHandler, true, dataObject, queryCancelationToken.Token));
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
                        catch (OperationCanceledException) {  }
                        catch (AggregateException) { }
                        catch (Exception err)
                        {
                            throw err;
                        }
                    }
                }
                return result;
            }
            public async Task<List<TModel>> QueryAllAsync<TArg, TModel>(string sprocName, TShard[] exclude, DbParameterCollection parameters, int shardIdParameterIndex, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken) where TModel: class, new()

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
                    if (shardIdParameterIndex >= 0 && shardIdParameterIndex < parameters.Count) // && cmd.Parameters[shardIdParameterIndex].DbType == System.Data.DbType.Byte)
                    {
                        parameters[shardIdParameterIndex].Value = this.ShardId;
                    }
                    tsks.Add(this.dtn[shardId].ReadConnection.QueryAsync<TArg, TModel>(sprocName, parameters, shardIdParameterIndex, resultHandler, false, dataObject, cancellationToken));
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
            public async Task<List<TModel>> QueryAllAsync<TModel>(string sprocName, TShard[] exclude, DbParameterCollection parameters, int shardIdParameterIndex, QueryResultModelHandler<TShard, object, TModel> resultHandler, CancellationToken cancellationToken) where TModel : class, new()
				=> await QueryAllAsync<object, TModel>(sprocName, exclude, parameters, shardIdParameterIndex, resultHandler, null, cancellationToken).ConfigureAwait(false);
			#endregion
		}

		public class ShardInstance
		{
			public enum DataAccess
			{
				ReadOnly,
				WriteAccess
			}
			public ShardInstance(ShardDataStores<TShard, TConfiguration> parent, TShard shardId, string resilienceStrategyKey, IConnectionConfiguration readConnection, IConnectionConfiguration writeConnection)
			{
				this.ShardId = shardId;
				this.ReadConnection = new DataConnection(parent, shardId, resilienceStrategyKey, readConnection);
				this.WriteConnection = new DataConnection(parent, shardId, resilienceStrategyKey, writeConnection);
			}
			public TShard ShardId { get; }
			public DataConnection ReadConnection { get; }
			public DataConnection WriteConnection { get; }

		}

		public class DataConnection
		{
			private readonly DataConnectionManager<TShard> _manager;

			private DataConnection() { } //hide ctor


			internal DataConnection(ShardDataStores<TShard, TConfiguration> parent, TShard shardId, string resilienceStrategyKey, IConnectionConfiguration config)
			{
				_manager = new DataConnectionManager<TShard>(shardId, 
					parent._dataProviderServices, 
					resilienceStrategyKey, 
					config.GetConnectionString(), 
					$"shard number { shardId.ToString() } on connection { config.ConnectionDescription }", 
					parent._resilienceStrategies, 
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

		//public class DataConnectionOld
  //      {
  //          private readonly ILogger _logger;
  //          //private readonly Policy _connectPolicy;
  //          private readonly Dictionary<string, Policy> _commandPolicy = new Dictionary<string, Policy>();
  //          private readonly DataResilienceConfiguration _resilienceStrategy;
  //          private readonly IDataProviderServiceFactory _dataProviderServices;
  //          private readonly string _connectionString;
  //          private readonly string _connectionName; //shardset key + shardId.ToString() || dataset key
  //          private readonly TShard _shardId;

  //          private Policy ConnectionPolicy { get; set; }
  //          private Dictionary<string, Policy> CommandPolicies { get; set; } = new Dictionary<string, Policy>();

  //          private DataConnectionOld() {  } //hid ctor

		//	internal DataConnectionOld(ShardDataStores<TShard, TConfiguration> parent, string resilienceStrategyKey, IConnectionConfiguration config)
		//		: this(parent, default(TShard), resilienceStrategyKey, config.ConnectionDescription, config)
		//	{

		//	}

		//	internal DataConnectionOld(ShardDataStores<TShard, TConfiguration> parent, TShard shardId, string resilienceStrategyKey, IConnectionConfiguration config)
  //              : this(parent, default(TShard), resilienceStrategyKey, $"shard number { shardId.ToString() } on connection { config.ConnectionDescription }", config)
  //          {

  //          }

  //          private DataConnectionOld(ShardDataStores<TShard, TConfiguration> parent, TShard shardId,  string resilienceStrategyKey, string connectionName, IConnectionConfiguration config)
  //          {
  //              //this._config = config;
  //              this._logger = parent._logger;
  //              this._dataProviderServices = parent._dataProviderServices;
  //              this._connectionString = config.GetConnectionString();
  //              this._connectionName = config.ConnectionDescription;
  //              this._shardId = shardId;
		//		this._resilienceStrategy = DataStoreHelper.GetResilienceStrategy(parent._resilienceStrategies, resilienceStrategyKey, connectionName, _logger);
  //          }

		//	//private Policy GetCommandResiliencePolicy(string sprocName)
		//	//{
		//	//	Policy result;
		//	//	var retryPolicy = Policy.Handle<DbException>(ex =>
		//	//			this._dataProviderServices.GetIsErrorTransient(ex)
		//	//		)
		//	//		.WaitAndRetryAsync(
		//	//			retryCount: this._resilienceStrategy.RetryCount,
		//	//			sleepDurationProvider: attempt => this._resilienceStrategy.HandleRetryTimespan(attempt),
		//	//			onRetry: (exception, timeSpan, retryCount, context) => this.HandleCommandRetry(sprocName, this._connectionName, retryCount, exception)
		//	//		);

		//	//	if (this._resilienceStrategy.CircuitBreakerFailureCount < 1)
		//	//	{
		//	//		result = retryPolicy;
		//	//	}
		//	//	else
		//	//	{
		//	//		var circuitBreakerPolicy = Policy.Handle<Exception>().CircuitBreakerAsync(
		//	//			exceptionsAllowedBeforeBreaking: this._resilienceStrategy.CircuitBreakerFailureCount,
		//	//			durationOfBreak: TimeSpan.FromMilliseconds(this._resilienceStrategy.CircuitBreakerTestInterval)
		//	//			);
		//	//		result = circuitBreakerPolicy.Wrap(retryPolicy);
		//	//	}
		//	//	return result;
		//	//}

		//	private void HandleCommandRetry(string sprocName, string connectionName, int attempt, Exception exception)
  //          {
  //              this._logger.RetryingDbCommand(sprocName, connectionName, attempt, exception);

  //          }
  //          private void HandleConnectionRetry(string connectionName, int attempt, Exception exception)
  //          {
  //              this._logger.RetryingDbConnection(connectionName, attempt, exception);

  //          }

		//	//private async static Task<DbDataReader> ExecuteReaderWithRetryAsync(DbCommand command)
		//	//{
		//	//    //GuardConnectionIsNotNull(command);

		//	//    var policy = Polly.Policy.Handle<Exception>().WaitAndRetryAsync(
		//	//        retryCount: 3, // Retry 3 times
		//	//        sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)), // Exponential backoff based on an initial 200ms delay.
		//	//        onRetry: (exception, attempt) =>
		//	//        {
		//	//        // Capture some info for logging/telemetry.  
		//	//        logger.LogWarn($"ExecuteReaderWithRetryAsync: Retry {attempt} due to {exception}.");
		//	//        });

		//	//    // Retry the following call according to the policy.
		//	//    await policy.ExecuteAsync<SqlDataReader>(async token =>
		//	//    {
		//	//        // This code is executed within the Policy 

		//	//        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync(token).ConfigureAwait(false);
		//	//        return await command.ExecuteReaderAsync(System.Data.CommandBehavior.Default, token).ConfigureAwait(false);

		//	//    }, cancellationToken);
		//	//}


		//	private static readonly double TimestampToMilliseconds = (double)TimeSpan.TicksPerSecond / (Stopwatch.Frequency * TimeSpan.TicksPerMillisecond);

		//	#endregion
		//	#region Public properties
		//	public List<Exception> SqlExceptionsEncountered { get; } = new List<Exception>();

		//	public string ConnectionString { get => this._connectionString; }

		//	#endregion


		//	#region Public data fetch methods
		//	public async Task<TModel> QueryAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, int shardIdParameterIndex, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) where TModel : class, new()

		//	{

  //              cancellationToken.ThrowIfCancellationRequested();
  //              var startTimestamp = Stopwatch.GetTimestamp();
  //              SqlExceptionsEncountered.Clear();
                
  //              if (!this._commandPolicy.ContainsKey(sprocName))
  //              {
  //                  //this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
		//			this._commandPolicy.Add(sprocName, DataStoreHelper.GetCommandResiliencePolicy(sprocName, _dataProviderServices, this._resilienceStrategy, this._connectionName, this.HandleCommandRetry));
		//		}
		//		var result = await this._commandPolicy[sprocName].ExecuteAsync(newToken => 
		//			this.QueryDatabaseAsync<TArg, TModel>(sprocName, parameters, resultHandler, shardIdParameterIndex, isTopOne, optionalArgument, newToken), cancellationToken).ConfigureAwait(false);

  //              var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
  //              _logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
  //              return result;
  //          }

  //          private async Task<TModel> QueryDatabaseAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, int shardIdParameterIndex, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) where TModel : class, new()

		//	{
		//		TModel result = default(TModel);
  //              cancellationToken.ThrowIfCancellationRequested();
  //              SqlExceptionsEncountered.Clear();
  //              using (var connection = this._dataProviderServices.NewConnection(this._connectionString))
  //              {
  //                  await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken)).ConfigureAwait(false);
  //                  cancellationToken.ThrowIfCancellationRequested();
  //                  using (var cmd = this._dataProviderServices.NewCommand(sprocName, connection))
  //                  {
  //                      cmd.CommandType = System.Data.CommandType.StoredProcedure;
  //                      this._dataProviderServices.SetParameters(cmd, parameters);
  //                      if (shardIdParameterIndex >= 0 && shardIdParameterIndex < cmd.Parameters.Count)
  //                      {
  //                          cmd.Parameters[shardIdParameterIndex].Value = this._shardId;
  //                      }
  //                      var cmdType = System.Data.CommandBehavior.Default;
  //                      if (isTopOne)
  //                      {
  //                          cmdType = System.Data.CommandBehavior.SingleRow;
  //                      }
  //                      using (var dataReader = await cmd.ExecuteReaderAsync(cmdType, cancellationToken).ConfigureAwait(false))
  //                      {
  //                          cancellationToken.ThrowIfCancellationRequested();

		//					result = resultHandler(this._shardId, sprocName, optionalArgument, dataReader, cmd.Parameters, _connectionName, this._logger);
  //                      }
  //                  }
  //              }
  //              return result;
  //          }
  //      }

    #endregion
    }
}
