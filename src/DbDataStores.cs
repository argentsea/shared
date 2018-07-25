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
    /// This generic class manages non-sharded database connections.
    /// </summary>
    /// <typeparam name="TConfiguration">The provider-specific connection implementation.</typeparam>
    public class DbDataStores<TConfiguration> where TConfiguration : class, IDbDataConfigurationOptions, new()
	{
        private readonly ImmutableDictionary<string, SecurityConfiguration> _credentials;
        private readonly ImmutableDictionary<string, DataResilienceConfiguration> _resilienceStrategies;
        private readonly IDataProviderServiceFactory _dataProviderServices;
        private readonly ILogger _logger;

		#region DbDataStores Constructor
		public DbDataStores(
			IOptions<TConfiguration> configOptions,
			IOptions<DataSecurityOptions> securityOptions,
			IOptions<DataResilienceOptions> resilienceStrategiesOptions,
			IDataProviderServiceFactory dataProviderServices,
			ILogger<DbDataStores<TConfiguration>> logger)
		{
			this._logger = logger;

			if (configOptions?.Value?.DbConnectionsInternal is null)
			{
				throw new Exception("The DbDataStore object is missing required data connection information. Your application configuration may be missing a data configuration section.");
			}

			this._credentials = DataStoreHelper.BuildCredentialDictionary(securityOptions);
			this._resilienceStrategies = DataStoreHelper.BuildResilienceStrategies(resilienceStrategiesOptions);
			this._dataProviderServices = dataProviderServices;
			this.DbConnections = new DbDataSets(this, configOptions.Value.DbConnectionsInternal);
        }
		#endregion
		public DbDataSets DbConnections { get; }

		#region Nested classes
		public class DbDataSets : ICollection
		{
			private readonly object syncRoot = new Lazy<object>();
			private readonly ImmutableDictionary<string, DataConnection> dtn;

			public DbDataSets()
			{
				//hide ctor
			}
			public DbDataSets(DbDataStores<TConfiguration> parent, IDbConnectionConfiguration[] config)
			{
				var bdr = ImmutableDictionary.CreateBuilder<string, DataConnection>();

				foreach (var db in config)
				{
                    if (db is null)
                    {
                        throw new Exception($"A database connection configuration was not valid; the configuration provider returned null.");
                    }
                    if (!parent._credentials.TryGetValue(db.SecurityKey, out var secCfg))
					{
						throw new Exception($"Connection {db.DataConnectionInternal.ConnectionDescription} specifies a security key that could not be found in the “Credentials” list.");
					}
					db.DataConnectionInternal.SetSecurity(secCfg);
					bdr.Add(db.DatabaseKey, new DataConnection(parent, db.DataResilienceKey, db.DataConnectionInternal));
				}
				this.dtn = bdr.ToImmutable();
			}
			public DataConnection this[string key]
			{
				get { return dtn[key]; }
			}

			public int Count
			{
				get { return dtn.Count; }
			}

			public bool IsSynchronized => true;

			public object SyncRoot => syncRoot;

			public void CopyTo(Array array, int index)
				=> this.dtn.Values.ToImmutableList().CopyTo((DataConnection[])array, index);

			public IEnumerator GetEnumerator() => this.dtn.GetEnumerator();
		}

		public class DataConnection
		{
			private readonly DataConnectionManager<int> _manager;

			private DataConnection() { } //hide ctor

			internal DataConnection(DbDataStores<TConfiguration> parent, string resilienceStrategyKey, IConnectionConfiguration config)
			{
				_manager = new DataConnectionManager<int>(0, parent._dataProviderServices,
					resilienceStrategyKey, config.GetConnectionString(), config.ConnectionDescription, parent._resilienceStrategies, parent._logger);
			}
			public string ConnectionString { get => _manager.ConnectionString; }

			#region Public data fetch methods
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
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
				where TModel : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<int, TModel, Mapper.DummyType, TModel>, false, null, cancellationToken);

			/// <summary>
			/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
			/// </summary>
			/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
			/// <typeparam name="TReaderResult">The data reader result set will be mapped an object or property of this type. If TOutParmaters is set to Mapper.DummyType then this must be a single row result of type TModel.</typeparam>
			/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
			/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
			/// <param name="parameters">The query parameters.</param>
			/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
			/// <returns></returns>
			public Task<TModel> QueryAsync<TModel, TReaderResult, TOutParameters>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
				where TModel : class, new()
				where TReaderResult : class, new()
				where TOutParameters : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<int, TModel, TReaderResult, TOutParameters>, false, null, cancellationToken);

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
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<int, TModel, TReaderResult0, TReaderResult1, TReaderResult2, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken);

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
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<int, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken);

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
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<int, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken);

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
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<int, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken);

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
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<int, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, Mapper.DummyType, TOutParameters>, false, null, cancellationToken);

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
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, Mapper.QueryResultsHandler<int, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutParameters>, false, null, cancellationToken);

			public Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<int, object, TModel> resultHandler, bool isTopOne, CancellationToken cancellationToken) where TModel : class, new()
				=> _manager.QueryAsync<object, TModel>(sprocName, parameters, -1, resultHandler, isTopOne, null, cancellationToken);

			public Task<TModel> QueryAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<int, TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) where TModel : class, new()
				=> _manager.QueryAsync<TArg, TModel>(sprocName, parameters, -1, resultHandler, isTopOne, optionalArgument, cancellationToken);


			public Task RunAsync(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
				=> _manager.RunAsync(sprocName, parameters, -1, cancellationToken);

			#endregion
		}


		//public class DataConnectionOld
		//{
		//	#region Private variables and constructors
		//	private readonly ILogger _logger;
  //          //private readonly Policy _connectPolicy;
  //          private readonly Dictionary<string, Policy> _commandPolicy = new Dictionary<string, Policy>();
  //          private readonly DataResilienceConfiguration _resilienceStrategy;
  //          private readonly IDataProviderServiceFactory _dataProviderServices;
  //          private readonly string _connectionString;
  //          private readonly string _connectionName;

  //          private Policy ConnectionPolicy { get; set; }
  //          private Dictionary<string, Policy> CommandPolicies { get; set; } = new Dictionary<string, Policy>();

  //          private DataConnectionOld() {  } //hide ctor

		//	////internal DataConnection(DbDataStores<TConfiguration> parent, string resilienceStrategyKey, IConnectionConfiguration config)
		//	////    : this(parent, resilienceStrategyKey, config.ConnectionDescription, config)
		//	////{
		//	////
		//	////}

		//	//private DataConnection(DbDataStores<TConfiguration> parent, string resilienceStrategyKey, string connectionName, IConnectionConfiguration config)
		//	internal DataConnectionOld(DbDataStores<TConfiguration> parent, string resilienceStrategyKey, IConnectionConfiguration config)
		//	{
		//		var connectionName = config.ConnectionDescription;
		//		this._logger = parent._logger;
  //              this._dataProviderServices = parent._dataProviderServices;
  //              this._connectionString = config.GetConnectionString();
  //              this._connectionName = config.ConnectionDescription;
		//		this._resilienceStrategy = DataStoreHelper.GetResilienceStrategy(parent._resilienceStrategies, resilienceStrategyKey, connectionName, _logger);
		//	}
		//	#endregion
		//	#region Private helper methods
		//	//private Policy GetCommandResiliencePolicy(string sprocName)
  // //         {
  // //             Policy result;
  // //             var retryPolicy = Policy.Handle<DbException>(ex =>
  // //                     this._dataProviderServices.GetIsErrorTransient(ex)
  // //                 )
  // //                 .WaitAndRetryAsync(
  // //                     retryCount: this._resilienceStrategy.RetryCount,
  // //                     sleepDurationProvider: attempt => this._resilienceStrategy.HandleRetryTimespan(attempt),
  // //                     onRetry: (exception, timeSpan, retryCount, context) => this.HandleCommandRetry(sprocName, this._connectionName, retryCount, exception)
  // //                 );

  // //             if (this._resilienceStrategy.CircuitBreakerFailureCount < 1)
  // //             {
  // //                 result = retryPolicy;
  // //             }
  // //             else
  // //             {
  // //                 var circuitBreakerPolicy = Policy.Handle<Exception>().CircuitBreakerAsync(
  // //                     exceptionsAllowedBeforeBreaking: this._resilienceStrategy.CircuitBreakerFailureCount,
  // //                     durationOfBreak: TimeSpan.FromMilliseconds(this._resilienceStrategy.CircuitBreakerTestInterval)
  // //                     );
  // //                 result = circuitBreakerPolicy.Wrap(retryPolicy);
  // //             }
  // //             return result;
  // //         }

  //          private void HandleCommandRetry(string sprocName, string connectionName, int attempt, Exception exception)
  //          {
  //              this._logger.RetryingDbCommand(sprocName, connectionName, attempt, exception);

  //          }
  //          private void HandleConnectionRetry(string connectionName, int attempt, Exception exception)
  //          {
  //              this._logger.RetryingDbConnection(connectionName, attempt, exception);

  //          }

		//	//private static readonly double TimestampToMilliseconds = (double)TimeSpan.TicksPerSecond / (Stopwatch.Frequency * TimeSpan.TicksPerMillisecond);

		//	#endregion
		//	#region Public properties
		//	public List<Exception> SqlExceptionsEncountered { get; } = new List<Exception>();
 
		//	public string ConnectionString { get => this._connectionString; }
		//	#endregion


		//	#region Public data fetch methods
		//	/// <summary>
		//	/// Connect to the database and return a single value.
		//	/// </summary>
		//	/// <typeparam name="TValue">The expected type of the return value.</typeparam>
		//	/// <param name="sprocName">The stored procedure to call to fetch the value.</param>
		//	/// <param name="parameters">A parameters collction. Input parameters may be used to find the parameter; will return the value of the first output (or input/output) parameter. If TValue is an int, will also return the sproc return value.</param>
		//	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		//	/// <returns>The retrieved value.</returns>
		//	public async Task<TValue> LookupAsync<TValue>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
		//	{
		//		return await DataStoreHelper.AdoExecute<TValue>(sprocName, parameters, this._commandPolicy, this._dataProviderServices, this._resilienceStrategy, this._connectionName, this.HandleCommandRetry, this._logger, cancellationToken);
		//		//cancellationToken.ThrowIfCancellationRequested();
		//		//var startTimestamp = Stopwatch.GetTimestamp();
		//		//SqlExceptionsEncountered.Clear();

		//		//if (!this._commandPolicy.ContainsKey(sprocName))
		//		//{
		//		//	this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
		//		//}
		//		//var result = await this._commandPolicy[sprocName].ExecuteAsync(newToken =>
		//		//	ExecuteQueryToValueAsync<TValue>(sprocName, parameters, newToken), cancellationToken).ConfigureAwait(false);

		//		//var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
		//		//_logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
		//		//return result;
		//	}

		//	/// <summary>
		//	/// Connect to the database and return the values as a list of objects.
		//	/// </summary>
		//	/// <typeparam name="TResult">The type of object to be listed.</typeparam>
		//	/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
		//	/// <param name="parameters">The query parameters.</param>
		//	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		//	/// <returns>A list containing an object for each data row.</returns>
		//	public async Task<IList<TResult>> ListAsync<TResult>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken) where TResult : class, new()
		//	{
		//		cancellationToken.ThrowIfCancellationRequested();
		//		var startTimestamp = Stopwatch.GetTimestamp();
		//		//SqlExceptionsEncountered.Clear();

		//		if (!this._commandPolicy.ContainsKey(sprocName))
		//		{
		//			this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
		//		}
		//		var result = await this._commandPolicy[sprocName].ExecuteAsync(newToken =>
		//			ExecuteQueryToListAsync<TResult>(sprocName, parameters, newToken), cancellationToken).ConfigureAwait(false);

		//		var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
		//		_logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
		//		return result;
		//	}


		//	/// <summary>
		//	/// Connect to the database and return an object of the specified type built from the corresponding output parameters.
		//	/// </summary>
		//	/// <typeparam name="TModel">The type of the object to be returned.</typeparam>
		//	/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
		//	/// <param name="parameters">The query parameters.</param>
		//	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		//	/// <returns></returns>
		//	public async Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
		//		where TModel : class, new()
		//		=> await QueryAsync<object, TModel>(sprocName, parameters, Mapper.QueryResultsHandler<byte, TModel, Mapper.DummyType, TModel>, false, null, cancellationToken).ConfigureAwait(false);

		//	/// <summary>
		//	/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
		//	/// </summary>
		//	/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
		//	/// <typeparam name="TReaderResult">The data reader result set will be mapped an object or property of this type. If TOutParmaters is set to Mapper.DummyType then this must be a single row result of type TModel.</typeparam>
		//	/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
		//	/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
		//	/// <param name="parameters">The query parameters.</param>
		//	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		//	/// <returns></returns>
		//	public async Task<TModel> QueryAsync<TModel, TReaderResult, TOutParameters>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
		//		where TModel : class, new()
		//		where TReaderResult : class, new()
		//		where TOutParameters : class, new()
		//		=> await QueryAsync<object, TModel>(sprocName, parameters, Mapper.QueryResultsHandler<byte, TModel, TReaderResult, TOutParameters>, false, null, cancellationToken).ConfigureAwait(false);

		//	/// <summary>
		//	/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
		//	/// </summary>
		//	/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
		//	/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
		//	/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
		//	/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
		//	/// <param name="parameters">The query parameters.</param>
		//	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		//	/// <returns></returns>
		//	public async Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TOutParameters>
		//		(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
		//		where TModel : class, new()
		//		where TReaderResult0 : class, new()
		//		where TReaderResult1 : class, new()
		//		where TReaderResult2 : class, new()
		//		where TOutParameters : class, new()
		//		=> await QueryAsync<object, TModel>(sprocName, parameters, Mapper.QueryResultsHandler<byte, TModel, TReaderResult0, TReaderResult1, TReaderResult2, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken).ConfigureAwait(false);

		//	/// <summary>
		//	/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
		//	/// </summary>
		//	/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
		//	/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
		//	/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
		//	/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
		//	/// <param name="parameters">The query parameters.</param>
		//	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		//	/// <returns></returns>
		//	public async Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TOutParameters>
		//		(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
		//		where TModel : class, new()
		//		where TReaderResult0 : class, new()
		//		where TReaderResult1 : class, new()
		//		where TReaderResult2 : class, new()
		//		where TReaderResult3 : class, new()
		//		where TOutParameters : class, new()
		//		=> await QueryAsync<object, TModel>(sprocName, parameters, Mapper.QueryResultsHandler<byte, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken).ConfigureAwait(false);

		//	/// <summary>
		//	/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
		//	/// </summary>
		//	/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
		//	/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
		//	/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult4">The fifth result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
		//	/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
		//	/// <param name="parameters">The query parameters.</param>
		//	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		//	/// <returns></returns>
		//	public async Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TOutParameters>
		//		(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
		//		where TModel : class, new()
		//		where TReaderResult0 : class, new()
		//		where TReaderResult1 : class, new()
		//		where TReaderResult2 : class, new()
		//		where TReaderResult3 : class, new()
		//		where TReaderResult4 : class, new()
		//		where TOutParameters : class, new()
		//		=> await QueryAsync<object, TModel>(sprocName, parameters, Mapper.QueryResultsHandler<byte, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken).ConfigureAwait(false);

		//	/// <summary>
		//	/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
		//	/// </summary>
		//	/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
		//	/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
		//	/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult4">The fifth result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult5">The sixth result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
		//	/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
		//	/// <param name="parameters">The query parameters.</param>
		//	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		//	/// <returns></returns>
		//	public async Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TOutParameters>
		//		(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
		//		where TModel : class, new()
		//		where TReaderResult0 : class, new()
		//		where TReaderResult1 : class, new()
		//		where TReaderResult2 : class, new()
		//		where TReaderResult3 : class, new()
		//		where TReaderResult4 : class, new()
		//		where TReaderResult5 : class, new()
		//		where TOutParameters : class, new()
		//		=> await QueryAsync<object, TModel>(sprocName, parameters, Mapper.QueryResultsHandler<byte, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, Mapper.DummyType, Mapper.DummyType, TOutParameters>, false, null, cancellationToken).ConfigureAwait(false);

		//	/// <summary>
		//	/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
		//	/// </summary>
		//	/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
		//	/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
		//	/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult4">The fifth result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult5">The sixth result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult6">The seventh result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
		//	/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
		//	/// <param name="parameters">The query parameters.</param>
		//	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		//	/// <returns></returns>
		//	public async Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TOutParameters>
		//		(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
		//		where TModel : class, new()
		//		where TReaderResult0 : class, new()
		//		where TReaderResult1 : class, new()
		//		where TReaderResult2 : class, new()
		//		where TReaderResult3 : class, new()
		//		where TReaderResult4 : class, new()
		//		where TReaderResult5 : class, new()
		//		where TReaderResult6 : class, new()
		//		where TOutParameters : class, new()
		//		=> await QueryAsync<object, TModel>(sprocName, parameters, Mapper.QueryResultsHandler<byte, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, Mapper.DummyType, TOutParameters>, false, null, cancellationToken).ConfigureAwait(false);

		//	/// <summary>
		//	/// Connect to the database and return an object of the specified type built from the corresponding data reader results and/or output parameters.
		//	/// </summary>
		//	/// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
		//	/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type..</typeparam>
		//	/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult4">The fifth result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult5">The sixth result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult6">The seventh result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TReaderResult7">The eighth result set from data reader will be mapped an object or property of this type.</typeparam>
		//	/// <typeparam name="TOutParameters">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
		//	/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
		//	/// <param name="parameters">The query parameters.</param>
		//	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		//	/// <returns></returns>
		//	public async Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutParameters>
		//		(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken) 
		//		where TModel : class, new()
		//		where TReaderResult0 : class, new()
		//		where TReaderResult1 : class, new()
		//		where TReaderResult2 : class, new()
		//		where TReaderResult3 : class, new()
		//		where TReaderResult4 : class, new()
		//		where TReaderResult5 : class, new()
		//		where TReaderResult6 : class, new()
		//		where TReaderResult7 : class, new()
		//		where TOutParameters : class, new()
		//		=> await QueryAsync<object, TModel>(sprocName, parameters, Mapper.QueryResultsHandler<byte, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutParameters>, false, null, cancellationToken).ConfigureAwait(false);

		//	public async Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<byte, object, TModel> resultHandler, bool isTopOne, CancellationToken cancellationToken) where TModel : class, new()
		//		=> await QueryAsync<object, TModel>(sprocName, parameters, resultHandler, isTopOne, null, cancellationToken).ConfigureAwait(false);

		//	public async Task<TModel> QueryAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<byte, TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) where TModel : class, new()
  //          {
  //              cancellationToken.ThrowIfCancellationRequested();
  //              var startTimestamp = Stopwatch.GetTimestamp();
  //              SqlExceptionsEncountered.Clear();
                
  //              if (!this._commandPolicy.ContainsKey(sprocName))
  //              {
  //                  this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
  //              }
  //              var result = await this._commandPolicy[sprocName].ExecuteAsync(newToken => 
		//			this.ExecuteQueryWithDelegateAsync<TModel, TArg>(sprocName, parameters, resultHandler, isTopOne, optionalArgument, newToken), cancellationToken).ConfigureAwait(false);

  //              var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
  //              _logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
  //              return result;
  //          }



		//	public async Task RunAsync(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
		//	{

		//		cancellationToken.ThrowIfCancellationRequested();
		//		var startTimestamp = Stopwatch.GetTimestamp();
		//		SqlExceptionsEncountered.Clear();

		//		if (!this._commandPolicy.ContainsKey(sprocName))
		//		{
		//			this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
		//		}
		//		await this._commandPolicy[sprocName].ExecuteAsync(newToken => this.ExecuteRunAsync(sprocName, parameters, newToken), cancellationToken).ConfigureAwait(false);

		//		var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
		//		_logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
		//		//return result;
		//	}



		//	#endregion
		//	#region Private Handlers

		//	private async Task<IList<TResult>> ExecuteQueryToListAsync<TResult>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken) where TResult : class, new()
		//	{
		//		IList<TResult> result = null;
		//		cancellationToken.ThrowIfCancellationRequested();
		//		//SqlExceptionsEncountered.Clear();
		//		using (var connection = this._dataProviderServices.NewConnection(this._connectionString))
		//		{
		//			//await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken)).ConfigureAwait(false);
		//			await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
		//			cancellationToken.ThrowIfCancellationRequested();
		//			using (var cmd = this._dataProviderServices.NewCommand(sprocName, connection))
		//			{
		//				cmd.CommandType = System.Data.CommandType.StoredProcedure;
		//				this._dataProviderServices.SetParameters(cmd, parameters);
		//				var cmdType = System.Data.CommandBehavior.Default;
		//				using (var dataReader = await cmd.ExecuteReaderAsync(cmdType, cancellationToken).ConfigureAwait(false))
		//				{
		//					cancellationToken.ThrowIfCancellationRequested();
		//					result = Mapper.FromDataReader<TResult>(dataReader, _logger);
		//				}
		//			}
		//		}
		//		if (result is null)
		//		{
		//			result = new List<TResult>();
		//		}
		//		return result;
		//	}
		//	private async Task<TResult> ExecuteQueryToValueAsync<TResult>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
		//	{
		//		TResult result = default(TResult);
		//		cancellationToken.ThrowIfCancellationRequested();
		//		//SqlExceptionsEncountered.Clear();
		//		using (var connection = this._dataProviderServices.NewConnection(this._connectionString))
		//		{
		//			//await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken)).ConfigureAwait(false);
		//			await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
		//			cancellationToken.ThrowIfCancellationRequested();
		//			using (var cmd = this._dataProviderServices.NewCommand(sprocName, connection))
		//			{
		//				cmd.CommandType = System.Data.CommandType.StoredProcedure;
		//				this._dataProviderServices.SetParameters(cmd, parameters);
		//				await cmd.ExecuteNonQueryAsync(cancellationToken);
		//				foreach (DbParameter prm in parameters)
		//				{
		//					if (result is int && prm.Direction == System.Data.ParameterDirection.ReturnValue && !System.DBNull.Value.Equals(prm.Value))
		//					{
		//						result = (dynamic)prm.Value;
		//						return result;
		//					}
		//					else if (prm.Direction == System.Data.ParameterDirection.Output || prm.Direction == System.Data.ParameterDirection.InputOutput)
		//					{
		//						if (result is Nullable && System.DBNull.Value.Equals(prm.Value))
		//						{
		//							result = (dynamic)null;
		//							return result;
		//						}
		//						else
		//						{
		//							result = (dynamic)prm.Value;
		//							return result;
		//						}
		//					}
		//				}
		//			}
		//		}
		//		throw new UnexpectedSqlResultException($"Database query {sprocName} expected to output a type of {typeof(TResult).ToString()}, but no output values were found.");
		//	}

		//	private async Task<TResult> ExecuteQueryWithDelegateAsync<TResult, TArg>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<byte, TArg, TResult> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) where TResult : class, new()
		//	{
		//		var result = default(TResult);
		//		cancellationToken.ThrowIfCancellationRequested();
		//		SqlExceptionsEncountered.Clear();
		//		using (var connection = this._dataProviderServices.NewConnection(this._connectionString))
		//		{
		//			//await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken)).ConfigureAwait(false);
		//			await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

		//			cancellationToken.ThrowIfCancellationRequested();
		//			using (var cmd = this._dataProviderServices.NewCommand(sprocName, connection))
		//			{
		//				cmd.CommandType = System.Data.CommandType.StoredProcedure;
		//				this._dataProviderServices.SetParameters(cmd, parameters);
		//				var cmdType = System.Data.CommandBehavior.Default;
		//				if (isTopOne)
		//				{
		//					cmdType = System.Data.CommandBehavior.SingleRow;
		//				}
		//				using (var dataReader = await cmd.ExecuteReaderAsync(cmdType, cancellationToken).ConfigureAwait(false))
		//				{
		//					cancellationToken.ThrowIfCancellationRequested();

		//					result = resultHandler(0, sprocName, optionalArgument, dataReader, cmd.Parameters, _connectionName, this._logger);
		//				}
		//			}
		//		}
		//		return result;
		//	}

		//	private async Task ExecuteRunAsync(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
		//	{
		//		cancellationToken.ThrowIfCancellationRequested();
		//		SqlExceptionsEncountered.Clear();
		//		using (var connection = this._dataProviderServices.NewConnection(this._connectionString))
		//		{
		//			//await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken)).ConfigureAwait(false);
		//			await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

		//			cancellationToken.ThrowIfCancellationRequested();
		//			using (var cmd = this._dataProviderServices.NewCommand(sprocName, connection))
		//			{
		//				cmd.CommandType = System.Data.CommandType.StoredProcedure;
		//				this._dataProviderServices.SetParameters(cmd, parameters);
		//				await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
		//			}
		//		}
		//	}
		//	#endregion
		//}

		#endregion
    }
}
