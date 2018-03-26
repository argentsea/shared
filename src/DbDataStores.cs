using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Data.Common;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Polly;

namespace ArgentSea
{
    public class DbDataStores
    {
        private readonly ImmutableDictionary<string, SecurityConfiguration> _credentials;
        private readonly ImmutableDictionary<string, DataResilienceConfiguration> _resilienceStrategies;
        private readonly IDataProviderServices _dataProviderServices;
        private readonly ILogger _logger;

        public DbDataStores(
			IDbDataConfigurationOptions config, 
            DataSecurityOptions security,
			DataResilienceConfigurationOptions resilienceStrategies,
			IDataProviderServices dataProviderServices, 
            ILogger logger)
        {
            this._logger = logger;
            var sbdr = ImmutableDictionary.CreateBuilder<string, SecurityConfiguration>();
            foreach (var crd in security.Credentials)
            {
                sbdr.Add(crd.SecurityKey, crd);
            }
            this._credentials = sbdr.ToImmutable();

			var rbdr = ImmutableDictionary.CreateBuilder<string, DataResilienceConfiguration>();
			foreach (var rs in resilienceStrategies.DataResilienceStrategies)
			{
				rbdr.Add(rs.DataResilienceKey, rs);
			}
			this._resilienceStrategies = rbdr.ToImmutable();
			this._dataProviderServices = dataProviderServices;
			this.DbConnections = new DbDataSets(this, config.DbConnectionsInternal);
        }

        public DbDataSets DbConnections { get; }

        #region Nested classes

        public class DataConnection
        {
            private readonly ILogger _logger;
            private readonly Policy _connectPolicy;
            private readonly Dictionary<string, Policy> _commandPolicy = new Dictionary<string, Policy>();
            private readonly DataResilienceConfiguration _resilienceStrategy;
            private readonly IDataProviderServices _dataProviderServices;
            private readonly string _connectionString;
            private readonly string _connectionName;

            private Policy ConnectionPolicy { get; set; }
            private Dictionary<string, Policy> CommandPolicies { get; set; } = new Dictionary<string, Policy>();

            private DataConnection() {  } //hide ctor

            internal DataConnection(DbDataStores parent, string resilienceStrategyKey, IConnectionConfiguration config)
                : this(parent, resilienceStrategyKey, config.ConnectionDescription, config)
            {

            }

            private DataConnection(DbDataStores parent, string resilienceStrategyKey, string connectionName, IConnectionConfiguration config)
            {
                //this._config = config;
                this._logger = parent._logger;
                this._dataProviderServices = parent._dataProviderServices;
                this._connectionString = config.GetConnectionString();
                this._connectionName = config.ConnectionDescription;
                this._resilienceStrategy = new DataResilienceConfiguration(); //initialize to defaults
                if (!string.IsNullOrEmpty(resilienceStrategyKey))
                {
                    this._resilienceStrategy = parent._resilienceStrategies[resilienceStrategyKey];
                }
                var retryPolicy = Policy.Handle<DbException>(ex =>
                        parent._dataProviderServices.GetIsErrorTransient(ex)
                    )
                    .WaitAndRetryAsync(
                        retryCount: this._resilienceStrategy.RetryCount,
                        sleepDurationProvider: attempt => this._resilienceStrategy.HandleRetryTimespan(attempt),
                        onRetry: (exception, timeSpan, retryCount, context) => this.HandleConnectionRetry(this._connectionName, retryCount, exception)
                    );

                if (this._resilienceStrategy.CircuitBreakerFailureCount < 1)
                {
                    this._connectPolicy = retryPolicy;
                }
                else
                {
                    var circuitBreakerPolicy = Policy.Handle<Exception>().CircuitBreakerAsync(
                        exceptionsAllowedBeforeBreaking: this._resilienceStrategy.CircuitBreakerFailureCount,
                        durationOfBreak: TimeSpan.FromMilliseconds(this._resilienceStrategy.CircuitBreakerTestInterval)
                        );
                    this._connectPolicy = circuitBreakerPolicy.Wrap(retryPolicy);
                }
            }

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
            public List<Exception> SqlExceptionsEncountered { get; } = new List<Exception>();
 
            private static readonly double TimestampToMilliseconds = (double)TimeSpan.TicksPerSecond / (Stopwatch.Frequency * TimeSpan.TicksPerMillisecond);


            public async Task<TResult> QueryAsync<TResult, TPrm>(string sprocName, DbParameterCollection parameters, SqlDbObjectConverter<TResult, TPrm> sqlResultConverter, bool isTopOne, TPrm dataObject, CancellationToken cancellationToken)
            {

                cancellationToken.ThrowIfCancellationRequested();
                var startTimestamp = Stopwatch.GetTimestamp();
                SqlExceptionsEncountered.Clear();
                
                if (!this._commandPolicy.ContainsKey(sprocName))
                {
                    this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
                }
                var result = await this._commandPolicy[sprocName].ExecuteAsync(newToken => this.QueryDatabaseAsync<TResult, TPrm>(sprocName, parameters, sqlResultConverter, isTopOne, dataObject, newToken), cancellationToken);

                var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
                _logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
                return result;
            }
			public async Task QueryAsync(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
			{

				cancellationToken.ThrowIfCancellationRequested();
				var startTimestamp = Stopwatch.GetTimestamp();
				SqlExceptionsEncountered.Clear();

				if (!this._commandPolicy.ContainsKey(sprocName))
				{
					this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
				}
				await this._commandPolicy[sprocName].ExecuteAsync(newToken => this.QueryDatabaseAsync(sprocName, parameters, newToken), cancellationToken);

				var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
				_logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
				//return result;
			}

			private async Task<TResult> QueryDatabaseAsync<TResult, TPrm>(string sprocName, DbParameterCollection parameters, SqlDbObjectConverter<TResult, TPrm> sqlResultConverter, bool isTopOne, TPrm dataObject, CancellationToken cancellationToken)
            {
                var result = default(TResult);
                cancellationToken.ThrowIfCancellationRequested();
                SqlExceptionsEncountered.Clear();
                using (var connection = this._dataProviderServices.NewConnection(this._connectionString))
                {
                    await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken));
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
                        using (var dataReader = await cmd.ExecuteReaderAsync(cmdType, cancellationToken))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

							result = sqlResultConverter(dataObject, dataReader, parameters, this._logger);
                        }
                    }
                }
                return result;
            }
			private async Task QueryDatabaseAsync(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				SqlExceptionsEncountered.Clear();
				using (var connection = this._dataProviderServices.NewConnection(this._connectionString))
				{
					await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken));
					cancellationToken.ThrowIfCancellationRequested();
					using (var cmd = this._dataProviderServices.NewCommand(sprocName, connection))
					{
						cmd.CommandType = System.Data.CommandType.StoredProcedure;
						this._dataProviderServices.SetParameters(cmd, parameters);
						await cmd.ExecuteNonQueryAsync();
					}
				}
			}
		}

		public class DbDataSets : ICollection
        {
            private readonly object syncRoot = new Lazy<object>();
            private readonly ImmutableDictionary<string, DataConnection> dtn;

            public DbDataSets()
            {
                //hide ctor
            }
            public DbDataSets(DbDataStores parent, IDbConnectionConfiguration[] config)
            {
                var bdr = ImmutableDictionary.CreateBuilder<string, DataConnection>();

                foreach (var db in config)
                {
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
