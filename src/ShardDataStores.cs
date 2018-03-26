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
    public class ShardDataStores<TShard> where TShard : IComparable
    {
        private readonly ImmutableDictionary<string, SecurityConfiguration> _credentials;
        private readonly ImmutableDictionary<string, DataResilienceConfiguration> _resilienceStrategies;
        private readonly IDataProviderServices _dataProviderServices;
        private readonly ILogger _logger;

        public ShardDataStores(IShardDataConfigurationOptions<TShard> config, 
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
            this.ShardSets = new ShardDataSets(this, config.ShardSetsInternal);

        }

        public ShardDataSets ShardSets { get; }

        #region Nested classes
        public class ShardQueryResult
        {
            public ShardQueryResult(TShard shardNumber, string parameterName, object result)
            {
                this.ShardNumber = shardNumber;
                this.ParameterName = parameterName;
                this.Result = result;
            }
            public TShard ShardNumber { get; private set; }
            public string ParameterName { get; private set; }
            public object Result { get; private set; }
        }
        public class ShardDataSets : ICollection
        {
            private readonly object syncRoot = new Lazy<object>();
            private readonly ImmutableDictionary<string, ShardDataSet> dtn;

            private ShardDataSets()
            {
                //hide ctor
            }

            public ShardDataSets(ShardDataStores<TShard> parent, IShardConnectionsConfiguration<TShard>[] config)
            {
                var bdr = ImmutableDictionary.CreateBuilder<string, ShardDataSet>();
                foreach (var set in config)
                {
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

            private ShardDataSet()
            {
                //hid ctor
            }
            public ShardDataSet(ShardDataStores<TShard> parent, IShardConnectionsConfiguration<TShard> config)
            {
                var bdr = ImmutableDictionary.CreateBuilder<TShard, ShardInstance>();
                foreach (var shd in config.ShardsInternal)
                {
                    bdr.Add(shd.ShardNumber, new ShardInstance(parent, shd.ShardNumber, config.DataResilienceKey, shd.ReadConnectionInternal, shd.WriteConnectionInternal));
                }
                this.dtn = bdr.ToImmutable();
            }

            public TShard ShardNumber { get; set; }


            public ShardInstance this[TShard shardNumber]
            {
                get { return dtn[shardNumber]; }
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

            //public async Task<List<ShardQueryResult>> QueryFirstAsync(string sprocName, TShard[] exclude, IImmutableList<DbParameter> parameters, int shardNumberParameterIndex, CancellationToken cancellationToken)
            //{
            //    return null;
            //}


            public async Task<TResult> QueryFirstAsync<TResult, TPrm>(string sprocName, TShard[] exclude, DbParameterCollection parameters, int shardNumberParameterIndex, SqlShardObjectConverter<TShard, TResult, TPrm> sqlResultConverter, TPrm dataObject, CancellationToken cancellationToken)
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
                    foreach (var shardNumber in dtn.Keys)
                    {
                        if (shardNumberParameterIndex >= 0 && shardNumberParameterIndex < parameters.Count) // && cmd.Parameters[shardNumberParameterIndex].DbType == System.Data.DbType.Byte)
                        {
                            parameters[shardNumberParameterIndex].Value = this.ShardNumber;
                        }

                        tsks.Add(this.dtn[shardNumber].ReadConnection.QueryAsync<TResult, TPrm>(sprocName, parameters, sqlResultConverter, shardNumberParameterIndex, true, dataObject, queryCancelationToken.Token));
                    }
                    while (tsks.Count > 0)
                    {
                        var tsk = await Task.WhenAny(tsks);
                        tsks.Remove(tsk);
                        if (cancellationToken.IsCancellationRequested)
                        {
                            cancelTokenSource.Cancel();
                            break;
                        }
                        try
                        {
                            var tskResult = await tsk;
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
            public async Task<List<TResult>> QueryAllAsync<TResult, TPrm>(string sprocName, TShard[] exclude, DbParameterCollection parameters, int shardNumberParameterIndex, SqlShardObjectConverter<TShard, TResult, TPrm> sqlResultConverter, TPrm dataObject, CancellationToken cancellationToken)
            {
                var result = new List<TResult>();
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
                foreach (var shardNumber in dtn.Keys)
                {
                    if (shardNumberParameterIndex >= 0 && shardNumberParameterIndex < parameters.Count) // && cmd.Parameters[shardNumberParameterIndex].DbType == System.Data.DbType.Byte)
                    {
                        parameters[shardNumberParameterIndex].Value = this.ShardNumber;
                    }
                    tsks.Add(this.dtn[shardNumber].ReadConnection.QueryAsync<TResult, TPrm>(sprocName, parameters, sqlResultConverter, shardNumberParameterIndex, false, dataObject, cancellationToken));
                }
                await Task.WhenAll(tsks);
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
            public async Task<List<TResult>> QueryAllAsync<TResult>(string sprocName, TShard[] exclude, DbParameterCollection parameters, int shardNumberParameterIndex, SqlShardObjectConverter<TShard, TResult, object> sqlResultConverter, CancellationToken cancellationToken)
                => await QueryAllAsync<TResult, object>(sprocName, exclude, parameters, shardNumberParameterIndex, sqlResultConverter, null, cancellationToken);
        }

        public class DataConnection
        {
            private readonly ILogger _logger;
            private readonly Policy _connectPolicy;
            private readonly Dictionary<string, Policy> _commandPolicy = new Dictionary<string, Policy>();
            private readonly DataResilienceConfiguration _resilienceStrategy;
            private readonly IDataProviderServices _dataProviderServices;
            private readonly string _connectionString;
            private readonly string _connectionName; //shardset key + shardNumber.ToString() || dataset key
            private readonly TShard _shardNumber;

            private Policy ConnectionPolicy { get; set; }
            private Dictionary<string, Policy> CommandPolicies { get; set; } = new Dictionary<string, Policy>();

            private DataConnection() {  } //hid ctor

            internal DataConnection(ShardDataStores<TShard> parent, string resilienceStrategyKey, IConnectionConfiguration config)
                : this(parent, default(TShard), resilienceStrategyKey, config.ConnectionDescription, config)
            {

            }

            internal DataConnection(ShardDataStores<TShard> parent, TShard shardNumber, string resilienceStrategyKey, IConnectionConfiguration config)
                : this(parent, default(TShard), resilienceStrategyKey, "shard number " + shardNumber.ToString() + " on connection " + config.ConnectionDescription, config)
            {

            }

            private DataConnection(ShardDataStores<TShard> parent, TShard shardNumber,  string resilienceStrategyKey, string connectionName, IConnectionConfiguration config)
            {
                //this._config = config;
                this._logger = parent._logger;
                this._dataProviderServices = parent._dataProviderServices;
                this._connectionString = config.GetConnectionString();
                this._connectionName = config.ConnectionDescription;
                this._resilienceStrategy = new DataResilienceConfiguration(); //initialize to defaults
                this._shardNumber = shardNumber;
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
 
            //private async static Task<DbDataReader> ExecuteReaderWithRetryAsync(DbCommand command)
            //{
            //    //GuardConnectionIsNotNull(command);

            //    var policy = Polly.Policy.Handle<Exception>().WaitAndRetryAsync(
            //        retryCount: 3, // Retry 3 times
            //        sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)), // Exponential backoff based on an initial 200ms delay.
            //        onRetry: (exception, attempt) =>
            //        {
            //        // Capture some info for logging/telemetry.  
            //        logger.LogWarn($"ExecuteReaderWithRetryAsync: Retry {attempt} due to {exception}.");
            //        });

            //    // Retry the following call according to the policy.
            //    await policy.ExecuteAsync<SqlDataReader>(async token =>
            //    {
            //        // This code is executed within the Policy 

            //        if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync(token);
            //        return await command.ExecuteReaderAsync(System.Data.CommandBehavior.Default, token);

            //    }, cancellationToken);
            //}


            private static readonly double TimestampToMilliseconds = (double)TimeSpan.TicksPerSecond / (Stopwatch.Frequency * TimeSpan.TicksPerMillisecond);


            //public async Task<Dictionary<string, object>> QueryAsync(string sprocName, IImmutableList<DbParameter> parameters, CancellationToken cancellationToken)
            //{
            //    return null;
            //}
            public async Task<TResult> QueryAsync<TResult, TPrm>(string sprocName, DbParameterCollection parameters, SqlShardObjectConverter<TShard, TResult, TPrm> sqlResultConverter, int shardNumberParameterIndex, bool isTopOne, TPrm dataObject, CancellationToken cancellationToken)
            {

                cancellationToken.ThrowIfCancellationRequested();
                var startTimestamp = Stopwatch.GetTimestamp();
                SqlExceptionsEncountered.Clear();
                
                if (!this._commandPolicy.ContainsKey(sprocName))
                {
                    this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
                }
                var result = await this._commandPolicy[sprocName].ExecuteAsync(newToken => this.QueryDatabaseAsync<TResult, TPrm>(sprocName, parameters, sqlResultConverter, shardNumberParameterIndex, isTopOne, dataObject, newToken), cancellationToken);

                var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
                _logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
                return result;
            }

            private async Task<TResult> QueryDatabaseAsync<TResult, TPrm>(string sprocName, DbParameterCollection parameters, SqlShardObjectConverter<TShard, TResult, TPrm> sqlResultConverter, int shardNumberParameterIndex, bool isTopOne, TPrm dataObject, CancellationToken cancellationToken)
            {
                TResult result = default(TResult);
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
                        if (shardNumberParameterIndex >= 0 && shardNumberParameterIndex < cmd.Parameters.Count)
                        {
                            cmd.Parameters[shardNumberParameterIndex].Value = this._shardNumber;
                        }
                        var cmdType = System.Data.CommandBehavior.Default;
                        if (isTopOne)
                        {
                            cmdType = System.Data.CommandBehavior.SingleRow;
                        }
                        using (var dataReader = await cmd.ExecuteReaderAsync(cmdType, cancellationToken))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

							result = sqlResultConverter(this._shardNumber, dataObject, dataReader, parameters, this._logger);
                        }
                    }
                }

                return result;

            }




        }
        //public class ShardConnection: DataConnection
        //{
        //    public ShardConnection(DataStores<TShard> parent, string resilienceStrategyKey, IDbConnectionConfiguration config) : base(parent, resilienceStrategyKey, config) { }
        //}
        //public class DatabaseConnection: DataConnection
        //{
        //    public DatabaseConnection(DataStores<TShard> parent, string resilienceStrategyKey, IDbConnectionConfiguration config) : base(parent, resilienceStrategyKey, config) { }
        //}

        public class ShardInstance
        {
            public enum DataAccess
            {
                ReadOnly,
                WriteAccess
            }
            public ShardInstance(ShardDataStores<TShard> parent, TShard shardNumber, string resilienceStrategyKey, IConnectionConfiguration readConnection, IConnectionConfiguration writeConnection)
            {
                this.ShardNumber = shardNumber;
                this.ReadConnection = new DataConnection(parent, shardNumber, resilienceStrategyKey, readConnection);
                this.WriteConnection = new DataConnection(parent, shardNumber, resilienceStrategyKey, writeConnection);
            }
            public TShard ShardNumber { get; }
            public DataConnection ReadConnection { get; }
            public DataConnection WriteConnection { get; }

        }
    #endregion
    }
}
