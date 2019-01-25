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
    // This file contains the nested ShardSetReadAll class. It is nested because it needs to inherit the generic definitions of its parent.
    public abstract partial class ShardSetsBase<TShard, TConfiguration> : ICollection where TShard : IComparable where TConfiguration : class, IShardSetsConfigurationOptions<TShard>, new()
    {
        /// <summary>
        /// This class hosts the concurrent shard set read methods which return all valid results.
        /// </summary>
        public class ShardSetReadAll
        {
            private ShardSetsBase<TShard, TConfiguration>.ShardSet _shardSet;
            internal ShardSetReadAll(ShardSetsBase<TShard, TConfiguration>.ShardSet shardSet)
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
            public Task<IList<TModel>> QueryAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<TShard, object, TModel> resultHandler, CancellationToken cancellationToken)
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
            public Task<IList<TModel>> QueryAsync<TModel>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, QueryResultModelHandler<TShard, object, TModel> resultHandler, CancellationToken cancellationToken)
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
            public Task<IList<TModel>> QueryAsync<TModel>(Query query, DbParameterCollection parameters, QueryResultModelHandler<TShard, object, TModel> resultHandler, object dataObject, CancellationToken cancellationToken)
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
            public Task<IList<TModel>> QueryAsync<TModel>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, QueryResultModelHandler<TShard, object, TModel> resultHandler, CancellationToken cancellationToken)
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
            public Task<IList<TModel>> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
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
            public Task<IList<TModel>> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
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
            public Task<IList<TModel>> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
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
            public Task<IList<TModel>> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
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
            public Task<IList<TModel>> MapListAsync<TModel>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : class, new()
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
            public Task<IList<TModel>> MapListAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
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
            public Task<IList<TModel>> MapListAsync<TModel>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken) where TModel : class, new()
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
            public Task<IList<TModel>> MapListAsync<TModel>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
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
            public Task<IList<TModel>> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken) where TModel : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, null, cancellationToken);


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
            public Task<IList<TModel>> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);
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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);


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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);
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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);


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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);
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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);


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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);
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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);
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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);
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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);


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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);
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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);


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
            public Task<IList<TModel>> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns non-null result using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken) where TModel : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel>, null, cancellationToken);




            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="query">The statement or procedure to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or statement.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);


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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

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
            public Task<IList<TModel>> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(Query query, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryAllAsync<object, TModel>(query, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

            #endregion

        }
    }
}
