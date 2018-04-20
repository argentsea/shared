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
			if (securityOptions?.Value?.Credentials is null)
			{
				throw new Exception("The DbDataStore object cannot obtain required security information. Your application configuration may be missing the “Credentials” section.");
			}

			var sbdr = ImmutableDictionary.CreateBuilder<string, SecurityConfiguration>();
			foreach (var crd in securityOptions.Value.Credentials)
			{
				sbdr.Add(crd.SecurityKey, crd);
			}
			this._credentials = sbdr.ToImmutable();

			var rbdr = ImmutableDictionary.CreateBuilder<string, DataResilienceConfiguration>();
			if (resilienceStrategiesOptions?.Value?.DataResilienceStrategies is null)
			{
				rbdr.Add(string.Empty, new DataResilienceConfiguration());
			}
			else
			{
				foreach (var rs in resilienceStrategiesOptions.Value.DataResilienceStrategies)
				{
					rbdr.Add(rs.DataResilienceKey, rs);
				}
			}
			this._resilienceStrategies = rbdr.ToImmutable();
			this._dataProviderServices = dataProviderServices;
			this.DbConnections = new DbDataSets(this, configOptions.Value.DbConnectionsInternal);
        }
		#endregion
		public DbDataSets DbConnections { get; }

        #region Nested classes

        public class DataConnection
		{
			#region Private variables and constructors
			private readonly ILogger _logger;
            //private readonly Policy _connectPolicy;
            private readonly Dictionary<string, Policy> _commandPolicy = new Dictionary<string, Policy>();
            private readonly DataResilienceConfiguration _resilienceStrategy;
            private readonly IDataProviderServiceFactory _dataProviderServices;
            private readonly string _connectionString;
            private readonly string _connectionName;

            private Policy ConnectionPolicy { get; set; }
            private Dictionary<string, Policy> CommandPolicies { get; set; } = new Dictionary<string, Policy>();

            private DataConnection() {  } //hide ctor

            internal DataConnection(DbDataStores<TConfiguration> parent, string resilienceStrategyKey, IConnectionConfiguration config)
                : this(parent, resilienceStrategyKey, config.ConnectionDescription, config)
            {

            }

            private DataConnection(DbDataStores<TConfiguration> parent, string resilienceStrategyKey, string connectionName, IConnectionConfiguration config)
            {
                this._logger = parent._logger;
                this._dataProviderServices = parent._dataProviderServices;
                this._connectionString = config.GetConnectionString();
                this._connectionName = config.ConnectionDescription;
                this._resilienceStrategy = new DataResilienceConfiguration(); //initialize to defaults
				if (parent._resilienceStrategies != null && parent._resilienceStrategies.Count > 0 && !string.IsNullOrEmpty(resilienceStrategyKey))
				{
					if (!parent._resilienceStrategies.TryGetValue(resilienceStrategyKey, out var rstrategy))
					{
						_logger.LogWarning($"Connection {connectionName} specifies a resiliance strategy that could not be found in the list of configured strategies. Using a default strategy instead.");
						rstrategy = new DataResilienceConfiguration();
					}
					this._resilienceStrategy = rstrategy;
				}
				//var retryPolicy = Policy.Handle<DbException>(ex =>
    //                    parent._dataProviderServices.GetIsErrorTransient(ex)
    //                )
    //                .WaitAndRetryAsync(
    //                    retryCount: this._resilienceStrategy.RetryCount,
    //                    sleepDurationProvider: attempt => this._resilienceStrategy.HandleRetryTimespan(attempt),
    //                    onRetry: (exception, timeSpan, retryCount, context) => this.HandleConnectionRetry(this._connectionName, retryCount, exception)
    //                );

    //            if (this._resilienceStrategy.CircuitBreakerFailureCount < 1)
    //            {
    //                this._connectPolicy = retryPolicy;
    //            }
    //            else
    //            {
    //                var circuitBreakerPolicy = Policy.Handle<Exception>().CircuitBreakerAsync(
    //                    exceptionsAllowedBeforeBreaking: this._resilienceStrategy.CircuitBreakerFailureCount,
    //                    durationOfBreak: TimeSpan.FromMilliseconds(this._resilienceStrategy.CircuitBreakerTestInterval)
    //                    );
    //                this._connectPolicy = circuitBreakerPolicy.Wrap(retryPolicy);
    //            }
            }
			#endregion
			#region Private helper methods
			private Policy GetCommandResiliencePolicy(string sprocName)
            {
                Policy result;
                var retryPolicy = Policy.Handle<DbException>(ex =>
                        this._dataProviderServices.GetIsErrorTransient(ex)
                    )
                    .WaitAndRetryAsync(
                        retryCount: this._resilienceStrategy.RetryCount,
                        sleepDurationProvider: attempt => this._resilienceStrategy.HandleRetryTimespan(attempt),
                        onRetry: (exception, timeSpan, retryCount, context) => this.HandleCommandRetry(sprocName, this._connectionName, retryCount, exception)
                    );

                if (this._resilienceStrategy.CircuitBreakerFailureCount < 1)
                {
                    result = retryPolicy;
                }
                else
                {
                    var circuitBreakerPolicy = Policy.Handle<Exception>().CircuitBreakerAsync(
                        exceptionsAllowedBeforeBreaking: this._resilienceStrategy.CircuitBreakerFailureCount,
                        durationOfBreak: TimeSpan.FromMilliseconds(this._resilienceStrategy.CircuitBreakerTestInterval)
                        );
                    result = circuitBreakerPolicy.Wrap(retryPolicy);
                }
                return result;
            }

            private void HandleCommandRetry(string sprocName, string connectionName, int attempt, Exception exception)
            {
                this._logger.RetryingDbCommand(sprocName, connectionName, attempt, exception);

            }
            private void HandleConnectionRetry(string connectionName, int attempt, Exception exception)
            {
                this._logger.RetryingDbConnection(connectionName, attempt, exception);

            }

			private static readonly double TimestampToMilliseconds = (double)TimeSpan.TicksPerSecond / (Stopwatch.Frequency * TimeSpan.TicksPerMillisecond);

			#endregion
			#region Public properties
			public List<Exception> SqlExceptionsEncountered { get; } = new List<Exception>();
 
			public string ConnectionString { get => this._connectionString; }
			#endregion
			#region Query methods
			public async Task<IList<TResult>> QueryToListAsync<TResult>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken) where TResult : class, new()
			{
				cancellationToken.ThrowIfCancellationRequested();
				var startTimestamp = Stopwatch.GetTimestamp();
				SqlExceptionsEncountered.Clear();

				if (!this._commandPolicy.ContainsKey(sprocName))
				{
					this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
				}
				var result = await this._commandPolicy[sprocName].ExecuteAsync(newToken =>
					ExecuteQueryToListAsync<TResult>(sprocName, parameters, newToken), cancellationToken).ConfigureAwait(false);

				var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
				_logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
				return result;
			}

			private async Task<IList<TResult>> ExecuteQueryToListAsync<TResult>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken) where TResult: class, new()
			{
				IList<TResult> result = null;
				cancellationToken.ThrowIfCancellationRequested();
				SqlExceptionsEncountered.Clear();
				using (var connection = this._dataProviderServices.NewConnection(this._connectionString))
				{
					//await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken)).ConfigureAwait(false);
					await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
					cancellationToken.ThrowIfCancellationRequested();
					using (var cmd = this._dataProviderServices.NewCommand(sprocName, connection))
					{
						cmd.CommandType = System.Data.CommandType.StoredProcedure;
						this._dataProviderServices.SetParameters(cmd, parameters);
						var cmdType = System.Data.CommandBehavior.Default;
						using (var dataReader = await cmd.ExecuteReaderAsync(cmdType, cancellationToken).ConfigureAwait(false))
						{
							cancellationToken.ThrowIfCancellationRequested();
							result = Mapper.FromDataReader<TResult>(dataReader, _logger);
						}
					}
				}
				if (result is null)
				{
					result = new List<TResult>();
				}
				return result;
			}

			//public async Task<TModel> QueryAsync<TModel, TReaderResult, TOutParameters>(string sprocName, DbParameterCollection parameters, bool useOutputParameters, CancellationToken cancellationToken)
			//	where TModel : class, new()
			//{
			//	if (useOutputParameters)
			//	{
			//		return await QueryAsync<object, TModel>(sprocName, parameters, Mapper.QueryResultsHandler<byte, TModel>, false, null, cancellationToken).ConfigureAwait(false);

			//	}
			//	else
			//	{
			//		return await QueryAsync<object, TModel>(sprocName, parameters, Mapper.QueryResultsHandler<byte, TModel, TModel, Mapper.DummyType>, false, null, cancellationToken).ConfigureAwait(false);
			//	}
			//}

			/// <summary>
			/// Query the database and return an instance of the specified type from output parameters.
			/// </summary>
			/// <typeparam name="TModel"></typeparam>
			/// <typeparam name="TReaderResult"></typeparam>
			/// <typeparam name="TOutParameters"></typeparam>
			/// <param name="sprocName"></param>
			/// <param name="parameters"></param>
			/// <param name="cancellationToken"></param>
			/// <returns></returns>
			public async Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
				where TModel : class, new()
				=> await QueryAsync<object, TModel>(sprocName, parameters, Mapper.QueryResultsHandler<byte, TModel, Mapper.DummyType, TModel>, false, null, cancellationToken).ConfigureAwait(false);

			public async Task<TModel> QueryAsync<TModel, TReaderResult, TOutParameters>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
				where TModel : class, new()
				where TReaderResult : class, new()
				where TOutParameters : class, new()
				=> await QueryAsync<object, TModel>(sprocName, parameters, Mapper.QueryResultsHandler<byte, TModel, TReaderResult, TOutParameters>, false, null, cancellationToken).ConfigureAwait(false);

			public async Task<TModel> QueryAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutParameters>
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
				=> await QueryAsync<object, TModel>(sprocName, parameters, Mapper.QueryResultsHandler<byte, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutParameters>, false, null, cancellationToken).ConfigureAwait(false);

			public async Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<byte, object, TModel> resultHandler, bool isTopOne, CancellationToken cancellationToken) where TModel : class, new()
				=> await QueryAsync<object, TModel>(sprocName, parameters, resultHandler, isTopOne, null, cancellationToken).ConfigureAwait(false);

			public async Task<TModel> QueryAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<byte, TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) where TModel : class, new()
            {
                cancellationToken.ThrowIfCancellationRequested();
                var startTimestamp = Stopwatch.GetTimestamp();
                SqlExceptionsEncountered.Clear();
                
                if (!this._commandPolicy.ContainsKey(sprocName))
                {
                    this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
                }
                var result = await this._commandPolicy[sprocName].ExecuteAsync(newToken => 
					this.ExecuteQueryWithDelegateAsync<TModel, TArg>(sprocName, parameters, resultHandler, isTopOne, optionalArgument, newToken), cancellationToken).ConfigureAwait(false);

                var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
                _logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
                return result;
            }

			private async Task<TResult> ExecuteQueryWithDelegateAsync<TResult, TArg>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<byte, TArg, TResult> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) where TResult: class, new()
			{
				var result = default(TResult);
				cancellationToken.ThrowIfCancellationRequested();
				SqlExceptionsEncountered.Clear();
				using (var connection = this._dataProviderServices.NewConnection(this._connectionString))
				{
					//await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken)).ConfigureAwait(false);
					await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

					cancellationToken.ThrowIfCancellationRequested();
					using (var cmd = this._dataProviderServices.NewCommand(sprocName, connection))
					{
						cmd.CommandType = System.Data.CommandType.StoredProcedure;
						this._dataProviderServices.SetParameters(cmd, parameters);
						var cmdType = System.Data.CommandBehavior.Default;
						if (isTopOne)
						{
							cmdType = System.Data.CommandBehavior.SingleRow;
						}
						using (var dataReader = await cmd.ExecuteReaderAsync(cmdType, cancellationToken).ConfigureAwait(false))
						{
							cancellationToken.ThrowIfCancellationRequested();

							result = resultHandler(0, sprocName, optionalArgument, dataReader, cmd.Parameters, _connectionName, this._logger);
						}
					}
				}
				return result;
			}


			public async Task RunAsync(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
			{

				cancellationToken.ThrowIfCancellationRequested();
				var startTimestamp = Stopwatch.GetTimestamp();
				SqlExceptionsEncountered.Clear();

				if (!this._commandPolicy.ContainsKey(sprocName))
				{
					this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
				}
				await this._commandPolicy[sprocName].ExecuteAsync(newToken => this.ExecuteRunAsync(sprocName, parameters, newToken), cancellationToken).ConfigureAwait(false);

				var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
				_logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
				//return result;
			}

			private async Task ExecuteRunAsync(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				SqlExceptionsEncountered.Clear();
				using (var connection = this._dataProviderServices.NewConnection(this._connectionString))
				{
					//await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken)).ConfigureAwait(false);
					await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

					cancellationToken.ThrowIfCancellationRequested();
					using (var cmd = this._dataProviderServices.NewCommand(sprocName, connection))
					{
						cmd.CommandType = System.Data.CommandType.StoredProcedure;
						this._dataProviderServices.SetParameters(cmd, parameters);
						await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
					}
				}
			}


			#endregion
		}

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
		#endregion
    }
}
