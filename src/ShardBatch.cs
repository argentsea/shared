// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Common;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ArgentSea
{
    public class ShardBatch<TShard, TResult> : BatchBase<TShard, TResult> where TShard : IComparable
    {
        internal protected override async Task<TResult> Execute(TShard shardId, DbConnection connection, DbTransaction transaction, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken)
        {
            var result = default(TResult);
            for (var i = 0; i < _processes.Count; i++)
            {
                var process = _processes[i];
                logger.BatchStepStart(i, connectionName);
                var callResult = await process.Execute(shardId, connection, transaction, connectionName, services, logger, cancellationToken);
                logger.BatchStepEnd(i, connectionName);
                if (!EqualityComparer<TResult>.Default.Equals(callResult, default(TResult)))
                {
                    result = callResult;
                }
            }
            return result;
        }

        /// <summary>
        /// Loads an implementation of BatchStep into the collection.
        /// </summary>
        /// <param name="step">A BatchStep object.</param>
        /// <returns>A reference to the collection, for a fluent API.</returns>
        public ShardBatch<TShard, TResult> Add(BatchStep<TShard, TResult> step)
        {
            _processes.Add(step);
            return this;
        }

        /// <summary>
        /// Loads a stp to execute a SQL query. No results are returned.
        /// </summary>
        /// <param name="query">The query to execute at this step.</param>
        /// <returns>A reference to the collection, for a fluent API.</returns>
        public ShardBatch<TShard, TResult> Add(Query query)
        {
            _processes.Add(new ShardBatchQuery(query));
            return this;
        }


        /// <summary>
        /// Add a step to execute a SQL Query. This query does not return a result.
        /// </summary>
        /// <param name="query">The query to add.</param>
        /// <param name="parameters">The parameters for the query.</param>
        /// <returns></returns>
        public ShardBatch<TShard, TResult> Add(Query query, DbParameterCollection parameters)
        {
            _processes.Add(new ShardBatchQuery(query, parameters));
            return this;
        }
        private class ShardBatchQuery : BatchStep<TShard, TResult>
        {
            private readonly DbParameterCollection _parameters;
            private readonly Query _query;
            public ShardBatchQuery(Query query, DbParameterCollection parameters)
            {
                _query = query;
                _parameters = parameters;
            }
            public ShardBatchQuery(Query query)
            {
                _query = query;
                _parameters = new ParameterCollection();
            }

            protected internal override async Task<TResult> Execute(TShard shardId, DbConnection connection, DbTransaction transaction, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var cmd = services.NewCommand(_query.Sql, connection))
                {
                    cmd.CommandType = _query.Type;
                    cmd.Transaction = transaction;
                    services.SetParameters(cmd, _query.ParameterNames, _parameters, null);
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    return default(TResult);
                }
            }

        }
    }
}
