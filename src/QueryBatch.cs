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
    public class QueryBatch<TShard, TResult> : ICollection, IEnumerable<QueryBatch<TShard, TResult>.QueryBatchStep> where TResult : class, new() where TShard : IComparable
    {
        private readonly object syncRoot = new Lazy<object>();
        private readonly List<QueryBatchStep> _processes = new List<QueryBatchStep>();

        public bool IsSynchronized => true;

        public object SyncRoot => syncRoot;

        public QueryBatchStep this[int index] => _processes[index];

        public void CopyTo(Array array, int index)
            => _processes.CopyTo((QueryBatchStep[])array, index);

        public int Count => _processes.Count;

        public async Task<TResult> Execute(TShard shardId, DbConnection connection, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken)
        {
            var result = default(TResult);
            foreach (var process in _processes)
            {
                var callResult = await process.Execute(shardId, connection, connectionName, services, logger, cancellationToken);
                if (callResult != default(TResult))
                {
                    result = callResult;
                }
            }
            return result;
        }

        public QueryBatch<TShard, TResult> Add(QueryBatchStep process)
        {
            _processes.Add(process);
            return this;
        }
        public QueryBatch<TShard, TResult> Add(Query query)
        {
            _processes.Add(new QueryBatchQuery(query));
            return this;
        }
        public IEnumerator GetEnumerator() => _processes.GetEnumerator();

        IEnumerator<QueryBatchStep> IEnumerable<QueryBatchStep>.GetEnumerator() => _processes.GetEnumerator();

        public abstract class QueryBatchStep
        {
            protected internal abstract Task<TResult> Execute(TShard shardId, DbConnection connection, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken);
        }

        private class QueryBatchQuery : QueryBatchStep
        {
            private readonly DbParameterCollection _parameters;
            private readonly Query _query;
            public QueryBatchQuery(Query query, DbParameterCollection parameters)
            {
                _query = query;
                _parameters = parameters;
            }
            public QueryBatchQuery(Query query)
            {
                _query = query;
                _parameters = new ParameterCollection();
            }

            protected internal override async Task<TResult> Execute(TShard shardId, DbConnection connection, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var cmd = services.NewCommand(_query.Sql, connection))
                {
                    cmd.CommandType = _query.Type;
                    services.SetParameters(cmd, _query.ParameterNames, _parameters, null);
                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    return default(TResult);
                }
            }
        }
        private class QueryBatchStepHandler<TArg> : QueryBatchStep
        {
            private bool _isTopOne;
            private readonly Query _query;
            private readonly QueryResultModelHandler<TShard, TArg, TResult> _resultHandler;
            private readonly DbParameterCollection _parameters;
            private readonly TArg _optionalArgument;
            //		private async Task<TResult> ExecuteQueryWithDelegateAsync<TResult, TArg>(Query query, DbParameterCollection parameters, Dictionary<string, object> parameterValues, TShard shardId, QueryResultModelHandler<TShard, TArg, TResult> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken)
            public QueryBatchStepHandler(Query query, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TResult> resultHandler, bool isTopOne, TArg optionalArgument)
            {
                _isTopOne = isTopOne;
                _query = query;
                _resultHandler = resultHandler;
                _parameters = parameters;
                _optionalArgument = optionalArgument;
            }
            public QueryBatchStepHandler(Query query, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TResult> resultHandler)
            {
                _isTopOne = false;
                _query = query;
                _resultHandler = resultHandler;
                _parameters = parameters;
                _optionalArgument = default(TArg);
            }
            public QueryBatchStepHandler(Query query, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TResult> resultHandler, bool isTopOne)
            {
                _isTopOne = isTopOne;
                _query = query;
                _resultHandler = resultHandler;
                _parameters = parameters;
                _optionalArgument = default(TArg);
            }
            public QueryBatchStepHandler(Query query, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TResult> resultHandler, TArg optionalArgument)
            {
                _isTopOne = false;
                _query = query;
                _resultHandler = resultHandler;
                _parameters = parameters;
                _optionalArgument = optionalArgument;
            }

            protected internal override async Task<TResult> Execute(TShard shardId, DbConnection connection, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken)
            {
                var result = default(TResult);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();
                using (var cmd = services.NewCommand(_query.Sql, connection))
                {
                    cmd.CommandType = _query.Type;
                    services.SetParameters(cmd, _query.ParameterNames, _parameters, null);
                    var cmdType = System.Data.CommandBehavior.Default;
                    if (_isTopOne)
                    {
                        cmdType = System.Data.CommandBehavior.SingleRow;
                    }
                    using (var dataReader = await cmd.ExecuteReaderAsync(cmdType, cancellationToken).ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        result = _resultHandler(shardId, _query.Sql, _optionalArgument, dataReader, cmd.Parameters, connectionName, logger);
                    }
                }
                return result;

            }
        }

    }
}