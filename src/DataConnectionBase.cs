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
    internal class DataConnectionManager<TShard> where TShard: IComparable //<TConfiguration> where TConfiguration : class, IDbDataConfigurationOptions, new()
	{
		#region Private variables and constructors
		private readonly ILogger _logger;
		private readonly Dictionary<string, Policy> _commandPolicy = new Dictionary<string, Policy>();
		private readonly DataResilienceConfiguration _resilienceStrategy;
		private readonly IDataProviderServiceFactory _dataProviderServices;
		private readonly string _connectionString;
		private readonly string _connectionName;
		private readonly TShard _shardId;

		private Policy ConnectionPolicy { get; set; }
		private Dictionary<string, Policy> CommandPolicies { get; set; } = new Dictionary<string, Policy>();

		protected DataConnectionManager() { } //hide ctor

		internal DataConnectionManager(TShard shardId, IDataProviderServiceFactory dataProviderServices, string resilienceStrategyKey, string connectionString, string connectionName, ImmutableDictionary<string, DataResilienceConfiguration> resilienceStrategies, ILogger logger)
		{
			this._logger = logger;
			this._dataProviderServices = dataProviderServices;
			this._connectionString = connectionString;
			this._connectionName = connectionName;
			this._resilienceStrategy = DataStoreHelper.GetResilienceStrategy(resilienceStrategies, resilienceStrategyKey, connectionName, _logger);
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

		private void SetShardParameter(DbParameterCollection parameters, int shardParameterOrdinal, TShard value)
		{
			if (shardParameterOrdinal >= 0 && shardParameterOrdinal < parameters.Count)
			{
				parameters[shardParameterOrdinal].Value = this._shardId;
			}
		}
		#endregion

		#region Public properties

		public string ConnectionString { get => this._connectionString; }
		#endregion

		#region Public data fetch methods
		/// <summary>
		/// Connect to the database and return a single value.
		/// </summary>
		/// <typeparam name="TValue">The expected type of the return value.</typeparam>
		/// <param name="sprocName">The stored procedure to call to fetch the value.</param>
		/// <param name="parameters">A parameters collction. Input parameters may be used to find the parameter; will return the value of the first output (or input/output) parameter. If TValue is an int, will also return the sproc return value.</param>
		/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		/// <returns>The retrieved value.</returns>
		public async Task<TValue> LookupAsync<TValue>(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken)
		{
			//return await DataStoreHelper.AdoExecute<TValue>(sprocName, parameters, this._commandPolicy, this._dataProviderServices, this._resilienceStrategy, this._connectionName, this.HandleCommandRetry, this._logger, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			var startTimestamp = Stopwatch.GetTimestamp();

			if (!this._commandPolicy.ContainsKey(sprocName))
			{
				this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
			}
			SetShardParameter(parameters, shardParameterOrdinal, this._shardId);
			var result = await this._commandPolicy[sprocName].ExecuteAsync(newToken =>
				ExecuteQueryToValueAsync<TValue>(sprocName, parameters, newToken), cancellationToken).ConfigureAwait(false);

			var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
			_logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
			return result;
		}

		/// <summary>
		/// Connect to the database and return the values as a list of objects.
		/// </summary>
		/// <typeparam name="TResult">The type of object to be listed.</typeparam>
		/// <param name="sprocName">The stored procedure to call to fetch the data.</param>
		/// <param name="parameters">The query parameters.</param>
		/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		/// <returns>A list containing an object for each data row.</returns>
		public async Task<IList<TResult>> ListAsync<TResult>(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken) where TResult : class, new()
		{
			cancellationToken.ThrowIfCancellationRequested();
			var startTimestamp = Stopwatch.GetTimestamp();

			if (!this._commandPolicy.ContainsKey(sprocName))
			{
				this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
			}
			SetShardParameter(parameters, shardParameterOrdinal, this._shardId);
			var result = await this._commandPolicy[sprocName].ExecuteAsync(newToken =>
				ExecuteQueryToListAsync<TResult>(sprocName, parameters, newToken), cancellationToken).ConfigureAwait(false);

			var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
			_logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
			return result;
		}


		public async Task<TModel> QueryAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) 
            where TModel : class, new()
		{
			cancellationToken.ThrowIfCancellationRequested();
			var startTimestamp = Stopwatch.GetTimestamp();

			if (!this._commandPolicy.ContainsKey(sprocName))
			{
				this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
			}
			SetShardParameter(parameters, shardParameterOrdinal, this._shardId);
			var result = await this._commandPolicy[sprocName].ExecuteAsync(newToken =>
				this.ExecuteQueryWithDelegateAsync<TModel, TArg>(sprocName, parameters, this._shardId, resultHandler, isTopOne, optionalArgument, newToken), cancellationToken).ConfigureAwait(false);

			var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
			_logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
			return result;
		}



		public async Task RunAsync(string sprocName, DbParameterCollection parameters, int shardParameterOrdinal, CancellationToken cancellationToken)
		{

			cancellationToken.ThrowIfCancellationRequested();
			var startTimestamp = Stopwatch.GetTimestamp();

			if (!this._commandPolicy.ContainsKey(sprocName))
			{
				this._commandPolicy.Add(sprocName, this.GetCommandResiliencePolicy(sprocName));
			}
			SetShardParameter(parameters, shardParameterOrdinal, this._shardId);
			await this._commandPolicy[sprocName].ExecuteAsync(newToken => this.ExecuteRunAsync(sprocName, parameters, newToken), cancellationToken).ConfigureAwait(false);

			var elapsedMS = (long)((Stopwatch.GetTimestamp() - startTimestamp) * TimestampToMilliseconds);
			_logger.TraceDbCmdExecuted(sprocName, this._connectionName, elapsedMS);
			//return result;
		}

		#endregion
		#region Private Handlers

		private async Task<IList<TModel>> ExecuteQueryToListAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken) 
            where TModel : class, new()
		{
			IList<TModel> result = null;
			cancellationToken.ThrowIfCancellationRequested();
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
						result = Mapper.FromDataReader<TModel>(dataReader, _logger);
					}
				}
			}
			if (result is null)
			{
				result = new List<TModel>();
			}
			return result;
		}
		private async Task<TResult> ExecuteQueryToValueAsync<TResult>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
        {
            TResult result = default(TResult);
			cancellationToken.ThrowIfCancellationRequested();
			//SqlExceptionsEncountered.Clear();
			using (var connection = this._dataProviderServices.NewConnection(this._connectionString))
			{
				//await this._connectPolicy.ExecuteAsync(() => connection.OpenAsync(cancellationToken)).ConfigureAwait(false);
				await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
				cancellationToken.ThrowIfCancellationRequested();
				using (var cmd = this._dataProviderServices.NewCommand(sprocName, connection))
				{
					cmd.CommandType = System.Data.CommandType.StoredProcedure;
					this._dataProviderServices.SetParameters(cmd, parameters);
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
							if (result is Nullable && System.DBNull.Value.Equals(prm.Value))
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
			throw new UnexpectedSqlResultException($"Database query {sprocName} expected to output a type of {typeof(TResult).ToString()}, but no output values were found.");
		}

		private async Task<TResult> ExecuteQueryWithDelegateAsync<TResult, TArg>(string sprocName, DbParameterCollection parameters, TShard shardId, QueryResultModelHandler<TShard, TArg, TResult> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) where TResult : class, new()
		{
			var result = default(TResult);
			cancellationToken.ThrowIfCancellationRequested();
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

						result = resultHandler(shardId, sprocName, optionalArgument, dataReader, cmd.Parameters, _connectionName, this._logger);
					}
				}
			}
			return result;
		}

		private async Task ExecuteRunAsync(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
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
}
