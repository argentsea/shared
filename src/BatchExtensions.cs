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
    public static class BatchExtensions
    {
        #region Shard Extension Methods

        public static ShardBatch<TShard, ShardKey<TShard, TRecord>> Add<TShard, TRecord>(this ShardBatch<TShard, ShardKey<TShard, TRecord>> batch, Query query, DbParameterCollection parameters, char dataOrigin, string shardIdColumnName, string recordIdColumnName) where TShard : IComparable where TRecord : IComparable
        {
            batch.Add(new KeyStep<TShard, TRecord>(query, parameters, dataOrigin, shardIdColumnName, recordIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, ShardKey<TShard, TRecord>> Add<TShard, TRecord>(this ShardBatch<TShard, ShardKey<TShard, TRecord>> batch, Query query, char dataOrigin, string shardIdColumnName, string recordIdColumnName) where TShard : IComparable where TRecord : IComparable
        {
            batch.Add(new KeyStep<TShard, TRecord>(query, new ParameterCollection(), dataOrigin, shardIdColumnName, recordIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, ShardChild<TShard, TRecord, TChild>> Add<TShard, TRecord, TChild>(this ShardBatch<TShard, ShardChild<TShard, TRecord, TChild>> batch, Query query, DbParameterCollection parameters, char dataOrigin, string shardIdColumnName, string recordIdColumnName, string childIdColumnName) where TShard : IComparable where TRecord : IComparable where TChild : IComparable
        {
            batch.Add(new ChildStep<TShard, TRecord, TChild>(query, parameters, dataOrigin, shardIdColumnName, recordIdColumnName, childIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, ShardChild<TShard, TRecord, TChild>> Add<TShard, TRecord, TChild>(this ShardBatch<TShard, ShardChild<TShard, TRecord, TChild>> batch, Query query, char dataOrigin, string shardIdColumnName, string recordIdColumnName, string childIdColumnName) where TShard : IComparable where TRecord : IComparable where TChild : IComparable
        {
            batch.Add(new ChildStep<TShard, TRecord, TChild>(query, new ParameterCollection(), dataOrigin, shardIdColumnName, recordIdColumnName, childIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, ShardKey<TShard, TRecord>> Add<TShard, TRecord>(this ShardBatch<TShard, ShardKey<TShard, TRecord>> batch, Query query, DbParameterCollection parameters, char dataOrigin, string recordIdColumnName) where TShard : IComparable where TRecord : IComparable
        {
            batch.Add(new KeyStep<TShard, TRecord>(query, parameters, dataOrigin, null, recordIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, ShardKey<TShard, TRecord>> Add<TShard, TRecord>(this ShardBatch<TShard, ShardKey<TShard, TRecord>> batch, Query query, char dataOrigin, string recordIdColumnName) where TShard : IComparable where TRecord : IComparable
        {
            batch.Add(new KeyStep<TShard, TRecord>(query, new ParameterCollection(), dataOrigin, null, recordIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, ShardChild<TShard, TRecord, TChild>> Add<TShard, TRecord, TChild>(this ShardBatch<TShard, ShardChild<TShard, TRecord, TChild>> batch, Query query, DbParameterCollection parameters, char dataOrigin, string recordIdColumnName, string childIdColumnName) where TShard : IComparable where TRecord : IComparable where TChild : IComparable
        {
            batch.Add(new ChildStep<TShard, TRecord, TChild>(query, parameters, dataOrigin, null, recordIdColumnName, childIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, ShardChild<TShard, TRecord, TChild>> Add<TShard, TRecord, TChild>(this ShardBatch<TShard, ShardChild<TShard, TRecord, TChild>> batch, Query query, char dataOrigin, string recordIdColumnName, string childIdColumnName) where TShard : IComparable where TRecord : IComparable where TChild : IComparable
        {
            batch.Add(new ChildStep<TShard, TRecord, TChild>(query, new ParameterCollection(), dataOrigin, null, recordIdColumnName, childIdColumnName));
            return batch;
        }


        public static ShardBatch<TShard, TModel> Add<TShard, TArg, TModel>(this ShardBatch<TShard, TModel> batch, Query query, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg optionalArgument) where TShard : IComparable where TModel : class, new()
        {
            batch.Add(new ModelStep<TShard, TArg, TModel>(query, parameters, resultHandler, optionalArgument));
            return batch;
        }
        public static ShardBatch<TShard, TModel> Add<TShard, TModel>(this ShardBatch<TShard, TModel> batch, Query query, DbParameterCollection parameters, QueryResultModelHandler<TShard, object, TModel> resultHandler) where TShard : IComparable where TModel : class, new()
        {
            batch.Add(new ModelStep<TShard, object, TModel>(query, parameters, resultHandler, null));
            return batch;
        }
        public static ShardBatch<TShard, TModel> Add<TShard, TArg, TModel>(this ShardBatch<TShard, TModel> batch, Query query, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg optionalArgument) where TShard : IComparable where TModel : class, new()
        {
            batch.Add(new ModelStep<TShard, TArg, TModel>(query, new ParameterCollection(), resultHandler, optionalArgument));
            return batch;
        }
        public static ShardBatch<TShard, TModel> Add<TShard, TModel>(this ShardBatch<TShard, TModel> batch, Query query, QueryResultModelHandler<TShard, object, TModel> resultHandler) where TShard : IComparable where TModel : class, new()
        {
            batch.Add(new ModelStep<TShard, object, TModel>(query, new ParameterCollection(), resultHandler, null));
            return batch;
        }

        public static ShardBatch<TShard, IList<ShardKey<TShard, TRecord>>> Add<TShard, TRecord>(this ShardBatch<TShard, IList<ShardKey<TShard, TRecord>>> batch, Query query, DbParameterCollection parameters, char dataOrigin, string shardIdColumnName, string recordIdColumnName) where TShard : IComparable where TRecord : IComparable
        {
            batch.Add(new KeyListStep<TShard, TRecord>(query, parameters, dataOrigin, shardIdColumnName, recordIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, IList<ShardKey<TShard, TRecord>>> Add<TShard, TRecord>(this ShardBatch<TShard, IList<ShardKey<TShard, TRecord>>> batch, Query query, char dataOrigin, string shardIdColumnName, string recordIdColumnName) where TShard : IComparable where TRecord : IComparable
        {
            batch.Add(new KeyListStep<TShard, TRecord>(query, new ParameterCollection(), dataOrigin, shardIdColumnName, recordIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, IList<ShardChild<TShard, TRecord, TChild>>> Add<TShard, TRecord, TChild>(this ShardBatch<TShard, IList<ShardChild<TShard, TRecord, TChild>>> batch, Query query, DbParameterCollection parameters, char dataOrigin, string shardIdColumnName, string recordIdColumnName, string childIdColumnName) where TShard : IComparable where TRecord : IComparable where TChild : IComparable
        {
            batch.Add(new ChildListStep<TShard, TRecord, TChild>(query, parameters, dataOrigin, shardIdColumnName, recordIdColumnName, childIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, IList<ShardChild<TShard, TRecord, TChild>>> Add<TShard, TRecord, TChild>(this ShardBatch<TShard, IList<ShardChild<TShard, TRecord, TChild>>> batch, Query query, char dataOrigin, string shardIdColumnName, string recordIdColumnName, string childIdColumnName) where TShard : IComparable where TRecord : IComparable where TChild : IComparable
        {
            batch.Add(new ChildListStep<TShard, TRecord, TChild>(query, new ParameterCollection(), dataOrigin, shardIdColumnName, recordIdColumnName, childIdColumnName));
            return batch;
        }

        public static ShardBatch<TShard, List<ShardKey<TShard, TRecord>>> Add<TShard, TRecord>(this ShardBatch<TShard, List<ShardKey<TShard, TRecord>>> batch, Query query, DbParameterCollection parameters, char dataOrigin, string shardIdColumnName, string recordIdColumnName) where TShard : IComparable where TRecord : IComparable
        {
            ((IList)batch).Add(new KeyListStep<TShard, TRecord>(query, parameters, dataOrigin, shardIdColumnName, recordIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, List<ShardKey<TShard, TRecord>>> Add<TShard, TRecord>(this ShardBatch<TShard, List<ShardKey<TShard, TRecord>>> batch, Query query, char dataOrigin, string shardIdColumnName, string recordIdColumnName) where TShard : IComparable where TRecord : IComparable
        {
            ((IList)batch).Add(new KeyListStep<TShard, TRecord>(query, new ParameterCollection(), dataOrigin, shardIdColumnName, recordIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, List<ShardChild<TShard, TRecord, TChild>>> Add<TShard, TRecord, TChild>(this ShardBatch<TShard, List<ShardChild<TShard, TRecord, TChild>>> batch, Query query, DbParameterCollection parameters, char dataOrigin, string shardIdColumnName, string recordIdColumnName, string childIdColumnName) where TShard : IComparable where TRecord : IComparable where TChild : IComparable
        {
            ((IList)batch).Add(new ChildListStep<TShard, TRecord, TChild>(query, parameters, dataOrigin, shardIdColumnName, recordIdColumnName, childIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, List<ShardChild<TShard, TRecord, TChild>>> Add<TShard, TRecord, TChild>(this ShardBatch<TShard, List<ShardChild<TShard, TRecord, TChild>>> batch, Query query, char dataOrigin, string shardIdColumnName, string recordIdColumnName, string childIdColumnName) where TShard : IComparable where TRecord : IComparable where TChild : IComparable
        {
            ((IList)batch).Add(new ChildListStep<TShard, TRecord, TChild>(query, new ParameterCollection(), dataOrigin, shardIdColumnName, recordIdColumnName, childIdColumnName));
            return batch;
        }


        public static ShardBatch<TShard, IList<ShardKey<TShard, TRecord>>> Add<TShard, TRecord>(this ShardBatch<TShard, IList<ShardKey<TShard, TRecord>>> batch, Query query, DbParameterCollection parameters, char dataOrigin, string recordIdColumnName) where TShard : IComparable where TRecord : IComparable
        {
            batch.Add(new KeyListStep<TShard, TRecord>(query, parameters, dataOrigin, null, recordIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, IList<ShardKey<TShard, TRecord>>> Add<TShard, TRecord>(this ShardBatch<TShard, IList<ShardKey<TShard, TRecord>>> batch, Query query, char dataOrigin, string recordIdColumnName) where TShard : IComparable where TRecord : IComparable
        {
            batch.Add(new KeyListStep<TShard, TRecord>(query, new ParameterCollection(), dataOrigin, null, recordIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, IList<ShardChild<TShard, TRecord, TChild>>> Add<TShard, TRecord, TChild>(this ShardBatch<TShard, IList<ShardChild<TShard, TRecord, TChild>>> batch, Query query, DbParameterCollection parameters, char dataOrigin, string recordIdColumnName, string childIdColumnName) where TShard : IComparable where TRecord : IComparable where TChild : IComparable
        {
            batch.Add(new ChildListStep<TShard, TRecord, TChild>(query, parameters, dataOrigin, null, recordIdColumnName, childIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, IList<ShardChild<TShard, TRecord, TChild>>> Add<TShard, TRecord, TChild>(this ShardBatch<TShard, IList<ShardChild<TShard, TRecord, TChild>>> batch, Query query, char dataOrigin, string recordIdColumnName, string childIdColumnName) where TShard : IComparable where TRecord : IComparable where TChild : IComparable
        {
            batch.Add(new ChildListStep<TShard, TRecord, TChild>(query, new ParameterCollection(), dataOrigin, null, recordIdColumnName, childIdColumnName));
            return batch;
        }

        public static ShardBatch<TShard, List<ShardKey<TShard, TRecord>>> Add<TShard, TRecord>(this ShardBatch<TShard, List<ShardKey<TShard, TRecord>>> batch, Query query, DbParameterCollection parameters, char dataOrigin, string recordIdColumnName) where TShard : IComparable where TRecord : IComparable
        {
            ((IList)batch).Add(new KeyListStep<TShard, TRecord>(query, parameters, dataOrigin, null, recordIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, List<ShardKey<TShard, TRecord>>> Add<TShard, TRecord>(this ShardBatch<TShard, List<ShardKey<TShard, TRecord>>> batch, Query query, char dataOrigin, string recordIdColumnName) where TShard : IComparable where TRecord : IComparable
        {
            ((IList)batch).Add(new KeyListStep<TShard, TRecord>(query, new ParameterCollection(), dataOrigin, null, recordIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, List<ShardChild<TShard, TRecord, TChild>>> Add<TShard, TRecord, TChild>(this ShardBatch<TShard, List<ShardChild<TShard, TRecord, TChild>>> batch, Query query, DbParameterCollection parameters, char dataOrigin, string recordIdColumnName, string childIdColumnName) where TShard : IComparable where TRecord : IComparable where TChild : IComparable
        {
            ((IList)batch).Add(new ChildListStep<TShard, TRecord, TChild>(query, parameters, dataOrigin, null, recordIdColumnName, childIdColumnName));
            return batch;
        }
        public static ShardBatch<TShard, List<ShardChild<TShard, TRecord, TChild>>> Add<TShard, TRecord, TChild>(this ShardBatch<TShard, List<ShardChild<TShard, TRecord, TChild>>> batch, Query query, char dataOrigin, string recordIdColumnName, string childIdColumnName) where TShard : IComparable where TRecord : IComparable where TChild : IComparable
        {
            ((IList)batch).Add(new ChildListStep<TShard, TRecord, TChild>(query, new ParameterCollection(), dataOrigin, null, recordIdColumnName, childIdColumnName));
            return batch;
        }

        #endregion
        #region Database Extension Methods
        public static DatabaseBatch<TModel> Add<TArg, TModel>(this DatabaseBatch<TModel> batch, Query query, DbParameterCollection parameters, QueryResultModelHandler<int, TArg, TModel> resultHandler, TArg optionalArgument) where TModel : class, new()
        {
            batch.Add(new ModelStep<int, TArg, TModel>(query, parameters, resultHandler, optionalArgument));
            return batch;
        }
        public static DatabaseBatch<TModel> Add<TModel>(this DatabaseBatch<TModel> batch, Query query, DbParameterCollection parameters, QueryResultModelHandler<int, object, TModel> resultHandler) where TModel : class, new()
        {
            batch.Add(new ModelStep<int, object, TModel>(query, parameters, resultHandler, null));
            return batch;
        }
        public static DatabaseBatch<TModel> Add<TArg, TModel>(this DatabaseBatch<TModel> batch, Query query, QueryResultModelHandler<int, TArg, TModel> resultHandler, TArg optionalArgument) where TModel : class, new()
        {
            batch.Add(new ModelStep<int, TArg, TModel>(query, new ParameterCollection(), resultHandler, optionalArgument));
            return batch;
        }
        public static DatabaseBatch<TModel> Add<TModel>(this DatabaseBatch<TModel> batch, Query query, QueryResultModelHandler<int, object, TModel> resultHandler) where TModel : class, new()
        {
            batch.Add(new ModelStep<int, object, TModel>(query, new ParameterCollection(), resultHandler, null));
            return batch;
        }


        public static DatabaseBatch<TRecord> Add<TRecord>(this DatabaseBatch<TRecord> batch, Query query, DbParameterCollection parameters, string recordIdColumnName) where TRecord : IComparable
        {
            batch.Add(new RecordStep<TRecord>(query, parameters, recordIdColumnName));
            return batch;
        }
        public static DatabaseBatch<TRecord> Add<TRecord>(this DatabaseBatch<TRecord> batch, Query query, string recordIdColumnName) where TRecord : IComparable
        {
            batch.Add(new RecordStep<TRecord>(query, new ParameterCollection(), recordIdColumnName));
            return batch;
        }
        #endregion
        #region ShardSet Extension Methods
        #endregion
        #region Private classes
        private class KeyStep<TShard, TRecord> : BatchStep<TShard, ShardKey<TShard, TRecord>> where TShard : IComparable where TRecord : IComparable
        {
            private readonly string _shardIdColumnName;
            private readonly string _recordIdColumnName;
            private readonly Query _query;
            private char _dataOrigin;
            private readonly DbParameterCollection _parameters;

            public KeyStep(Query query, DbParameterCollection parameters, char dataOrigin, string shardIdColumnName, string recordIdColumnName)
            {
                _query = query;
                _parameters = parameters;
                _dataOrigin = dataOrigin;
                _shardIdColumnName = shardIdColumnName;
                _recordIdColumnName = recordIdColumnName;
            }
            protected internal override async Task<ShardKey<TShard, TRecord>> Execute(TShard shardId, DbConnection connection, DbTransaction transaction, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var cmd = services.NewCommand(_query.Sql, connection))
                {
                    cmd.CommandType = _query.Type;
                    cmd.Transaction = transaction;
                    services.SetParameters(cmd, _query.ParameterNames, _parameters, null);
                    using (var dataReader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var shardOrd = -1;
                        TShard shardIdData = shardId;
                        if (!(_shardIdColumnName is null))
                        {
                            shardOrd = dataReader.GetOrdinal(_shardIdColumnName);
                        }
                        var recordOrd = dataReader.GetOrdinal(_recordIdColumnName);
                        if (dataReader.Read())
                        {
                            if (shardOrd != -1)
                            {
                                shardIdData = dataReader.GetFieldValue<TShard>(shardOrd);
                            }
                            var recordid = dataReader.GetFieldValue<TRecord>(recordOrd);
                            return new ShardKey<TShard, TRecord>(_dataOrigin, shardIdData, recordid);
                        }
                    }
                }
                return ShardKey<TShard, TRecord>.Empty;
            }
        }
        private class ChildStep<TShard, TRecord, TChildId> : BatchStep<TShard, ShardChild<TShard, TRecord, TChildId>> where TShard : IComparable where TRecord : IComparable where TChildId : IComparable
        {
            private readonly string _shardIdColumnName;
            private readonly string _recordIdColumnName;
            private readonly string _childIdColumnName;
            private readonly Query _query;
            private char _dataOrigin;
            private readonly DbParameterCollection _parameters;

            public ChildStep(Query query, DbParameterCollection parameters, char dataOrigin, string shardIdColumnName, string recordIdColumnName, string childIdColumnName)
            {
                _query = query;
                _parameters = parameters;
                _dataOrigin = dataOrigin;
                _shardIdColumnName = shardIdColumnName;
                _recordIdColumnName = recordIdColumnName;
                _childIdColumnName = childIdColumnName;
            }

            protected internal override async Task<ShardChild<TShard, TRecord, TChildId>> Execute(TShard shardId, DbConnection connection, DbTransaction transaction, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var cmd = services.NewCommand(_query.Sql, connection))
                {
                    cmd.CommandType = _query.Type;
                    cmd.Transaction = transaction;
                    services.SetParameters(cmd, _query.ParameterNames, _parameters, null);
                    using (var dataReader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var shardOrd = -1;
                        TShard shardIdData = shardId;
                        if (!(_shardIdColumnName is null))
                        {
                            shardOrd = dataReader.GetOrdinal(_shardIdColumnName);
                        }
                        var recordOrd = dataReader.GetOrdinal(_recordIdColumnName);
                        var childOrd = dataReader.GetOrdinal(_childIdColumnName);
                        if (dataReader.Read())
                        {
                            if (shardOrd != -1)
                            {
                                shardIdData = dataReader.GetFieldValue<TShard>(shardOrd);
                            }
                            var recordid = dataReader.GetFieldValue<TRecord>(recordOrd);
                            var childid = dataReader.GetFieldValue<TChildId>(childOrd);
                            return new ShardChild<TShard, TRecord, TChildId>(_dataOrigin, shardIdData, recordid, childid);
                        }
                    }
                }
                return ShardChild<TShard, TRecord, TChildId>.Empty;
            }
        }
        private class ModelStep<TShard, TArg, TModel> : BatchStep<TShard, TModel> where TShard : IComparable
        {
            private bool _isTopOne;
            private readonly Query _query;
            private readonly QueryResultModelHandler<TShard, TArg, TModel> _resultHandler;
            private readonly DbParameterCollection _parameters;
            private readonly TArg _optionalArgument;
            public ModelStep(Query query, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument)
            {
                _isTopOne = isTopOne;
                _query = query;
                _resultHandler = resultHandler;
                _parameters = parameters;
                _optionalArgument = optionalArgument;
            }
            public ModelStep(Query query, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TModel> resultHandler)
            {
                _isTopOne = false;
                _query = query;
                _resultHandler = resultHandler;
                _parameters = parameters;
                _optionalArgument = default(TArg);
            }
            public ModelStep(Query query, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, bool isTopOne)
            {
                _isTopOne = isTopOne;
                _query = query;
                _resultHandler = resultHandler;
                _parameters = parameters;
                _optionalArgument = default(TArg);
            }
            public ModelStep(Query query, DbParameterCollection parameters, QueryResultModelHandler<TShard, TArg, TModel> resultHandler, TArg optionalArgument)
            {
                _isTopOne = false;
                _query = query;
                _resultHandler = resultHandler;
                _parameters = parameters;
                _optionalArgument = optionalArgument;
            }

            protected internal override async Task<TModel> Execute(TShard shardId, DbConnection connection, DbTransaction transaction, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken)
            {
                var result = default(TModel);
                cancellationToken.ThrowIfCancellationRequested();
                using (var cmd = services.NewCommand(_query.Sql, connection))
                {
                    cmd.CommandType = _query.Type;
                    cmd.Transaction = transaction;
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

        private class KeyListStep<TShard, TRecord> : BatchStep<TShard, IList<ShardKey<TShard, TRecord>>> where TShard : IComparable where TRecord : IComparable
        {
            private readonly string _shardIdColumnName;
            private readonly string _recordIdColumnName;
            private readonly Query _query;
            private char _dataOrigin;
            private readonly DbParameterCollection _parameters;

            public KeyListStep(Query query, DbParameterCollection parameters, char dataOrigin, string shardIdColumnName, string recordIdColumnName)
            {
                _query = query;
                _parameters = parameters;
                _dataOrigin = dataOrigin;
                _shardIdColumnName = shardIdColumnName;
                _recordIdColumnName = recordIdColumnName;
            }
            protected internal override async Task<IList<ShardKey<TShard, TRecord>>> Execute(TShard shardId, DbConnection connection, DbTransaction transaction, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken)
            {
                var result = new List<ShardKey<TShard, TRecord>>();
                cancellationToken.ThrowIfCancellationRequested();
                using (var cmd = services.NewCommand(_query.Sql, connection))
                {
                    cmd.CommandType = _query.Type;
                    cmd.Transaction = transaction;
                    services.SetParameters(cmd, _query.ParameterNames, _parameters, null);
                    using (var dataReader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, cancellationToken).ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var shardOrd = -1;
                        var shardIdData = shardId;
                        if (!(_shardIdColumnName is null))
                        {
                            shardOrd = dataReader.GetOrdinal(_shardIdColumnName);
                        }
                        var recordOrd = dataReader.GetOrdinal(_recordIdColumnName);
                        while (dataReader.Read())
                        {
                            if (shardOrd != -1)
                            {
                                shardIdData = dataReader.GetFieldValue<TShard>(shardOrd);
                            }
                            var recordid = dataReader.GetFieldValue<TRecord>(recordOrd);
                            result.Add(new ShardKey<TShard, TRecord>(_dataOrigin, shardIdData, recordid));
                        }
                    }
                }
                return result;
            }
        }

        private class ChildListStep<TShard, TRecord, TChild> : BatchStep<TShard, IList<ShardChild<TShard, TRecord, TChild>>> where TShard : IComparable where TRecord : IComparable where TChild : IComparable
        {
            private readonly string _shardIdColumnName;
            private readonly string _recordIdColumnName;
            private readonly string _childIdColumnName;
            private readonly Query _query;
            private char _dataOrigin;
            private readonly DbParameterCollection _parameters;

            public ChildListStep(Query query, DbParameterCollection parameters, char dataOrigin, string shardIdColumnName, string recordIdColumnName, string childIdColumnName)
            {
                _query = query;
                _parameters = parameters;
                _dataOrigin = dataOrigin;
                _shardIdColumnName = shardIdColumnName;
                _recordIdColumnName = recordIdColumnName;
                _childIdColumnName = childIdColumnName;
            }

            protected internal override async Task<IList<ShardChild<TShard, TRecord, TChild>>> Execute(TShard shardId, DbConnection connection, DbTransaction transaction, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken)
            {
                var result = new List<ShardChild<TShard, TRecord, TChild>>();
                cancellationToken.ThrowIfCancellationRequested();
                using (var cmd = services.NewCommand(_query.Sql, connection))
                {
                    cmd.CommandType = _query.Type;
                    cmd.Transaction = transaction;
                    services.SetParameters(cmd, _query.ParameterNames, _parameters, null);
                    using (var dataReader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.Default, cancellationToken).ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var shardOrd = -1;
                        var shardIdData = shardId;
                        if (!(_shardIdColumnName is null))
                        {
                            shardOrd = dataReader.GetOrdinal(_shardIdColumnName);
                        }
                        var recordOrd = dataReader.GetOrdinal(_recordIdColumnName);
                        var childOrd = dataReader.GetOrdinal(_childIdColumnName);
                        while (dataReader.Read())
                        {
                            if (shardOrd != -1)
                            {
                                shardIdData = dataReader.GetFieldValue<TShard>(shardOrd);
                            }
                            var recordid = dataReader.GetFieldValue<TRecord>(recordOrd);
                            var childid = dataReader.GetFieldValue<TChild>(childOrd);
                            result.Add(new ShardChild<TShard, TRecord, TChild>(_dataOrigin, shardIdData, recordid, childid));
                        }
                    }
                }
                return result;
            }
        }

        private class RecordStep<TRecord> : BatchStep<int, TRecord> where TRecord : IComparable
        {
            private readonly string _recordIdColumnName;
            private readonly Query _query;
            private readonly DbParameterCollection _parameters;

            public RecordStep(Query query, DbParameterCollection parameters, string recordIdColumnName)
            {
                _query = query;
                _parameters = parameters;
                _recordIdColumnName = recordIdColumnName;
            }

            protected internal override async Task<TRecord> Execute(int shardId, DbConnection connection, DbTransaction transaction, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var cmd = services.NewCommand(_query.Sql, connection))
                {
                    cmd.CommandType = _query.Type;
                    cmd.Transaction = transaction;
                    services.SetParameters(cmd, _query.ParameterNames, _parameters, null);
                    using (var dataReader = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var recordOrd = dataReader.GetOrdinal(_recordIdColumnName);
                        if (dataReader.Read())
                        {
                            var recordid = dataReader.GetFieldValue<TRecord>(recordOrd);
                            return recordid;
                        }
                    }
                }
                return default(TRecord);
            }
        }
        #endregion
    }
}