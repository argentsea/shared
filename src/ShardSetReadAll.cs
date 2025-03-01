﻿// © John Hicks. All rights reserved. Licensed under the MIT license.
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
    public abstract partial class ShardSetsBase<TConfiguration> where TConfiguration : class, IShardSetsConfigurationOptions, new()
    {
        /// <summary>
        /// This class hosts the concurrent shard set read methods which return all valid results.
        /// </summary>
        public class ShardSetReadAll
        {
            private readonly ShardSet _shardSet;
            internal ShardSetReadAll(ShardSet shardSet)
            {
                _shardSet = shardSet;
            }
            #region QueryAsync
            /// <summary>
            /// Query across all shards in the shard set using a handler delegate.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> QueryAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<object, TModel> resultHandler, CancellationToken cancellationToken)
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, resultHandler, null, cancellationToken);

            /// <summary>
            /// Query across the specified shards, generating results using a handler delegate.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> QueryAsync<TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, QueryResultModelHandler<object, TModel> resultHandler, CancellationToken cancellationToken)
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, resultHandler, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set using a handler delegate.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> QueryAsync<TModel>(Query query, DbParameterCollection parameters, QueryResultModelHandler<object, TModel> resultHandler, object dataObject, CancellationToken cancellationToken)
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, resultHandler, null, cancellationToken);

            /// <summary>
            /// Query across the specified shards, generating results using a handler delegate.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> QueryAsync<TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, QueryResultModelHandler<object, TModel> resultHandler, CancellationToken cancellationToken)
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, resultHandler, null, cancellationToken);


            /// <summary>
            /// Query across all shards in the shard set using a handler delegate.
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
                => _shardSet.ReadQueryAllAsync<TArg, TModel>(query, parameters, null, shardParameterName, resultHandler, dataObject, cancellationToken);

            /// <summary>
            /// Query across the specified shards, generating results using a handler delegate.
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, QueryResultModelHandler<TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
                => _shardSet.ReadQueryAllAsync<TArg, TModel>(query, parameters, shardParameterValues, null, resultHandler, dataObject, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set using a handler delegate.
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, QueryResultModelHandler<TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
                => _shardSet.ReadQueryAllAsync<TArg, TModel>(query, parameters, null, null, resultHandler, dataObject, cancellationToken);


            /// <summary>
            /// Query across the specified shards, generating results using a handler delegate.
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, QueryResultModelHandler<TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
                => _shardSet.ReadQueryAllAsync<TArg, TModel>(query, parameters, shardParameterValues, shardParameterName, resultHandler, dataObject, cancellationToken);
            #endregion
            #region MapListAsync
            /// <summary>
            /// Returns a list of objects created by the specified delegate.
            /// </summary>
            /// <typeparam name="TModel">The data type of the objects in the list result.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of TModel objects, built by the delegate from the data results.</returns>
            public Task<List<TModel>> MapListAsync<TModel>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : new()
                => _shardSet.MapListAsync<TModel>(query, parameters, null, null, cancellationToken);

            /// <summary>
            /// Returns a list of objects created by the specified delegate.
            /// </summary>
            /// <typeparam name="TModel">The data type of the objects in the list result.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of TModel objects, built by the delegate from the data results.</returns>
            public Task<List<TModel>> MapListAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken) where TModel : new()
                => _shardSet.MapListAsync<TModel>(query, parameters, null, shardParameterName, cancellationToken);

            /// <summary>
            /// Returns a list of objects created by the specified delegate.
            /// </summary>
            /// <typeparam name="TModel">The data type of the objects in the list result.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of TModel objects, built by the delegate from the data results.</returns>
            public Task<List<TModel>> MapListAsync<TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken) where TModel : new()
                => _shardSet.MapListAsync<TModel>(query, parameters, shardParameterValues, null, cancellationToken);

            /// <summary>
            /// Returns a list of objects created by the specified delegate.
            /// </summary>
            /// <typeparam name="TModel">The data type of the objects in the list result.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of TModel objects, built by the delegate from the data results.</returns>
            public Task<List<TModel>> MapListAsync<TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken) where TModel : new()
                => _shardSet.MapListAsync<TModel>(query, parameters, shardParameterValues, shardParameterName, cancellationToken);
            #endregion
            #region MapReaderAsync
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken) where TModel : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken) where TModel : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel>, null, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken) where TModel : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult>, null, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult>, null, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                where TReaderResult7 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                where TReaderResult7 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                where TReaderResult7 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                where TReaderResult7 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

            #endregion
            #region MapOutputAsync
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken) where TModel : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken) where TModel : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken) where TModel : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel>, null, cancellationToken);




            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);


            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                where TReaderResult7 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                where TReaderResult7 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                where TReaderResult7 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<List<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : new()
                where TReaderResult0 : new()
                where TReaderResult1 : new()
                where TReaderResult2 : new()
                where TReaderResult3 : new()
                where TReaderResult4 : new()
                where TReaderResult5 : new()
                where TReaderResult6 : new()
                where TReaderResult7 : new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

            #endregion
            #region ListAsync
            /// <summary>
            /// Connect to the shard set and return a combined list of values from the specified column.
            /// </summary>
            /// <typeparam name="TValue">The type of the return value, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="columnName">This should match the name of a column containing the values.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of values representing the combined results of all of the shards.</returns>
            public Task<List<TValue>> ListAsync<TValue>(Query query, string columnName, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                => _shardSet.ListAsync<TValue>(query, parameters, columnName, shardParameterValues, shardParameterName, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a list of values from the specified column.
            /// </summary>
            /// <typeparam name="TValue">The type of the return value, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="columnName">This should match the name of a column containing the values.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of values representing the combined results of all of the shards.</returns>
            public Task<List<TValue>> ListAsync<TValue>(Query query, string columnName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                => _shardSet.ListAsync<TValue>(query, parameters, columnName, null, shardParameterName, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of values from the specified column.
            /// </summary>
            /// <typeparam name="TValue">The type of the return value, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="columnName">This should match the name of a column containing the values.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of values representing the combined results of all of the shards.</returns>
            public Task<List<TValue>> ListAsync<TValue>(Query query, string columnName, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                => _shardSet.ListAsync<TValue>(query, parameters, columnName, shardParameterValues, null, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of values from the specified column.
            /// </summary>
            /// <typeparam name="TValue">The type of the return value, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="columnName">This should match the name of a column containing the values.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of values representing the combined results of all of the shards.</returns>
            public Task<List<TValue>> ListAsync<TValue>(Query query, string columnName, DbParameterCollection parameters, CancellationToken cancellationToken)
                => _shardSet.ListAsync<TValue>(query, parameters, columnName, null, null, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardKey values using the specified record Id column and the ShardId of the current shard.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardKey result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardKey type.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardKey values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord>>> ListAsync<TRecord>(Query query, char origin, string recordColumnName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TRecord : IComparable
                => _shardSet.ListAsync<TRecord>(query, origin, recordColumnName, parameters, null, shardParameterName, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardKey values using the specified record Id column and the ShardId of the current shard.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardKey result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardKey type.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterValues"></param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardKey values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord>>> ListAsync<TRecord>(Query query, char origin, string recordColumnName, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TRecord : IComparable
                => _shardSet.ListAsync<TRecord>(query, origin, recordColumnName, parameters, shardParameterValues, null, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardKey values using the specified record Id column and the ShardId of the current shard.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardKey result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardKey type.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardKey values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord>>> ListAsync<TRecord>(Query query, char origin, string recordColumnName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TRecord : IComparable
                => _shardSet.ListAsync<TRecord>(query, origin, recordColumnName, parameters, null, null, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardKey values using the specified record Id column and the ShardId of the current shard.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardKey result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardKey type.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="shardParameterValues"></param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardKey values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord>>> ListAsync<TRecord>(Query query, char origin, string recordColumnName, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TRecord : IComparable
                => _shardSet.ListAsync<TRecord>(query, origin, recordColumnName, parameters, shardParameterValues, shardParameterName, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardKey values from the specified columns.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardKey result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardKey type.</param>
            /// <param name="shardColumnName">This should match the name of a column containing the ShardID component of the ShardKey.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardKey values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord>>> ListAsync<TRecord>(Query query, char origin, string shardColumnName, string recordColumnName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TRecord : IComparable
                => _shardSet.ListAsync<TRecord>(query, origin, shardColumnName, recordColumnName, parameters, null, shardParameterName, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardKey values from the specified columns.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardKey result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardKey type.</param>
            /// <param name="shardColumnName">This should match the name of a column containing the ShardID component of the ShardKey.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardKey values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord>>> ListAsync<TRecord>(Query query, char origin, string shardColumnName, string recordColumnName, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TRecord : IComparable
                => _shardSet.ListAsync<TRecord>(query, origin, shardColumnName, recordColumnName, parameters, shardParameterValues, null, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardKey values from the specified columns.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardKey result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardKey type.</param>
            /// <param name="shardColumnName">This should match the name of a column containing the ShardID component of the ShardKey.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardKey values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord>>> ListAsync<TRecord>(Query query, char origin, string shardColumnName, string recordColumnName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TRecord : IComparable
                => _shardSet.ListAsync<TRecord>(query, origin, shardColumnName, recordColumnName, parameters, null, null, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardKey values from the specified columns.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardKey result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardKey type.</param>
            /// <param name="shardColumnName">This should match the name of a column containing the ShardID component of the ShardKey.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardKey values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord>>> ListAsync<TRecord>(Query query, char origin, string shardColumnName, string recordColumnName, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TRecord : IComparable
                => _shardSet.ListAsync<TRecord>(query, origin, shardColumnName, recordColumnName, parameters, shardParameterValues, shardParameterName, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardChild values using the specified record Id column and the ShardId of the current shard.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardChild result.</typeparam>
            /// <typeparam name="TChild">The type of the child id component in the ShardChild result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardChild type.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardChild.</param>
            /// <param name="childColumnName">This should match the name of a column containing the ChildID component of the ShardChild.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardChild values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string recordColumnName, string childColumnName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TRecord : IComparable
                where TChild : IComparable
                => _shardSet.ListAsync<TRecord, TChild>(query, origin, recordColumnName, childColumnName, parameters, null, shardParameterName, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardChild values using the specified record Id column and the ShardId of the current shard.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardChild result.</typeparam>
            /// <typeparam name="TChild">The type of the child id component in the ShardChild result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardChild type.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardChild.</param>
            /// <param name="childColumnName">This should match the name of a column containing the ChildID component of the ShardChild.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterValues"></param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardChild values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string recordColumnName, string childColumnName, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TRecord : IComparable
                where TChild : IComparable
                => _shardSet.ListAsync<TRecord, TChild>(query, origin, recordColumnName, childColumnName, parameters, shardParameterValues, null, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardChild values using the specified record Id column and the ShardId of the current shard.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardChild result.</typeparam>
            /// <typeparam name="TChild">The type of the child id component in the ShardChild result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardChild type.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardChild.</param>
            /// <param name="childColumnName">This should match the name of a column containing the ChildID component of the ShardChild.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardChild values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string recordColumnName, string childColumnName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TRecord : IComparable
                where TChild : IComparable
                => _shardSet.ListAsync<TRecord, TChild>(query, origin, recordColumnName, childColumnName, parameters, null, null, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardChild values using the specified record Id column and the ShardId of the current shard.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardChild result.</typeparam>
            /// <typeparam name="TChild">The type of the child id component in the ShardChild result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardChild type.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardChild.</param>
            /// <param name="childColumnName">This should match the name of a column containing the ChildID component of the ShardChild.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="shardParameterValues"></param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardChild values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string recordColumnName, string childColumnName, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TRecord : IComparable
                where TChild : IComparable
                => _shardSet.ListAsync<TRecord, TChild>(query, origin, recordColumnName, childColumnName, parameters, shardParameterValues, shardParameterName, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardChild values from the specified columns.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardChild result.</typeparam>
            /// <typeparam name="TChild">The type of the child id component in the ShardChild result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardChild type.</param>
            /// <param name="shardColumnName">This should match the name of a column containing the ShardID component of the ShardChild.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardChild.</param>
            /// <param name="childColumnName">This should match the name of a column containing the ChildID component of the ShardChild.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardChild values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string shardColumnName, string recordColumnName, string childColumnName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TRecord : IComparable
                where TChild : IComparable
                => _shardSet.ListAsync<TRecord, TChild>(query, origin, shardColumnName, recordColumnName, childColumnName, parameters, null, shardParameterName, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardChild values from the specified columns.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardChild result.</typeparam>
            /// <typeparam name="TChild">The type of the child id component in the ShardChild result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardChild type.</param>
            /// <param name="shardColumnName">This should match the name of a column containing the ShardID component of the ShardChild.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardChild.</param>
            /// <param name="childColumnName">This should match the name of a column containing the ChildID component of the ShardChild.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardChild values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string shardColumnName, string recordColumnName, string childColumnName, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
                where TRecord : IComparable
                where TChild : IComparable
                => _shardSet.ListAsync<TRecord, TChild>(query, origin, shardColumnName, recordColumnName, childColumnName, parameters, shardParameterValues, null, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardChild values from the specified columns.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardChild result.</typeparam>
            /// <typeparam name="TChild">The type of the child id component in the ShardChild result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardChild type.</param>
            /// <param name="shardColumnName">This should match the name of a column containing the ShardID component of the ShardChild.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardChild.</param>
            /// <param name="childColumnName">This should match the name of a column containing the ChildID component of the ShardChild.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardChild values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string shardColumnName, string recordColumnName, string childColumnName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TRecord : IComparable
                where TChild : IComparable
                => _shardSet.ListAsync<TRecord, TChild>(query, origin, shardColumnName, recordColumnName, childColumnName, parameters, null, null, cancellationToken);

            /// <summary>
            /// Connect to the shard set and return a combined list of ShardChild values from the specified columns.
            /// </summary>
            /// <typeparam name="TRecord">The type of the record id component in the ShardChild result.</typeparam>
            /// <typeparam name="TChild">The type of the child id component in the ShardChild result.</typeparam>
            /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
            /// <param name="origin">Origin value to indicate the ShardChild type.</param>
            /// <param name="shardColumnName">This should match the name of a column containing the ShardID component of the ShardChild.</param>
            /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardChild.</param>
            /// <param name="childColumnName">This should match the name of a column containing the ChildID component of the ShardChild.</param>
            /// <param name="parameters">The query parameters.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of ShardChild values representing the combined results of all of the shards or the specified shards.</returns>
            public Task<List<ShardKey<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string shardColumnName, string recordColumnName, string childColumnName, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TRecord : IComparable
                where TChild : IComparable
                => _shardSet.ListAsync<TRecord, TChild>(query, origin, shardColumnName, recordColumnName, childColumnName, parameters, shardParameterValues, shardParameterName, cancellationToken);

            #endregion
        }
    }
}
