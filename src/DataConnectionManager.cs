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
    internal class DataConnectionManager<TShard> where TShard: IComparable
	{
		#region Private variables and constructors
		private readonly ILogger _logger;
		private readonly Dictionary<string, Policy> _commandPolicy = new Dictionary<string, Policy>();
		private readonly IDataProviderServiceFactory _dataProviderServices;
        private readonly IDataConnection _connectionConfig;
		private readonly string _connectionName;
        private readonly TShard _shardId;

        private const int DefaultRetryCount = 6;
        private const int DefaultCircuitBreakerTestInterval = 5000; // 5 seconds
        private const int DefaultCircuitBreakerFailureCount = 20;

        internal DataConnectionManager(TShard shardId, IDataProviderServiceFactory dataProviderServices, IDataConnection connectionConfig, string connectionName, ILogger logger)
		{
			this._logger = logger;
            this._dataProviderServices = dataProviderServices;
			this._connectionName = connectionName;
            _shardId = shardId;
            _connectionConfig = connectionConfig;
        }
		#endregion
		#region Private helper methods
		private Policy GetCommandResiliencePolicy(string sprocName)
		{
			Policy result;
            int retryCnt = DefaultRetryCount;
            int cbTestInterval = DefaultCircuitBreakerTestInterval;
            int cbFailureCount = DefaultCircuitBreakerFailureCount;

            if (_connectionConfig.RetryCount.HasValue)
            {
                retryCnt = _connectionConfig.RetryCount.Value;
            }
            if (_connectionConfig.CircuitBreakerTestInterval.HasValue)
            {
                cbTestInterval = _connectionConfig.CircuitBreakerTestInterval.Value;
            }
            if (_connectionConfig.CircuitBreakerFailureCount.HasValue)
            {
                cbFailureCount = _connectionConfig.CircuitBreakerFailureCount.Value;
            }
            var retryPolicy = Policy.Handle<DbException>(ex =>
					this._dataProviderServices.GetIsErrorTransient(ex)
				)
				.WaitAndRetryAsync(
					retryCount: retryCnt,
					sleepDurationProvider: attempt => this._connectionConfig.GetRetryTimespan(attempt),
					onRetry: (exception, timeSpan, retryCount, context) => this.HandleCommandRetry(sprocName, this._connectionName, retryCount, exception)
				);

			if (this._connectionConfig.CircuitBreakerFailureCount < 1)
			{
				result = retryPolicy;
			}
			else
			{
				var circuitBreakerPolicy = Policy.Handle<Exception>().CircuitBreakerAsync(
					exceptionsAllowedBeforeBreaking: cbFailureCount,
					durationOfBreak: TimeSpan.FromMilliseconds(cbTestInterval)
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

		public string ConnectionString { get => this._connectionConfig.GetConnectionString(); }

        #endregion

        #region Public data fetch methods
        /// <summary>
        /// Connect to the database and return a single value.
        /// </summary>
        /// <typeparam name="TValue">The expected type of the return value.</typeparam>
        /// <param name="sprocName">The stored procedure to call to fetch the value.</param>
        /// <param name="parameters">A parameters collction. Input parameters may be used to find the parameter; will return the value of the first output (or input/output) parameter. If TValue is an int, will also return the sproc return value.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <exception cref="ArgentSea.UnexpectedSqlResultException">Thrown when the expected return result or output parameter was not found.</exception>
        /// <returns>The retrieved value.</returns>
        internal async Task<TValue> LookupAsync<TValue>(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken)
		{
			//return await DataStoreHelper.AdoExecute<TValue>(sprocName, parameters, this._commandPolicy, this._dataProviderServices, this._resilienceStrategy, this._connectionName, this.HandleCommandRetry, this._logger, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			var startTimestamp = Stopwatch.GetTimestamp();

			if (!this._commandPolicy.ContainsKey(sprocName))
			{
				this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
			}
            parameters.SetShardId<TShard>(shardParameterOrdinal, this._shardId);
			var result = await this._commandPolicy[sprocName].ExecuteAsync(newToken =>
				ExecuteQueryToValueAsync<TValue>(sprocName, parameters, null, newToken), cancellationToken).ConfigureAwait(false);

			var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
			_logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
			return result;
		}

        /// <summary>
        /// Connect to the database and return the values as a list of objects.
        /// </summary>
        /// <typeparam name="TModel">The type of object to be listed.</typeparam>
        /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A list containing an object for each data row.</returns>
        internal async Task<IList<TModel>> ListAsync<TModel>(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, Dictionary<string, object> parameterValues, CancellationToken cancellationToken) where TModel : class, new()
		{
			cancellationToken.ThrowIfCancellationRequested();
			var startTimestamp = Stopwatch.GetTimestamp();

			if (!this._commandPolicy.ContainsKey(sprocName))
			{
				this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
			}
            parameters.SetShardId<TShard>(shardParameterOrdinal, this._shardId);
            var result = await this._commandPolicy[sprocName].ExecuteAsync(newToken =>
				ExecuteQueryToListAsync<TModel>(sprocName, parameters, parameterValues, newToken), cancellationToken).ConfigureAwait(false);

			var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
			_logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
			return result;
		}


        internal async Task<TModel> QueryAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, Dictionary<string, object> parameterValues, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) 
		{
			cancellationToken.ThrowIfCancellationRequested();
			var startTimestamp = Stopwatch.GetTimestamp();

			if (!this._commandPolicy.ContainsKey(sprocName))
			{
				this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
			}
            parameters.SetShardId<TShard>(shardParameterOrdinal, this._shardId);
            var result = await this._commandPolicy[sprocName].ExecuteAsync(newToken =>
				this.ExecuteQueryWithDelegateAsync<TModel, TArg>(sprocName, parameters, parameterValues, this._shardId, resultHandler, isTopOne, optionalArgument, newToken), cancellationToken).ConfigureAwait(false);

			var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
			_logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
			return result;
		}



        internal async Task RunAsync(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, Dictionary<string, object> parameterValues, CancellationToken cancellationToken)
		{

			cancellationToken.ThrowIfCancellationRequested();
			var startTimestamp = Stopwatch.GetTimestamp();

			if (!this._commandPolicy.ContainsKey(sprocName))
			{
				this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
			}
            parameters.SetShardId<TShard>(shardParameterOrdinal, this._shardId);
            await this._commandPolicy[sprocName].ExecuteAsync(newToken => this.ExecuteRunAsync(sprocName, parameters, parameterValues, newToken), cancellationToken).ConfigureAwait(false);

			var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
			_logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
			//return result;
		}

		#endregion
		#region Private Handlers

		private async Task<IList<TModel>> ExecuteQueryToListAsync<TModel>(string sprocName, DbParameterCollection parameters, Dictionary<string, object> parameterValues, CancellationToken cancellationToken) 
            where TModel : class, new()
		{
			IList<TModel> result = null;
			cancellationToken.ThrowIfCancellationRequested();
			using (var connection = this._dataProviderServices.NewConnection(this._connectionConfig.GetConnectionString()))
			{
				//await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken)).ConfigureAwait(false);
				await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
				cancellationToken.ThrowIfCancellationRequested();
				using (var cmd = this._dataProviderServices.NewCommand(sprocName, connection))
				{
					cmd.CommandType = System.Data.CommandType.StoredProcedure;
					this._dataProviderServices.SetParameters(cmd, parameters, parameterValues);
					var cmdType = System.Data.CommandBehavior.Default;
					using (var dataReader = await cmd.ExecuteReaderAsync(cmdType, cancellationToken).ConfigureAwait(false))
					{
						cancellationToken.ThrowIfCancellationRequested();
						result = Mapper.ToList<TModel>(dataReader, _logger);
					}
				}
			}
			if (result is null)
			{
				result = new List<TModel>();
			}
			return result;
		}
		private async Task<TValue> ExecuteQueryToValueAsync<TValue>(string sprocName, DbParameterCollection parameters, Dictionary<string, object> parameterValues, CancellationToken cancellationToken)
        {
            TValue result = default(TValue);
			cancellationToken.ThrowIfCancellationRequested();
			//SqlExceptionsEncountered.Clear();
			using (var connection = this._dataProviderServices.NewConnection(this._connectionConfig.GetConnectionString()))
			{
				//await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken)).ConfigureAwait(false);
				await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
				cancellationToken.ThrowIfCancellationRequested();
				using (var cmd = this._dataProviderServices.NewCommand(sprocName, connection))
				{
					cmd.CommandType = System.Data.CommandType.StoredProcedure;
					this._dataProviderServices.SetParameters(cmd, parameters, parameterValues);
					await cmd.ExecuteNonQueryAsync(cancellationToken);
					foreach (DbParameter prm in parameters)
					{
						if (result is int && prm.Direction == System.Data.ParameterDirection.ReturnValue && !System.DBNull.Value.Equals(prm.Value))
						{
							result = (dynamic)prm.Value;
							return result;
						}
						else if (prm.Direction == System.Data.ParameterDirection.Output || prm.Direction == System.Data.ParameterDirection.InputOutput)
						{
							if ((result is Nullable || !(result is ValueType)) && System.DBNull.Value.Equals(prm.Value))
							{
								result = (dynamic)null;
								return result;
							}
							else
							{
								result = (dynamic)prm.Value;
								return result;
							}
						}
					}
				}
			}
			throw new UnexpectedSqlResultException($"Database query {sprocName} expected to output a type of {typeof(TValue).ToString()}, but no output values were found.");
		}

		private async Task<TModel> ExecuteQueryWithDelegateAsync<TModel, TArg>(string sprocName, DbParameterCollection parameters, Dictionary<string, object> parameterValues, TShard shardId, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken)
		{
			var result = default(TModel);
			cancellationToken.ThrowIfCancellationRequested();
			using (var connection = this._dataProviderServices.NewConnection(this._connectionConfig.GetConnectionString()))
			{
				//await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken)).ConfigureAwait(false);
				await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

				cancellationToken.ThrowIfCancellationRequested();
				using (var cmd = this._dataProviderServices.NewCommand(sprocName, connection))
				{
					cmd.CommandType = System.Data.CommandType.StoredProcedure;
					this._dataProviderServices.SetParameters(cmd, parameters, parameterValues);
					var cmdType = System.Data.CommandBehavior.Default;
					if (isTopOne)
					{
						cmdType = System.Data.CommandBehavior.SingleRow;
					}
					using (var dataReader = await cmd.ExecuteReaderAsync(cmdType, cancellationToken).ConfigureAwait(false))
					{
						cancellationToken.ThrowIfCancellationRequested();

						result = resultHandler(shardId, sprocName, optionalArgument, dataReader, cmd.Parameters, _connectionName, this._logger);
					}
				}
			}
			return result;
		}

		private async Task ExecuteRunAsync(string sprocName, DbParameterCollection parameters, Dictionary<string, object> parameterValues, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			using (var connection = this._dataProviderServices.NewConnection(this._connectionConfig.GetConnectionString()))
			{
				//await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken)).ConfigureAwait(false);
				await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

				cancellationToken.ThrowIfCancellationRequested();
				using (var cmd = this._dataProviderServices.NewCommand(sprocName, connection))
				{
					cmd.CommandType = System.Data.CommandType.StoredProcedure;
					this._dataProviderServices.SetParameters(cmd, parameters, parameterValues);
					await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
				}
			}
		}
		#endregion
	}
}
