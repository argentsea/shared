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
    // This file contains the nested ShardDataConnection class. It is nested because it needs to inherit the generic definitions of its parent.
    public abstract partial class ShardSetsBase<TShard, TConfiguration> : ICollection where TShard : IComparable where TConfiguration : class, IShardSetsConfigurationOptions<TShard>, new()
    {
        /// <summary>
        /// This class represents an actual connection to the database. There may be two connections per shard.
        /// </summary>
        public class ShardDataConnection
        {
            internal readonly DataConnectionManager<TShard> _manager;

            internal ShardDataConnection(ShardSetsBase<TShard, TConfiguration> parent, TShard shardId, IDataConnection config)
            {
                _manager = new DataConnectionManager<TShard>(shardId,
                    parent._dataProviderServices, config,
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
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>The retrieved value.</returns>
            public Task<TValue> LookupAsync<TValue>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                => _manager.LookupAsync<TValue>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), cancellationToken);

            /// <summary>
            /// Connect to the database and return a single value.
            /// </summary>
            /// <typeparam name="TValue">The expected type of the return value.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the value.</param>
            /// <param name="parameters">A parameters collction. Input parameters may be used to find the parameter; will return the value of the first output (or input/output) parameter. If TValue is an int, will also return the sproc return value.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>The retrieved value.</returns>
            public Task<TValue> LookupAsync<TValue>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                => _manager.LookupAsync<TValue>(query, parameters, -1, cancellationToken);

            /// <summary>
            /// Connect to the database and return the values as a list of objects.
            /// </summary>
            /// <typeparam name="TModel">The type of object to be listed.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>A list containing an object for each data row.</returns>
            public Task<IList<TModel>> MapListAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
                => _manager.ListAsync<TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken);

            /// <summary>
            /// Connect to the database and return the values as a list of objects.
            /// </summary>
            /// <typeparam name="TModel">The type of object to be listed.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>A list containing an object for each data row.</returns>
            public Task<IList<TModel>> MapListAsync<TModel>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : class, new()
                => _manager.ListAsync<TModel>(query, parameters, -1, null, cancellationToken);


            /// <summary>
            /// Connect to the database and return the TModel object returned by the delegate.
            /// </summary>
            /// <typeparam name="TModel">The type of the object to be returned.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="resultHandler">A method with a signature that corresponds to the QueryResultModelHandler delegate, which converts the provided DataReader and output parameters and returns an object of type TModel.</param>
            /// <param name="isTopOne">If the procedure or function is expected to return only one record, setting this to True provides a minor optimization.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>An object of type TModel, as created and populated by the provided delegate.</returns>
            public Task<TModel> QueryAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<TShard, object, TModel> resultHandler, bool isTopOne, CancellationToken cancellationToken) where TModel : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, resultHandler, isTopOne, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return the TModel object returned by the delegate.
            /// </summary>
            /// <typeparam name="TModel">The type of the object to be returned.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="resultHandler">A method with a signature that corresponds to the QueryResultModelHandler delegate, which converts the provided DataReader and output parameters and returns an object of type TModel.</param>
            /// <param name="isTopOne">If the procedure or function is expected to return only one record, setting this to True provides a minor optimization.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>An object of type TModel, as created and populated by the provided delegate.</returns>
			public Task<TModel> QueryAsync<TModel>(Query query, DbParameterCollection parameters, QueryResultModelHandler<TShard, object, TModel> resultHandler, bool isTopOne, CancellationToken cancellationToken) where TModel : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, resultHandler, isTopOne, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return the TModel object returned by the delegate.
            /// </summary>
            /// <typeparam name="TArg"></typeparam>
            /// <typeparam name="TModel">The type of the object to be returned.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="resultHandler">A method with a signature that corresponds to the QueryResultModelHandler delegate, which converts the provided DataReader and output parameters and returns an object of type TModel.</param>
            /// <param name="isTopOne">If the procedure or function is expected to return only one record, setting this to True provides a minor optimization.</param>
            /// <param name="optionalArgument">An object of type TArg which can be used to pass non-datatabase data to the result-generating delegate.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>An object of type TModel, as created and populated by the provided delegate.</returns>
			public Task<TModel> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) where TModel : class, new()
                => _manager.QueryAsync<TArg, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, resultHandler, isTopOne, optionalArgument, cancellationToken);

            /// <summary>
            /// Connect to the database and return the TModel object returned by the delegate.
            /// </summary>
            /// <typeparam name="TArg"></typeparam>
            /// <typeparam name="TModel">The type of the object to be returned.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="resultHandler">A method with a signature that corresponds to the QueryResultModelHandler delegate, which converts the provided DataReader and output parameters and returns an object of type TModel.</param>
            /// <param name="isTopOne">If the procedure or function is expected to return only one record, setting this to True provides a minor optimization.</param>
            /// <param name="optionalArgument">An object of type TArg which can be used to pass non-datatabase data to the result-generating delegate.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>An object of type TModel, as created and populated by the provided delegate.</returns>
			public Task<TModel> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken) where TModel : class, new()
                => _manager.QueryAsync<TArg, TModel>(query, parameters, -1, null, resultHandler, isTopOne, optionalArgument, cancellationToken);


            /// <summary>
            /// Executes a database procedure or function that does not return a data result.
            /// </summary>
            /// <param name="sprocName">The stored procedure or function to call.</param>
            /// <param name="parameters">The query parameters with values set.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>Throws an error if not successful.</returns>
            public Task RunAsync(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                => _manager.RunAsync(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken);

            /// <summary>
            /// Executes a database procedure or function that does not return a data result.
            /// </summary>
            /// <param name="sprocName">The stored procedure or function to call.</param>
            /// <param name="parameters">The query parameters with values set.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>Throws an error if not successful.</returns>
			public Task RunAsync(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                => _manager.RunAsync(query, parameters, -1, null, cancellationToken);


            /// <summary>
            /// Execute a set of commands within a single transaction.
            /// </summary>
            /// <typeparam name="TResult">The optional return type specified in the batch.</typeparam>
            /// <param name="batch">The QueryBatch object.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns>The last valid TResult type returned by the collection of commands.</returns>
            public Task<TResult> RunAsync<TResult>(ShardBatch<TShard, TResult> batch, CancellationToken cancellationToken)
                => _manager.RunBatchAsync<TResult>(batch, cancellationToken);

            #endregion
            #region GetOut overloads
            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, Mapper.DummyType, TModel>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult>
                (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>
                (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
                (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
                (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
                (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
                (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
                (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
                (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult>
                (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>
                (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
                (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
                (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
                (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
                (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
                (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
                (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, false, null, cancellationToken);

            #endregion
            #region Read overloads
            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>
                (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
                (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
                (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
                (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
                (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
                (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
                (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results parameters.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>
                (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
                (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
                (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
                (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
                (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
                (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, false, null, cancellationToken);

            /// <summary>
            /// Connect to the database and return an object of the specified type built from the corresponding data reader results.
            /// </summary>
            /// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The stored procedure to call to fetch the data.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
            /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
            /// <returns></returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
                (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _manager.QueryAsync<object, TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, false, null, cancellationToken);
            #endregion
        }
    }
}
