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
    internal class DataConnectionManager
	{
		#region Private variables and constructors
		private readonly ILogger _logger;
        private readonly AsyncPolicy _resiliencePolicy;
		private readonly IDataProviderServiceFactory _dataProviderServices;
        private readonly IDataConnection _connectionConfig;
		private readonly string _connectionName;
        private readonly short _shardId;

        private const int DefaultRetryCount = 6;
        private const int DefaultCircuitBreakerTestInterval = 5000; // 5 seconds
        private const int DefaultCircuitBreakerFailureCount = 20;

        internal DataConnectionManager(short shardId, IDataProviderServiceFactory dataProviderServices, IDataConnection connectionConfig, string connectionName, ILogger logger)
		{
			this._logger = logger;
            this._dataProviderServices = dataProviderServices;
			this._connectionName = connectionName;
            _shardId = shardId;
            _connectionConfig = connectionConfig;
            _resiliencePolicy = GetConnectionResiliencePolicy();
        }
		#endregion
		#region Private helper methods
		private AsyncPolicy GetConnectionResiliencePolicy()
		{
            AsyncPolicy result;
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
					onRetry: (exception, timeSpan, retryCount, context) => this.HandleConnectionRetry(this._connectionName, retryCount, exception)
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
				result = circuitBreakerPolicy.WrapAsync(retryPolicy);
			}
			return result;
		}

		private void HandleConnectionRetry(string connectionName, int attempt, Exception exception)
		{
			this._logger?.RetryingDbConnection(connectionName, attempt, exception);

		}

		private static readonly double TimestampToMilliseconds = (double)TimeSpan.TicksPerSecond / (Stopwatch.Frequency * TimeSpan.TicksPerMillisecond);

		#endregion

		#region Public properties

		public string ConnectionString { get => this._connectionConfig.GetConnectionString(_logger); }

        #endregion

        #region Return data methods

        // returns up to three values. name must match paramter or it will revert to column name. ignored if null or empty.
        internal async Task<(TValue1, TValue2, TValue3)> ReturnAsync<TValue1, TValue2, TValue3>(Query query, string data1Name, string data2Name, string data3Name, DbParameterCollection parameters, IDictionary<string, object> parameterValues, int shardParameterOrdinal, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var startTimestamp = Stopwatch.GetTimestamp();

            parameters.SetShardId(shardParameterOrdinal, this._shardId);
			var result = await _resiliencePolicy.ExecuteAsync(newToken =>
				ExecuteQueryToValueAsync<TValue1, TValue2, TValue3>(query, data1Name, data2Name, data3Name, parameters, parameterValues, newToken), cancellationToken).ConfigureAwait(false);

            var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
			_logger?.TraceDbCmdExecuted(query.Name, this._connectionName, elapsedMS);
			return result;
		}

        // returns the integer return value of a procedure. Will add parameter if not provided.
        internal async Task<int> ReturnAsync(Query query, DbParameterCollection parameters, int shardParameterOrdinal, Dictionary<string, object> mockResults, CancellationToken cancellationToken)
        {
            if (!(mockResults is null) && mockResults.Count > 0 && mockResults.ContainsKey(query.Name))
            {
                return (int)mockResults[query.Name];
            }
            cancellationToken.ThrowIfCancellationRequested();
            var startTimestamp = Stopwatch.GetTimestamp();

            parameters.SetShardId(shardParameterOrdinal, this._shardId);
            var result = await _resiliencePolicy.ExecuteAsync(newToken =>
                ExecuteQueryToValueAsync(query, parameters, null, newToken), cancellationToken).ConfigureAwait(false);

            var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
            _logger?.TraceDbCmdExecuted(query.Name, this._connectionName, elapsedMS);
            return result;
        }

        #endregion

        #region List methods


        internal async Task<List<TModel>> ListAsync<TModel>(Query query, DbParameterCollection parameters, int shardParameterOrdinal, IDictionary<string, object> parameterValues, Dictionary<string, object> mockResults, CancellationToken cancellationToken) where TModel : new()
		{
            if (!(mockResults is null) && mockResults.Count > 0 && mockResults.ContainsKey(query.Name))
            {
                return (List<TModel>)mockResults[query.Name];
            }
            cancellationToken.ThrowIfCancellationRequested();
			var startTimestamp = Stopwatch.GetTimestamp();

            parameters.SetShardId(shardParameterOrdinal, this._shardId);
            var result = await this._resiliencePolicy.ExecuteAsync(newToken =>
				ExecuteQueryToModelListAsync<TModel>(_shardId, query, parameters, parameterValues, newToken), cancellationToken).ConfigureAwait(false);

			var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
			_logger?.TraceDbCmdExecuted(query.Name, this._connectionName, elapsedMS);
			return result;
		}

        internal async Task<List<(TValue1, TValue2, TValue3)>> ListAsync<TValue1, TValue2, TValue3>(Query query, string data1Name, string data2Name, string data3Name, DbParameterCollection parameters, int shardParameterOrdinal, IDictionary<string, object> parameterValues, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var startTimestamp = Stopwatch.GetTimestamp();

            parameters.SetShardId(shardParameterOrdinal, this._shardId);
            var result = await this._resiliencePolicy.ExecuteAsync(newToken =>
                ExecuteQueryToValueListAsync<TValue1, TValue2, TValue3>(query, data1Name, data2Name, data3Name, parameters, parameterValues, newToken), cancellationToken).ConfigureAwait(false);

            var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
            _logger?.TraceDbCmdExecuted(query.Name, this._connectionName, elapsedMS);
            return result;
        }

        #endregion

        #region Query methods

        internal async Task<TModel> QueryAsync<TArg, TModel>(TModel instance, Query query, DbParameterCollection parameters, int shardParameterOrdinal, IDictionary<string, object> parameterValues, QueryResultModelHandler<TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, Dictionary<string, object> mockResults, CancellationToken cancellationToken) 
		{
            if (!(mockResults is null) && mockResults.Count > 0 && mockResults.ContainsKey(query.Name))
            {
                return (TModel)mockResults[query.Name];
            }
            cancellationToken.ThrowIfCancellationRequested();
			var startTimestamp = Stopwatch.GetTimestamp();

            parameters.SetShardId(shardParameterOrdinal, this._shardId);
            var result = await _resiliencePolicy.ExecuteAsync(newToken =>
				this.ExecuteQueryWithDelegateAsync<TModel, TArg>(instance, query, parameters, parameterValues, this._shardId, resultHandler, isTopOne, optionalArgument, newToken), cancellationToken).ConfigureAwait(false);

			var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
			_logger?.TraceDbCmdExecuted(query.Name, this._connectionName, elapsedMS);
			return result;
		}


        #endregion

        #region Run methods

        internal async Task RunAsync(Query query, DbParameterCollection parameters, int shardParameterOrdinal, IDictionary<string, object> parameterValues, Dictionary<string, object> mockResults, CancellationToken cancellationToken)
		{
            if (!(mockResults is null) && mockResults.Count > 0 && mockResults.ContainsKey(query.Name))
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
			var startTimestamp = Stopwatch.GetTimestamp();
            parameters.SetShardId(shardParameterOrdinal, this._shardId);

            await _resiliencePolicy.ExecuteAsync(newToken => this.ExecuteRunAsync(query, parameters, parameterValues, newToken), cancellationToken).ConfigureAwait(false);

            var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
			_logger?.TraceDbCmdExecuted(query.Name, this._connectionName, elapsedMS);
		}

        internal async Task<TResult> RunBatchAsync<TResult>(BatchBase<TResult> batch, Dictionary<string, object> mockResults, CancellationToken cancellationToken)
        {
            if (!(mockResults is null) && mockResults.Count > 0 && mockResults.ContainsKey(string.Empty))
            {
                return (TResult)mockResults[string.Empty];
            }

            cancellationToken.ThrowIfCancellationRequested();
            var startTimestamp = Stopwatch.GetTimestamp();


            var result = await _resiliencePolicy.ExecuteAsync(newToken =>
                this.ExecuteBatchAsync<TResult>(batch, newToken), cancellationToken).ConfigureAwait(false);

            var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
            _logger?.TraceDbCmdExecuted("QueryBatch", this._connectionName, elapsedMS);
            return result;
        }

        #endregion
        #region Private Handlers

        private async Task<List<TModel>> ExecuteQueryToModelListAsync<TModel>(short shardId, Query query, DbParameterCollection parameters, IDictionary<string, object> parameterValues, CancellationToken cancellationToken) 
            where TModel : new()
		{
			List<TModel> result = null;
			cancellationToken.ThrowIfCancellationRequested();
			using (var connection = this._dataProviderServices.NewConnection(this._connectionConfig.GetConnectionString(_logger)))
			{
				//await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken)).ConfigureAwait(false);
				await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
				cancellationToken.ThrowIfCancellationRequested();
				using (var cmd = this._dataProviderServices.NewCommand(query.Sql, connection))
				{
					cmd.CommandType = query.Type;
					this._dataProviderServices.SetParameters(cmd, query.ParameterNames, parameters, parameterValues);
					using (var dataReader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, cancellationToken).ConfigureAwait(false))
					{
						cancellationToken.ThrowIfCancellationRequested();
						result = Mapper.ToList<TModel>(dataReader, shardId, _logger);
					}
				}
			}
			if (result is null)
			{
				result = new List<TModel>();
			}
			return result;
		}
        private async Task<List<(TValue1, TValue2, TValue3)>> ExecuteQueryToValueListAsync<TValue1, TValue2, TValue3>(Query query, string data1Name, string data2Name, string data3Name, DbParameterCollection parameters, IDictionary<string, object> parameterValues, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = new List<(TValue1, TValue2, TValue3)>();
            using (var connection = this._dataProviderServices.NewConnection(this._connectionConfig.GetConnectionString(_logger)))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                using (var cmd = this._dataProviderServices.NewCommand(query.Sql, connection))
                {
                    cmd.CommandType = query.Type;
                    this._dataProviderServices.SetParameters(cmd, query.ParameterNames, parameters, parameterValues);
                    using (var rdr = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, cancellationToken).ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        while (rdr.Read())
                        {
                            TValue1 value1 = default(TValue1);
                            TValue2 value2 = default(TValue2);
                            TValue3 value3 = default(TValue3);
                            if (!string.IsNullOrEmpty(data1Name))
                            {
                                value1 = rdr.GetFieldValue<TValue1>(rdr.GetOrdinal(data1Name));
                            }
                            if (!string.IsNullOrEmpty(data2Name))
                            {
                                value2 = rdr.GetFieldValue<TValue2>(rdr.GetOrdinal(data2Name));
                            }
                            if (!string.IsNullOrEmpty(data3Name))
                            {
                                value3 = rdr.GetFieldValue<TValue3>(rdr.GetOrdinal(data3Name));
                            }
                            result.Add((value1, value2, value3));
                        }
                    }
                }
            }
            return result;
        }

        // Adds return value parameter if it doesn't exist
        private async Task<int> ExecuteQueryToValueAsync(Query query, DbParameterCollection parameters, IDictionary<string, object> parameterValues, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (var connection = this._dataProviderServices.NewConnection(this._connectionConfig.GetConnectionString(_logger)))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                using (var cmd = this._dataProviderServices.NewCommand(query.Sql, connection))
                {
                    cmd.CommandType = query.Type;
                    this._dataProviderServices.SetParameters(cmd, query.ParameterNames, parameters, parameterValues);
                    DbParameter prmRtn = null;
                    foreach (DbParameter prm in cmd.Parameters)
                    {
                        if (prm.Direction == System.Data.ParameterDirection.ReturnValue)
                        {
                            prmRtn = prm;
                            break;
                        }
                    }
                    if (prmRtn is null)
                    {
                        prmRtn = cmd.CreateParameter();
                        prmRtn.Direction = System.Data.ParameterDirection.ReturnValue;
                        cmd.Parameters.Add(prmRtn);
                    }
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                    return (int)prmRtn.Value;
                }
            }
        }

        // if dataName matches output parameter name, then returns that, otherwise looks for column in first row.
        private async Task<(TValue1, TValue2, TValue3)> ExecuteQueryToValueAsync<TValue1, TValue2, TValue3>(Query query, string data1Name, string data2Name, string data3Name, DbParameterCollection parameters, IDictionary<string, object> parameterValues, CancellationToken cancellationToken)
        {
			cancellationToken.ThrowIfCancellationRequested();
			using (var connection = this._dataProviderServices.NewConnection(this._connectionConfig.GetConnectionString(_logger)))
			{
				await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
				cancellationToken.ThrowIfCancellationRequested();
				using (var cmd = this._dataProviderServices.NewCommand(query.Sql, connection))
				{
					cmd.CommandType = query.Type;
					this._dataProviderServices.SetParameters(cmd, query.ParameterNames, parameters, parameterValues);
                    DbParameter prmOutput1 = null;
                    DbParameter prmOutput2 = null;
                    DbParameter prmOutput3 = null;
                    foreach (DbParameter prm in cmd.Parameters)
                    {
                        if (!string.IsNullOrEmpty(data1Name) 
                            && (prm.Direction == System.Data.ParameterDirection.Output || prm.Direction == System.Data.ParameterDirection.InputOutput) 
                            && prm.ParameterName == data1Name)
                        {
                            prmOutput1 = prm;
                            if (!(prmOutput2 is null) && !(prmOutput3 is null))
                            {
                                break;
                            }
                        }
                        if (!string.IsNullOrEmpty(data2Name) 
                            && (prm.Direction == System.Data.ParameterDirection.Output || prm.Direction == System.Data.ParameterDirection.InputOutput) 
                            && prm.ParameterName == data2Name)
                        {
                            prmOutput2 = prm;
                            if (!(prmOutput1 is null) && !(prmOutput3 is null))
                            {
                                break;
                            }
                        }
                        if (!string.IsNullOrEmpty(data3Name) 
                            && (prm.Direction == System.Data.ParameterDirection.Output || prm.Direction == System.Data.ParameterDirection.InputOutput) 
                            && prm.ParameterName == data3Name)
                        {
                            prmOutput3 = prm;
                            if (!(prmOutput1 is null) && !(prmOutput2 is null))
                            {
                                break;
                            }
                        }
                    }
                    TValue1 value1 = default(TValue1);
                    TValue2 value2 = default(TValue2);
                    TValue3 value3 = default(TValue3);
                    if ((prmOutput1 is null && !string.IsNullOrEmpty(data1Name)) || (prmOutput2 is null && !string.IsNullOrEmpty(data2Name)) || (prmOutput3 is null && !string.IsNullOrEmpty(data3Name)))
                    {
                        var rdr = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow, cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();
                        if (rdr.Read())
                        {
                            if ((prmOutput1 is null) && !string.IsNullOrEmpty(data1Name))
                            {
                                value1 = rdr.GetFieldValue<TValue1>(rdr.GetOrdinal(data1Name));
                            }
                            if ((prmOutput2 is null) && !string.IsNullOrEmpty(data2Name))
                            {
                                value2 = rdr.GetFieldValue<TValue2>(rdr.GetOrdinal(data2Name));
                            }
                            if ((prmOutput3 is null) && !string.IsNullOrEmpty(data3Name))
                            {
                                value3 = rdr.GetFieldValue<TValue3>(rdr.GetOrdinal(data3Name));
                            }
                        }
                    }
                    else
                    {
                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                    if (!(prmOutput1 is null))
                    {
                        if (prmOutput1.Value is null || prmOutput1.Value == System.DBNull.Value)
                        {
                            throw new UnexpectedSqlResultException($"Database query {query.Name} expected to output the value of { data1Name }, but the database value was null.");
                        }
                        value1 = (TValue1)prmOutput1.Value;
                    }
                    if (!(prmOutput2 is null))
                    {
                        if (prmOutput2.Value is null || prmOutput2.Value == System.DBNull.Value)
                        {
                            throw new UnexpectedSqlResultException($"Database query {query.Name} expected to output the value of { data2Name }, but the database value was null.");
                        }
                        value2 = (TValue2)prmOutput2.Value;
                    }
                    if (!(prmOutput3 is null))
                    {
                        if (prmOutput3.Value is null || prmOutput3.Value == System.DBNull.Value)
                        {
                            throw new UnexpectedSqlResultException($"Database query {query.Name} expected to output the value of { data3Name }, but the database value was null.");
                        }
                        value3 = (TValue3)prmOutput3.Value;
                    }
                    return (value1, value2, value3);
                }
            }
		}

        private async Task<TModel> ExecuteQueryWithDelegateAsync<TModel, TArg>(TModel instance, Query query, DbParameterCollection parameters, IDictionary<string, object> parameterValues, short shardId, QueryResultModelHandler<TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken)
		{
			var result = default(TModel);
			cancellationToken.ThrowIfCancellationRequested();
			using (var connection = this._dataProviderServices.NewConnection(this._connectionConfig.GetConnectionString(_logger)))
			{
				await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

				cancellationToken.ThrowIfCancellationRequested();
				using (var cmd = this._dataProviderServices.NewCommand(query.Sql, connection))
				{
					cmd.CommandType = query.Type;
					this._dataProviderServices.SetParameters(cmd, query.ParameterNames, parameters, parameterValues);
					var cmdType = System.Data.CommandBehavior.Default;
					if (isTopOne)
					{
						cmdType = System.Data.CommandBehavior.SingleRow;
					}
					using (var dataReader = await cmd.ExecuteReaderAsync(cmdType, cancellationToken).ConfigureAwait(false))
					{
						cancellationToken.ThrowIfCancellationRequested();

						result = resultHandler(instance, shardId, query.Sql, optionalArgument, dataReader, cmd.Parameters, _connectionName, this._logger);
					}
				}
			}
			return result;
		}

		private async Task ExecuteRunAsync(Query query, DbParameterCollection parameters, IDictionary<string, object> parameterValues, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			using (var connection = this._dataProviderServices.NewConnection(this._connectionConfig.GetConnectionString(_logger)))
			{
				await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

				cancellationToken.ThrowIfCancellationRequested();
				using (var cmd = this._dataProviderServices.NewCommand(query.Sql, connection))
				{
					cmd.CommandType = query.Type;
					this._dataProviderServices.SetParameters(cmd, query.ParameterNames, parameters, parameterValues);
					await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
				}
			}
		}

        private async Task<TResult> ExecuteBatchAsync<TResult>(BatchBase<TResult> batch, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = default(TResult);
            using (var connection = this._dataProviderServices.NewConnection(this._connectionConfig.GetConnectionString(_logger)))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                using (var trn = connection.BeginTransaction())
                {
                    result = await batch.Execute(_shardId, connection, trn, _connectionName, _dataProviderServices, _logger, cancellationToken);
                    trn.Commit();
                }
            }
            return result;
        }
        #endregion
    }
}
