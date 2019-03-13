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
    public class ShardSetBatch<TShard> : BatchBase<TShard, object> where TShard : IComparable
    {

        internal protected override async Task<object> Execute(TShard shardId, DbConnection connection, DbTransaction transaction, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken)
        {
            for (var i = 0; i < _processes.Count; i++)
            {
                var process = _processes[i];
                logger.BatchStepStart(i, connectionName);
                await process.Execute(shardId, connection, transaction, connectionName, services, logger, cancellationToken);
                logger.BatchStepStart(i, connectionName);
            }
            return null;
        }


        /// <summary>
        /// Loads an implementation of BatchStep into the collection.
        /// </summary>
        /// <param name="step">A BatchStep object.</param>
        /// <returns>A reference to the collection, for a fluent API.</returns>
        public ShardSetBatch<TShard> Add(BatchStep<TShard, object> step)
        {
            _processes.Add(step);
            return this;
        }

        /// <summary>
        /// Loads a stp to execute a SQL query. No results are returned.
        /// </summary>
        /// <param name="query">The query to execute at this step.</param>
        /// <returns>A reference to the collection, for a fluent API.</returns>
        public ShardSetBatch<TShard> Add(Query query)
        {
            _processes.Add(new ShardSetBatchQuery(query));
            return this;
        }


        /// <summary>
        /// Add a step to execute a SQL Query. This query does not return a result.
        /// </summary>
        /// <param name="query">The query to add.</param>
        /// <param name="parameters">The parameters for the query.</param>
        /// <returns></returns>
        public ShardSetBatch<TShard> Add(Query query, DbParameterCollection parameters)
        {
            _processes.Add(new ShardSetBatchQuery(query, parameters));
            return this;
        }

        private class ShardSetBatchQuery : BatchStep<TShard, object>
        {
            private readonly DbParameterCollection _parameters;
            private readonly Query _query;
            public ShardSetBatchQuery(Query query, DbParameterCollection parameters)
            {
                _query = query;
                _parameters = parameters;
            }
            public ShardSetBatchQuery(Query query)
            {
                _query = query;
                _parameters = new ParameterCollection();
            }

            protected internal override async Task<object> Execute(TShard shardId, DbConnection connection, DbTransaction transaction, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var cmd = services.NewCommand(_query.Sql, connection))
                {
                    cmd.CommandType = _query.Type;
                    cmd.Transaction = transaction;
                    services.SetParameters(cmd, _query.ParameterNames, _parameters, null);
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    return null;
                }
            }

        }
    }
}
