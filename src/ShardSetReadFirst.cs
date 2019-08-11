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
    /// <summary>
    /// This class hosts the concurrent shard set read methods which return the fist valid result.
    /// </summary>
    public class ShardSetReadFirst<TConfiguration> where TConfiguration : class, IShardSetsConfigurationOptions, new()
    {
        private readonly ShardSet<TConfiguration> _shardSet;

        internal ShardSetReadFirst(ShardSet<TConfiguration> shardSet)
        {
            _shardSet = shardSet;
        }
        #region QueryAsync
        /// <summary>
        /// Query across all shards in the shard set, returning the first non-null result created by a handler delegate .
        /// </summary>
        /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
        /// <typeparam name="TModel">The data object return type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
        /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non-null object obtained from any shard.</returns>
        public Task<TModel> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, QueryResultModelHandler<TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
            => _shardSet.ReadQueryFirstAsync<TArg, TModel>(query, parameters, null, null, resultHandler, dataObject, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, returning the first non-null result created by a handler delegate .
        /// </summary>
        /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
        /// <typeparam name="TModel">The data object return type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
        /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non-null object obtained from any shard.</returns>
        public Task<TModel> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
            => _shardSet.ReadQueryFirstAsync<TArg, TModel>(query, parameters, null, shardParameterName, resultHandler, dataObject, cancellationToken);

        /// <summary>
        /// Query across the specified shards, returning the first non-null result created by a handler delegate .
        /// </summary>
        /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
        /// <typeparam name="TModel">The data object return type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
        /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
        /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non-null object obtained from any shard.</returns>
        public Task<TModel> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, QueryResultModelHandler<TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
            => _shardSet.ReadQueryFirstAsync<TArg, TModel>(query, parameters, shardParameterValues, null, resultHandler, dataObject, cancellationToken);

        /// <summary>
        /// Query across the specified shards, returning the first non-null result created by a handler delegate .
        /// </summary>
        /// <typeparam name="TModel">The data object return type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non-null object obtained from any shard.</returns>
        public Task<TModel> QueryAsync<TModel>(Query query, DbParameterCollection parameters, QueryResultModelHandler<object, TModel> resultHandler, CancellationToken cancellationToken)
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, resultHandler, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, returning the first non-null result created by a handler delegate .
        /// </summary>
        /// <typeparam name="TModel">The data object return type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
        /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non-null object obtained from any shard.</returns>
        public Task<TModel> QueryAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<object, TModel> resultHandler, CancellationToken cancellationToken)
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, resultHandler, null, cancellationToken);

        /// <summary>
        /// Query across the specified shards, returning the first non-null result created by a handler delegate .
        /// </summary>
        /// <typeparam name="TModel">The data object return type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
        /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
        /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non-null object obtained from any shard.</returns>
        public Task<TModel> QueryAsync<TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, QueryResultModelHandler<object, TModel> resultHandler, CancellationToken cancellationToken)
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, resultHandler, null, cancellationToken);


        /// <summary>
        /// Query across the specified shards, returning the first non-null result created by a handler delegate .
        /// </summary>
        /// <typeparam name="TModel">The data object return type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non-null object obtained from any shard.</returns>
        public Task<TModel> QueryAsync<TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, QueryResultModelHandler<object, TModel> resultHandler, CancellationToken cancellationToken)
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, resultHandler, null, cancellationToken);

        /// <summary>
        /// Query across the specified shards, returning the first non-null result created by a handler delegate .
        /// </summary>
        /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
        /// <typeparam name="TModel">The data object return type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
        /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non-null object obtained from any shard.</returns>
        public Task<TModel> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, QueryResultModelHandler<TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
            => _shardSet.ReadQueryFirstAsync<TArg, TModel>(query, parameters, shardParameterValues, shardParameterName, resultHandler, dataObject, cancellationToken);

        #endregion
        #region MapReaderAsync
        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel>, null, cancellationToken);
        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken) where TModel : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel>, null, cancellationToken);


        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);


        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);


        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);


        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);


        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            where TReaderResult7 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            where TReaderResult7 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            where TReaderResult7 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);


        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
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
        /// <returns>The first non=null object result returned from any shard.</returns>
        public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            where TReaderResult7 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);
        #endregion
        #region MapOutputAsync
        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
        /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
        /// </summary>
        /// <typeparam name="TModel">The data object return type for the list</typeparam>
        /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        /// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
        /// <param name="query">The statement or procedure to be invoked on every instance.</param>
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
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
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
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
        /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
        /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
        /// <returns>A list of the non-null object results returned from any shard.</returns>
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            where TReaderResult7 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

        /// <summary>
        /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            where TReaderResult7 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            where TReaderResult7 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

        /// <summary>
        /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
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
        public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, ShardsValues shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
            where TModel : class, new()
            where TReaderResult0 : class, new()
            where TReaderResult1 : class, new()
            where TReaderResult2 : class, new()
            where TReaderResult3 : class, new()
            where TReaderResult4 : class, new()
            where TReaderResult5 : class, new()
            where TReaderResult6 : class, new()
            where TReaderResult7 : class, new()
            => _shardSet.ReadQueryFirstAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

        #endregion
    }
}
