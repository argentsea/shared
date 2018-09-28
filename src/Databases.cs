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
    /// Classes that inherit from this class manage non-sharded database connections.
    /// </summary>
    /// <typeparam name="TConfiguration">A provider-specific implementation of IShardSetConfigurationOptions.</typeparam>
    public abstract class DatabasesBase<TConfiguration> : ICollection where TConfiguration : class, IDatabaseConfigurationOptions, new()
	{
		private readonly object syncRoot = new Lazy<object>();
		private readonly ImmutableDictionary<string, DataConnection> dtn;
        private readonly DataSecurityOptions _securityOptions;
        private readonly DataResilienceOptions _resilienceStrategiesOptions;
        private readonly IDataProviderServiceFactory _dataProviderServices;
        private readonly ILogger _logger;

        public DatabasesBase(
            IOptions<TConfiguration> configOptions,
            IOptions<DataSecurityOptions> securityOptions,
            IOptions<DataResilienceOptions> resilienceStrategiesOptions,
            IDataProviderServiceFactory dataProviderServices,
            ILogger<DatabasesBase<TConfiguration>> logger)
		{

            this._logger = logger;

            if (configOptions?.Value?.DbConnectionsInternal is null)
            {
                logger.LogWarning("The Databases collection is missing required database connection information. Your application configuration may be missing a database configuration section.");
            }

            this._securityOptions = securityOptions?.Value;
            this._resilienceStrategiesOptions = resilienceStrategiesOptions?.Value;
            this._dataProviderServices = dataProviderServices;
            if (!(configOptions?.Value?.DbConnectionsInternal is null))
            {
                var bdr = ImmutableDictionary.CreateBuilder<string, DataConnection>();
                foreach (var db in configOptions.Value.DbConnectionsInternal)
                {
                    if (db is null)
                    {
                        throw new Exception($"A database connection configuration was not valid; the configuration provider returned null.");
                    }
                    bdr.Add(db.DatabaseKey, new DataConnection(this, db.DataConnectionInternal));
                }
                this.dtn = bdr.ToImmutable();
            }
            else
            {
                dtn = ImmutableDictionary<string, DataConnection>.Empty;
            }

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
	

        #region Nested classes

        public class DataConnection
		{
			private readonly DataConnectionManager<int> _manager;

			internal DataConnection(DatabasesBase<TConfiguration> parent, IConnectionConfiguration config)
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

                _manager = new DataConnectionManager<int>(0, parent._dataProviderServices, drc,
					config.GetConnectionString(), config.ConnectionDescription, parent._logger);
                config.SetConfigurationOptions(parent._securityOptions, parent._resilienceStrategiesOptions);
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
		#endregion
    }
}
