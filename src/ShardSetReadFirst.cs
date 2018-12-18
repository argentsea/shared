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
    // This file contains the nested ShardSetReadFirst class. It is nested because it needs to inherit the generic definitions of its parent.
    public abstract partial class ShardSetsBase<TShard, TConfiguration> : ICollection where TShard : IComparable where TConfiguration : class, IShardSetsConfigurationOptions<TShard>, new()
    {
        /// <summary>
        /// This class hosts the concurrent shard set read methods which return the fist valid result.
        /// </summary>
        public class ShardSetReadFirst
        {
            private ShardSetsBase<TShard, TConfiguration>.ShardSet _shardSet;
            internal ShardSetReadFirst(ShardSetsBase<TShard, TConfiguration>.ShardSet shardSet)
            {
                _shardSet = shardSet;
            }
            #region QueryAsync
            /// <summary>
            /// Query across all shards in the shard set, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public Task<TModel> QueryAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
                => _shardSet.ReadQueryFirstAsync<TArg, TModel>(sprocName, parameters, null, null, resultHandler, dataObject, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public Task<TModel> QueryAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
                => _shardSet.ReadQueryFirstAsync<TArg, TModel>(sprocName, parameters, null, shardParameterName, resultHandler, dataObject, cancellationToken);

            /// <summary>
            /// Query across the specified shards, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public Task<TModel> QueryAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
                => _shardSet.ReadQueryFirstAsync<TArg, TModel>(sprocName, parameters, shardParameterValues, null, resultHandler, dataObject, cancellationToken);

            /// <summary>
            /// Query across the specified shards, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, QueryResultModelHandler<TShard, object, TModel> resultHandler, CancellationToken cancellationToken)
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, resultHandler, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<TShard, object, TModel> resultHandler, CancellationToken cancellationToken)
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, resultHandler, null, cancellationToken);

            /// <summary>
            /// Query across the specified shards, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, QueryResultModelHandler<TShard, object, TModel> resultHandler, CancellationToken cancellationToken)
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, resultHandler, null, cancellationToken);


            /// <summary>
            /// Query across the specified shards, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public Task<TModel> QueryAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, QueryResultModelHandler<TShard, object, TModel> resultHandler, CancellationToken cancellationToken)
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, resultHandler, null, cancellationToken);

            /// <summary>
            /// Query across the specified shards, returning the first non-null result created by a handler delegate .
            /// </summary>
            /// <typeparam name="TArg">The optional object type to be passed to the handler.</typeparam>
            /// <typeparam name="TModel">The data object return type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="resultHandler">The thread-safe delegate that converts the data results into the return object type.</param>
            /// <param name="dataObject">An object of type TArg to be passed to the resultHandler, which may contain additional data.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non-null object obtained from any shard.</returns>
            public Task<TModel> QueryAsync<TArg, TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg dataObject, CancellationToken cancellationToken)
                => _shardSet.ReadQueryFirstAsync<TArg, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, resultHandler, dataObject, cancellationToken);

            #endregion
            #region MapReaderAsync
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, null, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken) where TModel : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, null, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken) where TModel : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);


            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes to build results from the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns first non-null result using Mapping attributes and the DataReader.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);
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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);
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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);


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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);
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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);


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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>The first non=null object result returned from any shard.</returns>
            public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);
            #endregion
            #region MapOutputAsync
            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

            /// <summary>
            /// Query across all shards in the shard set, using mapping attributes and output parameters to build results.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters.
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

            /// <summary>
            /// Query across the shards identified by collection of shard parameter values, returns the first non-null result created using Mapping attributes and output parameters .
            /// </summary>
            /// <typeparam name="TModel">The data object return type for the list</typeparam>
            /// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, null, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, null, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

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
            /// <param name="sprocName">The name of the stored procedure or function to be invoked on every instance.</param>
            /// <param name="parameters">The parameters to be passed to the procedure or function.</param>
            /// <param name="shardParameterValues">A list of shards to be queried, and shard-specific values to use for named parameters.</param>
            /// <param name="shardParameterName">The name of the ShardId parameter, to be set for each connection before it is called.</param>
            /// <param name="cancellationToken">A token which allows the query to be cancelled.</param>
            /// <returns>A list of the non-null object results returned from any shard.</returns>
            public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(string sprocName, DbParameterCollection parameters, IEnumerable<ShardParameterValue<TShard>> shardParameterValues, string shardParameterName, CancellationToken cancellationToken)
                where TModel : class, new()
                where TReaderResult0 : class, new()
                where TReaderResult1 : class, new()
                where TReaderResult2 : class, new()
                where TReaderResult3 : class, new()
                where TReaderResult4 : class, new()
                where TReaderResult5 : class, new()
                where TReaderResult6 : class, new()
                where TReaderResult7 : class, new()
                => _shardSet.ReadQueryFirstAsync<object, TModel>(sprocName, parameters, shardParameterValues, shardParameterName, Mapper.ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, null, cancellationToken);

            #endregion
        }
    }
}
