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
    /// <summary>
    /// This class represents an actual connection to the database. There may be two connections per shard.
    /// </summary>
    public class ShardDataConnection<TConfiguration> where TConfiguration : class, IShardSetsConfigurationOptions, new()
    {
        internal readonly DataConnectionManager _manager;
        private readonly short _shardId;
        private Dictionary<string, object> _mockResult = null;

        internal ShardDataConnection(ShardSetsBase<TConfiguration> parent, short shardId, IDataConnection config)
        {
            _manager = new DataConnectionManager(shardId,
                parent._dataProviderServices, config,
                $"shard number { shardId.ToString() } on connection { config.ConnectionDescription }",
                parent._logger);
            _shardId = shardId;
        }

        public string ConnectionString { get => _manager.ConnectionString; }

        /// <summary>
        /// Set this property to prevent database execution and return the provided result instead.
        /// The dictionary key should match the Query object name.
        /// Methods returning a result will return he corresponding result instead of the datbase result. Note that the return types must match or an error will be thrown.
        /// A Shard batch can specify a empty string as the key “query” name to provide a mock batch result and avoid the batch database execution.
        /// </summary>
        public Dictionary<string, object> MockResults
        {
            get
            {
                if (this._mockResult is null)
                {
                    this._mockResult = new Dictionary<string, object>();
                }
                return this._mockResult;
            }
        }

        #region Public data fetch methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns></returns>
        public Task<int> ReturnValueAsync(Query query, CancellationToken cancellationToken)
            => _manager.ReturnAsync(query, new ParameterCollection(), -1, this.MockResults, cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="parameters">The query parameters. If this does not include a return parameter, one will be added.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns></returns>
        public Task<int> ReturnValueAsync(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            => _manager.ReturnAsync(query, parameters, -1, this.MockResults, cancellationToken);

        /// <summary>
        /// Invokes the query and returns the integer result.
        /// </summary>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="parameters">The query parameters. If this does not include a return parameter, one will be added.</param>
        /// <param name="shardParameterName"></param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns></returns>
        public Task<int> ReturnValueAsync(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            => _manager.ReturnAsync(query, parameters, parameters.GetParameterOrdinal(shardParameterName), this.MockResults, cancellationToken);


        /// <summary>
        /// Invokes the query and returns the value of the output parameter or first-row column value whose name matches the “dataName”.
        /// </summary>
        /// <typeparam name="TValue">The type of the return value, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="dataName">A value that should match an output parameter name or column name. This value will be used for the result.</param>
        /// <param name="parameters">The query parameters. If “dataName” argument matches an output parameter name, this will be the value returned.</param>
        /// <param name="shardParameterName">The name of the parameter who value should be set to the shard Id.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns></returns>
        public async Task<TValue> ReturnValueAsync<TValue>(Query query, string dataName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (TValue)this.MockResults[query.Name];
            }
            var result = await _manager.ReturnAsync<TValue, object, object>(query, dataName, null, null, parameters, null, parameters.GetParameterOrdinal(shardParameterName), cancellationToken);
            return result.Item1;

        }

        /// <summary>
        /// Invokes the query and returns the value of the output parameter or first-row column value whose name matches the “dataName”.
        /// </summary>
        /// <typeparam name="TValue">The type of the return value, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="dataName">A value that should match an output parameter name or column name. This value will be used for the result.</param>
        /// <param name="parameters">The query parameters. If “dataName” argument matches an output parameter name, this will be the value returned.</param>
        /// <param name="shardParameterName">The name of the parameter who value should be set to the shard Id.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns></returns>
        public async Task<TValue> ReturnValueAsync<TValue>(Query query, string dataName, DbParameterCollection parameters, CancellationToken cancellationToken)
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (TValue)this.MockResults[query.Name];
            }
            var result = await _manager.ReturnAsync<TValue, object, object>(query, dataName, null, null, parameters, null, -1, cancellationToken);
            return result.Item1;
        }

        /// <summary>
        /// Invokes the query and returns a ShardKey whose ShardId is the current shard and RecordId is obtained from the output parameter or first-row column value whose name matches the “recordDataName”.
        /// </summary>
        /// <typeparam name="TRecord">The type of the recordId in the ShardKey</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="recordDataName">A value that should match an output parameter name or column name. This value will be used for the RecordId of the ShardKey.</param>
        /// <param name="parameters">The query parameters. If “recordDataName” argument matches an output parameter name, that value will be used for the ShardKey.</param>
        /// <param name="shardParameterName">The name of the parameter who value should be set to the shard Id.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns></returns>
        public async Task<ShardKey<TRecord>> ReturnValueAsync<TRecord>(Query query, char origin, string recordDataName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TRecord : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (ShardKey<TRecord>)this.MockResults[query.Name];
            }
            return new ShardKey<TRecord>(origin, _shardId, (await _manager.ReturnAsync<TRecord, object, object>(query, recordDataName, null, null, parameters, null, parameters.GetParameterOrdinal(shardParameterName), cancellationToken)).Item1);
        }

        /// <summary>
        /// Invokes the query and returns a ShardKey whose ShardId and RecordId are obtained from the output parameters or first-row column values whose name matches “shardDataName” and “recordDataName” respectively.
        /// </summary>
        /// <typeparam name="TRecord">The type of the recordId in the ShardKey.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="shardDataName">A value that should match an output parameter name or column name. This value will be used for the ShardId of the ShardKey.</param>
        /// <param name="recordDataName">A value that should match an output parameter name or column name. This value will be used for the RecordId of the ShardKey.</param>
        /// <param name="parameters">The query parameters. If “shardDataName” and/or “recordDataName” argument matches an output parameter name, those values will be used for the ShardKey.</param>
        /// <param name="shardParameterName">The name of the parameter whose value should be set to the shard Id.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns></returns>
        public async Task<ShardKey<TRecord>> ReturnValueAsync<TRecord>(Query query, char origin, string shardDataName, string recordDataName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TRecord : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (ShardKey<TRecord>)this.MockResults[query.Name];
            }
            var result = await _manager.ReturnAsync<short, TRecord, object>(query, shardDataName, recordDataName, null, parameters, null, parameters.GetParameterOrdinal(shardParameterName), cancellationToken);
            return new ShardKey<TRecord>(origin, result.Item1, result.Item2);
        }

        /// <summary>
        /// Invokes the query and returns a ShardKey whose ShardId is the current shard and RecordId and ChildId is obtained from the output parameter or first-row column value whose name matches the “recordDataName” and “childDataName”.
        /// </summary>
        /// <typeparam name="TRecord">The type of the recordId in the ShardKey.</typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="recordDataName">A value that should match an output parameter name or column name. This value will be used for the RecordId of the ShardKey.</param>
        /// <param name="parameters">The query parameters. If “recordDataName” and/or “childDataName” argument matches an output parameter name, those values will be used for the ShardKey.</param>
        /// <param name="shardParameterName">The name of the parameter whose value should be set to the shard Id.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns></returns>
        public async Task<ShardKey<TRecord, TChild>> ReturnValueAsync<TRecord, TChild>(Query query, char origin, string recordDataName, string childDataName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TRecord : IComparable
            where TChild : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (ShardKey<TRecord, TChild>)this.MockResults[query.Name];
            }
            var result = await _manager.ReturnAsync<TRecord, TChild, object>(query, recordDataName, childDataName, null, parameters, null, parameters.GetParameterOrdinal(shardParameterName), cancellationToken);
            return new ShardKey<TRecord, TChild>(origin, _shardId, result.Item1, result.Item2);
        }

        /// <summary>
        /// Invokes the query and returns a ShardKey whose ShardId, RecordId, and ChildId are obtained from the output parameters or first-row column values whose name matches “shardDataName”, “recordDataName”, and “childDataName” respectively.
        /// </summary>
        /// <typeparam name="TRecord">The type of the recordId in the ShardKey.</typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="shardDataName">A value that should match an output parameter name or column name. This value will be used for the ShardId of the ShardKey.</param>
        /// <param name="recordDataName">A value that should match an output parameter name or column name. This value will be used for the RecordId of the ShardKey.</param>
        /// <param name="childDataName">A value that should match an output parameter name or column name. This value will be used for the ChildId of the ShardKey.</param>
        /// <param name="parameters">The query parameters. If data name arguments match an output parameter name, those values will be used for the ShardKey.</param>
        /// <param name="shardParameterName">The name of the parameter whose value should be set to the shard Id.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns></returns>
        public async Task<ShardKey<TRecord, TChild>> ReturnValueAsync<TRecord, TChild>(Query query, char origin, string shardDataName, string recordDataName, string childDataName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TRecord : IComparable
            where TChild : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (ShardKey<TRecord, TChild>)this.MockResults[query.Name];
            }
            var result = await _manager.ReturnAsync<short, TRecord, TChild>(query, shardDataName, recordDataName, childDataName, parameters, null, parameters.GetParameterOrdinal(shardParameterName), cancellationToken);
            return new ShardKey<TRecord, TChild>(origin, result.Item1, result.Item2, result.Item3);
        }

        /// <summary>
        /// Invokes the query and returns a ShardKey whose ShardId is the current shard and RecordId is obtained from the output parameter or first-row column value whose name matches the “recordDataName”.
        /// </summary>
        /// <typeparam name="TRecord">The type of the recordId in the ShardKey</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="recordDataName">A value that should match an output parameter name or column name. This value will be used for the RecordId of the ShardKey.</param>
        /// <param name="parameters">The query parameters. If “recordDataName” argument matches an output parameter name, that value will be used for the ShardKey.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns></returns>
        public async Task<ShardKey<TRecord>> ReturnValueAsync<TRecord>(Query query, char origin, string recordDataName, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TRecord : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (ShardKey<TRecord>)this.MockResults[query.Name];
            }
            return new ShardKey<TRecord>(origin, _shardId, (await _manager.ReturnAsync<TRecord, object, object>(query, recordDataName, null, null, parameters, null, -1, cancellationToken)).Item1);
        }

        /// <summary>
        /// Invokes the query and returns a ShardKey whose ShardId and RecordId are obtained from the output parameters or first-row column values whose name matches “shardDataName” and “recordDataName” respectively.
        /// </summary>
        /// <typeparam name="TRecord">The type of the recordId in the ShardKey.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="shardDataName">A value that should match an output parameter name or column name. This value will be used for the ShardId of the ShardKey.</param>
        /// <param name="recordDataName">A value that should match an output parameter name or column name. This value will be used for the RecordId of the ShardKey.</param>
        /// <param name="parameters">The query parameters. If “shardDataName” and/or “recordDataName” argument matches an output parameter name, those values will be used for the ShardKey.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns></returns>
        public async Task<ShardKey<TRecord>> ReturnValueAsync<TRecord>(Query query, char origin, string shardDataName, string recordDataName, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TRecord : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (ShardKey<TRecord>)this.MockResults[query.Name];
            }
            var result = await _manager.ReturnAsync<short, TRecord, object>(query, shardDataName, recordDataName, null, parameters, null, -1, cancellationToken);
            return new ShardKey<TRecord>(origin, result.Item1, result.Item2);
        }

        /// <summary>
        /// Invokes the query and returns a ShardKey whose ShardId is the current shard and RecordId and ChildId is obtained from the output parameter or first-row column value whose name matches the “recordDataName” and “childDataName”.
        /// </summary>
        /// <typeparam name="TRecord">The type of the recordId in the ShardKey.</typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="recordDataName">A value that should match an output parameter name or column name. This value will be used for the RecordId of the ShardKey.</param>
        /// <param name="parameters">The query parameters. If “recordDataName” and/or “childDataName” argument matches an output parameter name, those values will be used for the ShardKey.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns></returns>
        public async Task<ShardKey<TRecord, TChild>> ReturnValueAsync<TRecord, TChild>(Query query, char origin, string recordDataName, string childDataName, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TRecord : IComparable
            where TChild : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (ShardKey<TRecord, TChild>)this.MockResults[query.Name];
            }
            var result = await _manager.ReturnAsync<TRecord, TChild, object>(query, recordDataName, childDataName, null, parameters, null, -1, cancellationToken);
            return new ShardKey<TRecord, TChild>(origin, _shardId, result.Item1, result.Item2);
        }

        /// <summary>
        /// Invokes the query and returns a ShardKey whose ShardId, RecordId, and ChildId are obtained from the output parameters or first-row column values whose name matches “shardDataName”, “recordDataName”, and “childDataName” respectively.
        /// </summary>
        /// <typeparam name="TRecord">The type of the recordId in the ShardKey.</typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="shardDataName">A value that should match an output parameter name or column name. This value will be used for the ShardId of the ShardKey.</param>
        /// <param name="recordDataName">A value that should match an output parameter name or column name. This value will be used for the RecordId of the ShardKey.</param>
        /// <param name="childDataName">A value that should match an output parameter name or column name. This value will be used for the ChildId of the ShardKey.</param>
        /// <param name="parameters">The query parameters. If data name arguments match an output parameter name, those values will be used for the ShardKey.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns></returns>
        public async Task<ShardKey<TRecord, TChild>> ReturnValueAsync<TRecord, TChild>(Query query, char origin, string shardDataName, string recordDataName, string childDataName, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TRecord : IComparable
            where TChild : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (ShardKey<TRecord, TChild>)this.MockResults[query.Name];
            }
            var result = await _manager.ReturnAsync<short, TRecord, TChild>(query, shardDataName, recordDataName, childDataName, parameters, null, -1, cancellationToken);
            return new ShardKey<TRecord, TChild>(origin, result.Item1, result.Item2, result.Item3);
        }


        /// <summary>
        /// Connect to the database and return the values as a list of objects.
        /// </summary>
        /// <typeparam name="TModel">The type of object to be listed.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to null or empty.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A list containing an object for each data row.</returns>
        public Task<List<TModel>> MapListAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken) where TModel : new()
            => _manager.ListAsync<TModel>(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, this.MockResults, cancellationToken);

        /// <summary>
        /// Connect to the database and return the values as a list of objects.
        /// </summary>
        /// <typeparam name="TModel">The type of object to be listed.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A list containing an object for each data row.</returns>
        public Task<List<TModel>> MapListAsync<TModel>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken) where TModel : new()
            => _manager.ListAsync<TModel>(query, parameters, -1, null, this.MockResults, cancellationToken);

        /// <summary>
        /// Connect to the database and return a list of column values.
        /// </summary>
        /// <typeparam name="TValue">The type of the return value, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="columnName">This should match the name of a column containing the values.</param>
        /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to null or empty.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A list containing an object for each data row.</returns>
        public async Task<List<TValue>> ListAsync<TValue>(Query query, DbParameterCollection parameters, string columnName, string shardParameterName, CancellationToken cancellationToken)
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (List<TValue>)this.MockResults[query.Name];
            }
            var data = await _manager.ListAsync<TValue, object, object>(query, columnName, null, null, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken);
            var result = new List<TValue>();
            foreach (var itm in data)
            {
                result.Add(itm.Item1);
            }
            return result;
        }

        /// <summary>
        /// Connect to the database and return a list of column values.
        /// </summary>
        /// <typeparam name="TValue">The type of the return value, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="columnName">This should match the name of a column containing the values.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A list containing an object for each data row.</returns>
        public async Task<List<TValue>> ListAsync<TValue>(Query query, DbParameterCollection parameters, string columnName, CancellationToken cancellationToken)
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (List<TValue>)this.MockResults[query.Name];
            }
            var data = await _manager.ListAsync<TValue, object, object>(query, columnName, null, null, parameters, -1, null, cancellationToken);
            var result = new List<TValue>();
            foreach (var itm in data)
            {
                result.Add(itm.Item1);
            }
            return result;
        }

        /// <summary>
        /// Connect to the database and return a list of column values.
        /// </summary>
        /// <typeparam name="TRecord">The type of the record Id of the table key, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
        /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to null or empty.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A list containing an object for each data row.</returns>
        public async Task<List<ShardKey<TRecord>>> ListAsync<TRecord>(Query query, char origin, string recordColumnName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TRecord : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (List<ShardKey<TRecord>>)this.MockResults[query.Name];
            }
            var data = await _manager.ListAsync<TRecord, object, object>(query, recordColumnName, null, null, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken);
            var result = new List<ShardKey<TRecord>>();
            foreach (var itm in data)
            {
                result.Add(new ShardKey<TRecord>(origin, _shardId, itm.Item1));
            }
            return result;
        }

        /// <summary>
        /// Connect to the database and return a list of column values.
        /// </summary>
        /// <typeparam name="TRecord">The type of the record Id of the table key, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="columnName">This should match the name of a column containing the values.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A list containing an object for each data row.</returns>
        public async Task<List<ShardKey<TRecord>>> ListAsync<TRecord>(Query query, char origin, string recordColumnName, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TRecord : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (List<ShardKey<TRecord>>)this.MockResults[query.Name];
            }
            var data = await _manager.ListAsync<TRecord, object, object>(query, recordColumnName, null, null, parameters, -1, null, cancellationToken);
            var result = new List<ShardKey<TRecord>>();
            foreach (var itm in data)
            {
                result.Add(new ShardKey<TRecord>(origin, _shardId, itm.Item1));
            }
            return result;
        }

        /// <summary>
        /// Connect to the database and return a list of column values.
        /// </summary>
        /// <typeparam name="TRecord">The type of the record Id of the table key, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="shardColumnName">This should match the name of a column containing the ShardID component of the ShardKey.</param>
        /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to null or empty.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A list containing an object for each data row.</returns>
        public async Task<List<ShardKey<TRecord>>> ListAsync<TRecord>(Query query, char origin, string shardColumnName, string recordColumnName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TRecord : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (List<ShardKey<TRecord>>)this.MockResults[query.Name];
            }
            var data = await _manager.ListAsync<short, TRecord, object>(query, shardColumnName, recordColumnName, null, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken);
            var result = new List<ShardKey<TRecord>>();
            foreach (var itm in data)
            {
                result.Add(new ShardKey<TRecord>(origin, itm.Item1, itm.Item2));
            }
            return result;
        }

        /// <summary>
        /// Connect to the database and return a list of column values.
        /// </summary>
        /// <typeparam name="TRecord">The type of the record Id of the table key, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="shardColumnName">This should match the name of a column containing the ShardID component of the ShardKey.</param>
        /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A list containing an object for each data row.</returns>
        public async Task<List<ShardKey<TRecord>>> ListAsync<TRecord>(Query query, char origin, string shardColumnName, string recordColumnName, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TRecord : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (List<ShardKey<TRecord>>)this.MockResults[query.Name];
            }
            var data = await _manager.ListAsync<short, TRecord, object>(query, shardColumnName, recordColumnName, null, parameters, -1, null, cancellationToken);
            var result = new List<ShardKey<TRecord>>();
            foreach (var itm in data)
            {
                result.Add(new ShardKey<TRecord>(origin, itm.Item1, itm.Item2));
            }
            return result;
        }

        /// <summary>
        /// Connect to the database and return a list of column values.
        /// </summary>
        /// <typeparam name="TRecord">The type of the record Id of the table key, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <typeparam name="TChild">The type of the child Id of the compound table key, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
        /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to null or empty.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A list containing an object for each data row.</returns>
        public async Task<List<ShardKey<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string recordColumnName, string childColumnName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TRecord : IComparable
            where TChild : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (List<ShardKey<TRecord, TChild>>)this.MockResults[query.Name];
            }
            var data = await _manager.ListAsync<TRecord, TChild, object>(query, recordColumnName, childColumnName, null, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken);
            var result = new List<ShardKey<TRecord, TChild>>();
            foreach (var itm in data)
            {
                result.Add(new ShardKey<TRecord, TChild>(origin, _shardId, itm.Item1, itm.Item2));
            }
            return result;
        }

        /// <summary>
        /// Connect to the database and return a list of column values.
        /// </summary>
        /// <typeparam name="TRecord">The type of the record Id of the table key, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <typeparam name="TChild">The type of the child Id of the compound table key, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="columnName">This should match the name of a column containing the values.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A list containing an object for each data row.</returns>
        public async Task<List<ShardKey<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string recordColumnName, string childColumnName, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TRecord : IComparable
            where TChild : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (List<ShardKey<TRecord, TChild>>)this.MockResults[query.Name];
            }
            var data = await _manager.ListAsync<TRecord, TChild, object>(query, recordColumnName, childColumnName, null, parameters, -1, null, cancellationToken);
            var result = new List<ShardKey<TRecord, TChild>>();
            foreach (var itm in data)
            {
                result.Add(new ShardKey<TRecord, TChild>(origin, _shardId, itm.Item1, itm.Item2));
            }
            return result;
        }

        /// <summary>
        /// Connect to the database and return a list of column values.
        /// </summary>
        /// <typeparam name="TRecord">The type of the record Id of the table key, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <typeparam name="TChild">The type of the child Id of the compound table key, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="shardColumnName">This should match the name of a column containing the ShardID component of the ShardKey.</param>
        /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to null or empty.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A list containing an object for each data row.</returns>
        public async Task<List<ShardKey<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string shardColumnName, string recordColumnName, string childColumnName, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            where TRecord : IComparable
            where TChild : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (List<ShardKey<TRecord, TChild>>)this.MockResults[query.Name];
            }
            var data = await _manager.ListAsync<short, TRecord, TChild>(query, shardColumnName, recordColumnName, childColumnName, parameters, parameters.GetParameterOrdinal(shardParameterName), null, cancellationToken);
            var result = new List<ShardKey<TRecord, TChild>>();
            foreach (var itm in data)
            {
                result.Add(new ShardKey<TRecord, TChild>(origin, itm.Item1, itm.Item2, itm.Item3));
            }
            return result;
        }

        /// <summary>
        /// Connect to the database and return a list of column values.
        /// </summary>
        /// <typeparam name="TRecord">The type of the record Id of the table key, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <typeparam name="TChild">The type of the child Id of the compound table key, typically: Boolean, Byte, Char, DateTime, DateTimeOffset, Decimal, Double, Float, Guid, Int16, Int32, Int64, or String.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="origin">Origin value to indicate the ShardKey type.</param>
        /// <param name="shardColumnName">This should match the name of a column containing the ShardID component of the ShardKey.</param>
        /// <param name="recordColumnName">This should match the name of a column containing the RecordID component of the ShardKey.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>A list containing an object for each data row.</returns>
        public async Task<List<ShardKey<TRecord, TChild>>> ListAsync<TRecord, TChild>(Query query, char origin, string shardColumnName, string recordColumnName, string childColumnName, DbParameterCollection parameters, CancellationToken cancellationToken)
            where TRecord : IComparable
            where TChild : IComparable
        {
            if (!(this.MockResults is null) && this.MockResults.Count > 0 && this.MockResults.ContainsKey(query.Name))
            {
                return (List<ShardKey<TRecord, TChild>>)this.MockResults[query.Name];
            }
            var data = await _manager.ListAsync<short, TRecord, TChild>(query, shardColumnName, recordColumnName, childColumnName, parameters, -1, null, cancellationToken);
            var result = new List<ShardKey<TRecord, TChild>>();
            foreach (var itm in data)
            {
                result.Add(new ShardKey<TRecord, TChild>(origin, itm.Item1, itm.Item2, itm.Item3));
            }
            return result;
        }


        /// <summary>
        /// Connect to the database and return the TModel object returned by the delegate.
        /// </summary>
        /// <typeparam name="TModel">The type of the object to be returned.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to null or empty.</param>
        /// <param name="resultHandler">A method with a signature that corresponds to the QueryResultModelHandler delegate, which converts the provided DataReader and output parameters and returns an object of type TModel.</param>
        /// <param name="isTopOne">If the procedure or function is expected to return only one record, setting this to True provides a minor optimization.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>An object of type TModel, as created and populated by the provided delegate.</returns>
        public Task<TModel> QueryAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<object, TModel> resultHandler, bool isTopOne, CancellationToken cancellationToken)
            => _manager.QueryAsync<object, TModel>(default(TModel), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, resultHandler, isTopOne, null, this.MockResults, cancellationToken);

        /// <summary>
        /// Connect to the database and return the TModel object returned by the delegate.
        /// </summary>
        /// <typeparam name="TModel">The type of the object to be returned.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="resultHandler">A method with a signature that corresponds to the QueryResultModelHandler delegate, which converts the provided DataReader and output parameters and returns an object of type TModel.</param>
        /// <param name="isTopOne">If the procedure or function is expected to return only one record, setting this to True provides a minor optimization.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>An object of type TModel, as created and populated by the provided delegate.</returns>
		public Task<TModel> QueryAsync<TModel>(Query query, DbParameterCollection parameters, QueryResultModelHandler<object, TModel> resultHandler, bool isTopOne, CancellationToken cancellationToken)
            => _manager.QueryAsync<object, TModel>(default(TModel), query, parameters, -1, null, resultHandler, isTopOne, null, this.MockResults, cancellationToken);

        /// <summary>
        /// Connect to the database and return the TModel object returned by the delegate.
        /// </summary>
        /// <typeparam name="TArg"></typeparam>
        /// <typeparam name="TModel">The type of the object to be returned.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to null or empty.</param>
        /// <param name="resultHandler">A method with a signature that corresponds to the QueryResultModelHandler delegate, which converts the provided DataReader and output parameters and returns an object of type TModel.</param>
        /// <param name="isTopOne">If the procedure or function is expected to return only one record, setting this to True provides a minor optimization.</param>
        /// <param name="optionalArgument">An object of type TArg which can be used to pass non-datatabase data to the result-generating delegate.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>An object of type TModel, as created and populated by the provided delegate.</returns>
		public Task<TModel> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, string shardParameterName, QueryResultModelHandler<TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken)
            => _manager.QueryAsync<TArg, TModel>(default(TModel), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, resultHandler, isTopOne, optionalArgument, this.MockResults, cancellationToken);

        /// <summary>
        /// Connect to the database and return the TModel object returned by the delegate.
        /// </summary>
        /// <typeparam name="TArg"></typeparam>
        /// <typeparam name="TModel">The type of the object to be returned.</typeparam>
        /// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        /// <param name="parameters">The query parameters.</param>
        /// <param name="resultHandler">A method with a signature that corresponds to the QueryResultModelHandler delegate, which converts the provided DataReader and output parameters and returns an object of type TModel.</param>
        /// <param name="isTopOne">If the procedure or function is expected to return only one record, setting this to True provides a minor optimization.</param>
        /// <param name="optionalArgument">An object of type TArg which can be used to pass non-datatabase data to the result-generating delegate.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>An object of type TModel, as created and populated by the provided delegate.</returns>
		public Task<TModel> QueryAsync<TArg, TModel>(Query query, DbParameterCollection parameters, QueryResultModelHandler<TArg, TModel> resultHandler, bool isTopOne, TArg optionalArgument, CancellationToken cancellationToken)
            => _manager.QueryAsync<TArg, TModel>(default(TModel), query, parameters, -1, null, resultHandler, isTopOne, optionalArgument, this.MockResults, cancellationToken);


        /// <summary>
        /// Executes a database procedure or function that does not return a data result.
        /// </summary>
        /// <param name="sprocName">The stored procedure or function to call.</param>
        /// <param name="parameters">The query parameters with values set.</param>
        /// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to null or empty.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>Throws an error if not successful.</returns>
        public Task RunAsync(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
            => _manager.RunAsync(query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, this.MockResults, cancellationToken);

        /// <summary>
        /// Executes a database procedure or function that does not return a data result.
        /// </summary>
        /// <param name="sprocName">The stored procedure or function to call.</param>
        /// <param name="parameters">The query parameters with values set.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>Throws an error if not successful.</returns>
		public Task RunAsync(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
            => _manager.RunAsync(query, parameters, -1, null, this.MockResults, cancellationToken);


        /// <summary>
        /// Execute a set of commands within a single transaction.
        /// </summary>
        /// <typeparam name="TResult">The optional return type specified in the batch.</typeparam>
        /// <param name="batch">The QueryBatch object.</param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>The last valid TResult type returned by the collection of commands.</returns>
        public Task<TResult> RunAsync<TResult>(ShardBatch<TResult> batch, CancellationToken cancellationToken)
            => _manager.RunBatchAsync<TResult>(batch, this.MockResults, cancellationToken);

        #endregion
        #region GetOut overloads
        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TModel, Mapper.DummyType, TModel>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult>
        //    (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>
        //    (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
        //    (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
        //    (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
        //    (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
        //    (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    where TReaderResult5 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
        //    (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    where TReaderResult5 : new()
        //    where TReaderResult6 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
        //    (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    where TReaderResult5 : new()
        //    where TReaderResult6 : new()
        //    where TReaderResult7 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to null or empty.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TModel>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to null or empty.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult>
        //    (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to null or empty.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1>
        //    (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
        //    (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
        //    (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
        //    (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
        //    (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    where TReaderResult5 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
        //    (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    where TReaderResult5 : new()
        //    where TReaderResult6 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results and output parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query and should map to the output parameters.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult5">The sixth result set from data reader. This it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult6">The seventh result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult7">The eighth result set from data reader. This will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapOutputAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
        //    (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    where TReaderResult5 : new()
        //    where TReaderResult6 : new()
        //    where TReaderResult7 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, false, null, this.MockResults, cancellationToken);

        #endregion
        #region Read overloads
        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TModel>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>
        //    (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
        //    (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
        //    (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
        //    (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
        //    (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    where TReaderResult5 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult6">The seventh result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
        //    (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    where TReaderResult5 : new()
        //    where TReaderResult6 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult6">The seventh result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult7">The eighth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
        //    (Query query, DbParameterCollection parameters, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    where TReaderResult5 : new()
        //    where TReaderResult6 : new()
        //    where TReaderResult7 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, -1, null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results parameters.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel>(Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TModel>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1>
        //    (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
        //    (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
        //    (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
        //    (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
        //    (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    where TReaderResult5 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult6">The seventh result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
        //    (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    where TReaderResult5 : new()
        //    where TReaderResult6 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>, false, null, this.MockResults, cancellationToken);

        ///// <summary>
        ///// Connect to the database and return an object of the specified type built from the corresponding data reader results.
        ///// </summary>
        ///// <typeparam name="TModel">This is the expected return type of the query. It must also be the same type as one of the TReaderResult values.</typeparam>
        ///// <typeparam name="TReaderResult0">The first result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult1">The second result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult2">The third result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult3">The forth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult4">The fifth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult5">The sixth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult6">The seventh result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <typeparam name="TReaderResult7">The eighth result set from data reader. If the same type as TModel, it must return exactly one record. Otherwise, it will be mapped to any property with a List of this type.</typeparam>
        ///// <param name="query">The SQL procedure or statement to invoke to fetch the data.</param>
        ///// <param name="parameters">The query parameters.</param>
        ///// <param name="shardParameterName">The ordinal position of a parameter that should be automatically set to the current shard number value. If there is no such parameter, set to -1.</param>
        ///// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        ///// <returns></returns>
        //public Task<TModel> MapReaderAsync<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
        //    (Query query, DbParameterCollection parameters, string shardParameterName, CancellationToken cancellationToken)
        //    where TModel : new()
        //    where TReaderResult0 : new()
        //    where TReaderResult1 : new()
        //    where TReaderResult2 : new()
        //    where TReaderResult3 : new()
        //    where TReaderResult4 : new()
        //    where TReaderResult5 : new()
        //    where TReaderResult6 : new()
        //    where TReaderResult7 : new()
        //    => _manager.QueryAsync<object, TModel>(new TModel(), query, parameters, parameters.GetParameterOrdinal(shardParameterName), null, Mapper.ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>, false, null, this.MockResults, cancellationToken);
        #endregion
    }
}
