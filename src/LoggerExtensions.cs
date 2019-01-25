// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Linq.Expressions;
using System.Text;
using System.Globalization;

namespace ArgentSea
{
    /// <summary>
    /// The are extension methods for high-performance logging.
    /// </summary>
    public static class LoggingExtensions
    {
        public enum EventIdentifier
        {
            ConnectionStringBuilt,
            LogExpressionTreeCreation,
            MapperSqlParameterNotFound,
            MapperSqlColumnNotFound,
            MapperInParameterCacheStatus,
            MapperSetOutParameterCache,
            MapperSetOutParameterCacheStatus,
            MapperReadOutParameterCacheStatus,
            MapperReaderCacheStatus,
            MapperInTrace,
            MapperSetOutTrace,
            MapperGetOutTrace,
            MapperRdrTrace,
            MapperShardKeyNull,
            MapperShardChildNull,
            MapperResultsReaderInvalid,
            LogCmdExecuted,
            LogConnectRetry,
            LogCommandRetry,
            LogCircuitBreakerOn,
            LogCircuitBreakerTest,
            LogCircuitBreakerOff,
            RequiredPropertyIsDbNull
        }

        private static readonly Action<ILogger, string, Exception> _sqlConnectionStringBuilt;
        private static readonly Action<ILogger, Type, Exception> _sqlInParameterCacheMiss;
        private static readonly Action<ILogger, Type, Exception> _sqlInParameterCacheHit;
        private static readonly Action<ILogger, Type, Exception> _sqlSetOutParameterCacheMiss;
        private static readonly Action<ILogger, Type, Exception> _sqlSetOutParameterCacheHit;
        private static readonly Action<ILogger, Type, Exception> _sqlReadOutParameterCacheMiss;
        private static readonly Action<ILogger, Type, Exception> _sqlReadOutParameterCacheHit;
        private static readonly Action<ILogger, Type, Exception> _sqlReaderCacheMiss;
        private static readonly Action<ILogger, Type, Exception> _sqlReaderCacheHit;
        private static readonly Action<ILogger, string, Type, Exception> _sqlParameterNotFound;
        private static readonly Action<ILogger, string, Type, Exception> _sqlFieldNotFound;
        private static readonly Action<ILogger, string, Exception> _sqlMapperInTrace;
        private static readonly Action<ILogger, string, Exception> _sqlMapperSetOutTrace;
        private static readonly Action<ILogger, string, Exception> _sqlMapperGetOutTrace;
        private static readonly Action<ILogger, string, Exception> _sqlMapperRdrTrace;
        private static readonly Action<ILogger, string, string, Exception> _sqlShardKeyNull;
        private static readonly Action<ILogger, string, string, Exception> _sqlShardChildNull;
        private static readonly Func<ILogger, string, Type, IDisposable> _buildSqlResultsHandlerScope;
        private static readonly Action<ILogger, string, string, long, Exception> _sqlDbCmdExecutedTrace;
        private static readonly Action<ILogger, string, string, string, long, Exception> _sqlShardCmdExecutedTrace;
        private static readonly Action<ILogger, string, int, Exception> _sqlConnectRetry;
        private static readonly Action<ILogger, string, string, int, Exception> _sqlCommandRetry;
        private static readonly Action<ILogger, string, Exception> _sqlConnectionCircuitBreakerOn;
        private static readonly Action<ILogger, string, string, Exception> _sqlCommandCircuitBreakerOn;
        private static readonly Action<ILogger, string, Exception> _sqlConnectionCircuitBreakerTest;
        private static readonly Action<ILogger, string, string, Exception> _sqlCommandCircuitBreakerTest;
        private static readonly Action<ILogger, string, Exception> _sqlConnectionCircuitBreakerOff;
        private static readonly Action<ILogger, string, string, Exception> _sqlCommandCircuitBreakerOff;

        private static readonly Action<ILogger, string, string, Exception> _sqlMapperReaderIsNull;
        private static readonly Action<ILogger, string, string, Exception> _sqlMapperReaderIsClosed;
        private static readonly Action<ILogger, string, string, Exception> _sqlRequiredValueIsDbNull;

        private static readonly Action<ILogger, string, string, Exception> _sqlGetInExpressionTreeCreation;
        private static readonly Action<ILogger, string, string, Exception> _sqlSetOutExpressionTreeCreation;
        private static readonly Action<ILogger, string, string, Exception> _sqlReadOutExpressionTreeCreation;
        private static readonly Action<ILogger, string, string, Exception> _sqlReaderExpressionTreeDataRowCreation;
        private static readonly Action<ILogger, string, string, Exception> _sqlReaderExpressionTreeOrdinalsCreation;
        private static readonly Action<ILogger, string, string, string, Exception> _sqlObjectExpressionTreeCreation;

        static LoggingExtensions()
        {
            //_sqlCreate = LoggerMessage.Define<string, Type, string>(LogLevel.Debug, new EventId((int)EventIdentifier.EventDelegate, nameof(SqlDelegateCreated)), "Created delegate for {source} for object type {type}: \r\n{{{text}}}");

            _sqlConnectionStringBuilt = LoggerMessage.Define<string>(LogLevel.Debug, new EventId((int)EventIdentifier.ConnectionStringBuilt, nameof(SqlConnectionStringBuilt)), "A new connection string was built with a value of {ConnectString}.");
            _sqlInParameterCacheMiss = LoggerMessage.Define<Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperInParameterCacheStatus, nameof(SqlInParametersCacheMiss)), "No cached delegate for creating input parameters was initialized for type {TModel}; this is normal for the first execution.");
            _sqlInParameterCacheHit = LoggerMessage.Define<Type>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperInParameterCacheStatus, nameof(SqlInParametersCacheHit)), "The cached delegate for creating input parameters was already initialized for type {TModel}.");
            _sqlSetOutParameterCacheMiss = LoggerMessage.Define<Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperSetOutParameterCacheStatus, nameof(SqlSetOutParametersCacheMiss)), "No cached delegate for creating output parameters was initialized for type {TModel}; this is normal for the first execution.");
            _sqlSetOutParameterCacheHit = LoggerMessage.Define<Type>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperSetOutParameterCacheStatus, nameof(SqlSetOutParametersCacheHit)), "The cached delegate for creating output parameters was already initialized for type {TModel}.");
            _sqlReadOutParameterCacheMiss = LoggerMessage.Define<Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperReadOutParameterCacheStatus, nameof(SqlSetOutParametersCacheMiss)), "No cached delegate for creating output parameters was initialized for type {TModel}; this is normal for the first execution.");
            _sqlReadOutParameterCacheHit = LoggerMessage.Define<Type>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperReadOutParameterCacheStatus, nameof(SqlSetOutParametersCacheHit)), "The cached delegate for creating output parameters was already initialized for type {TModel}.");
            _sqlReaderCacheMiss = LoggerMessage.Define<Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperReaderCacheStatus, nameof(SqlReaderCacheMiss)), "No cached delegate for mapping a data reader was initialized for type {TModel}; this is normal for the first execution.");
            _sqlReaderCacheHit = LoggerMessage.Define<Type>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperReaderCacheStatus, nameof(SqlReaderCacheHit)), "The cached delegate for mapping a data reader was already initialized for type {TModel}.");
            _sqlParameterNotFound = LoggerMessage.Define<string, Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperSqlParameterNotFound, nameof(SqlParameterNotFound)), "Sql Parameter {parameterName} was defined on {Type} but was not found among the provided output parameters.");
            _sqlFieldNotFound = LoggerMessage.Define<string, Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperSqlColumnNotFound, nameof(SqlFieldNotFound)), "Sql Parameter {parameterName} was defined on {TModel} but was not found in output parameters.");
            _sqlMapperInTrace = LoggerMessage.Define<string>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperInTrace, nameof(TraceInMapperProperty)), "In-parameter mapper is now processing property {name}.");
            _sqlMapperSetOutTrace = LoggerMessage.Define<string>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperSetOutTrace, nameof(TraceSetOutMapperProperty)), "Set out-parameter mapper is now processing property {name}.");
            _sqlMapperGetOutTrace = LoggerMessage.Define<string>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperGetOutTrace, nameof(TraceGetOutMapperProperty)), "Get out-parameter mapper is now processing property {name}.");
            _sqlMapperRdrTrace = LoggerMessage.Define<string>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperRdrTrace, nameof(TraceRdrMapperProperty)), "Data reader field mapper is now processing property {name}.");
            _sqlShardKeyNull = LoggerMessage.Define<string, string>(LogLevel.Information, new EventId((int)EventIdentifier.MapperShardKeyNull, nameof(TraceRdrMapperProperty)), "The {name} shard key could not be built because one of the input values was dbNull. The shard key value was {shardKey}.");
            _sqlShardChildNull = LoggerMessage.Define<string, string>(LogLevel.Information, new EventId((int)EventIdentifier.MapperShardChildNull, nameof(TraceRdrMapperProperty)), "The {name} shard child could not be built because one or two of the input values was dbNull. The shard child value was {shardChild}.");
            _buildSqlResultsHandlerScope = LoggerMessage.DefineScope<string, Type>("Build logic to convert sql procedure {name} results to result {type}");
            _sqlDbCmdExecutedTrace = LoggerMessage.Define<string, string, long>(LogLevel.Trace, new EventId((int)EventIdentifier.LogCmdExecuted, nameof(TraceDbCmdExecuted)), "Executed command {name} on Db connection {connectionName} in {milliseconds} milliseconds.");
            _sqlShardCmdExecutedTrace = LoggerMessage.Define<string, string, string, long>(LogLevel.Trace, new EventId((int)EventIdentifier.LogCmdExecuted, nameof(TraceShardCmdExecuted)), "Executed command {name} on ShardSet {shardSet} connection {shardId} in {milliseconds} milliseconds.");
            _sqlConnectRetry = LoggerMessage.Define<string, int>(LogLevel.Warning, new EventId((int)EventIdentifier.LogConnectRetry, nameof(RetryingDbConnection)), "Initiating automatic connection retry for transient error on Db connection {connectionName}. This is attempt number {attempt}.");
            _sqlCommandRetry = LoggerMessage.Define<string, string, int>(LogLevel.Warning, new EventId((int)EventIdentifier.LogCommandRetry, nameof(RetryingDbCommand)), "Initiating automatic command retry for transient error on command {name} on Db connection {connectionName}. This is attempt number {attempt}.");
            _sqlConnectionCircuitBreakerOn = LoggerMessage.Define<string>(LogLevel.Critical, new EventId((int)EventIdentifier.LogCircuitBreakerOn, nameof(CiruitBreakingDbConnection)), "Circuit breaking failing connection on Db connection {connectionName}. Most subsequent calls to this connection will fail.");
            _sqlCommandCircuitBreakerOn = LoggerMessage.Define<string, string>(LogLevel.Critical, new EventId((int)EventIdentifier.LogCircuitBreakerOn, nameof(CiruitBreakingDbCommand)), "Circuit breaking failing command {name} on Db connection {connectionName}.");
            _sqlConnectionCircuitBreakerTest = LoggerMessage.Define<string>(LogLevel.Information, new EventId((int)EventIdentifier.LogCircuitBreakerTest, nameof(CiruitBrokenDbConnectionTest)), "Circuit broken connection {connectionName} is being retested.");
            _sqlCommandCircuitBreakerTest = LoggerMessage.Define<string, string>(LogLevel.Information, new EventId((int)EventIdentifier.LogCircuitBreakerTest, nameof(CiruitBrokenDbCommandTest)), "Circuit broken command {name} on Db connection {connectionName} is being retested.");
            _sqlConnectionCircuitBreakerOff = LoggerMessage.Define<string>(LogLevel.Information, new EventId((int)EventIdentifier.LogCircuitBreakerOff, nameof(CiruitBrokenDbConnectionRestored)), "Circuit broken connection {connectionName} is restored.");
            _sqlCommandCircuitBreakerOff = LoggerMessage.Define<string, string>(LogLevel.Information, new EventId((int)EventIdentifier.LogCircuitBreakerOff, nameof(CiruitBrokenDbCommandRestored)), "Circuit broken command {name} on Db connection {connectionName} is restored.");
            _sqlMapperReaderIsNull = LoggerMessage.Define<string, string>(LogLevel.Error, new EventId((int)EventIdentifier.MapperResultsReaderInvalid, nameof(DataReaderIsNull)), "Expected data reader object was null from procedure {sproc} on connection {connectionName}.");
            _sqlMapperReaderIsClosed = LoggerMessage.Define<string, string>(LogLevel.Error, new EventId((int)EventIdentifier.MapperResultsReaderInvalid, nameof(DataReaderIsClosed)), "Expected data reader object was closed from procedure {sproc} on connection {connectionName}.");
            _sqlRequiredValueIsDbNull = LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId((int)EventIdentifier.RequiredPropertyIsDbNull, nameof(RequiredPropertyIsDbNull)), "Request for object {model} returned null because database parameter {parameterName} was null. This may mean the object was not found in the database.");

            _sqlGetInExpressionTreeCreation = LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId((int)EventIdentifier.LogExpressionTreeCreation, nameof(CreatedExpressionTreeForSetInParameters)), "Compiled code to map model {model} to input parameters as:\r\n{code}.");
            _sqlSetOutExpressionTreeCreation = LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId((int)EventIdentifier.LogExpressionTreeCreation, nameof(CreatedExpressionTreeForSetOutParameters)), "Compiled code to map model {model} to set output parameters as:\r\n{code}.");
            _sqlReadOutExpressionTreeCreation = LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId((int)EventIdentifier.LogExpressionTreeCreation, nameof(CreatedExpressionTreeForReadOutParameters)), "Compiled code to map model {model} to read output parameters as:\r\n{code}.");
            _sqlReaderExpressionTreeDataRowCreation = LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId((int)EventIdentifier.LogExpressionTreeCreation, nameof(CreatedExpressionTreeForReaderRowData)), "Compiled code to map model {model} to data reader row values as:\r\n{code}.");
            _sqlReaderExpressionTreeOrdinalsCreation = LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId((int)EventIdentifier.LogExpressionTreeCreation, nameof(CreatedExpressionTreeForReaderRowData)), "Compiled code to map model {model} ordinals to data reader values as:\r\n{code}.");
            _sqlObjectExpressionTreeCreation = LoggerMessage.Define<string, string, string>(LogLevel.Debug, new EventId((int)EventIdentifier.LogExpressionTreeCreation, nameof(CreatedExpressionTreeForModel)), "Compiled code to map model {model} to stored procedure {sproc} as:\r\n{code}.");
        }

        public static void SqlConnectionStringBuilt(this ILogger logger, string connectionString)
            => _sqlConnectionStringBuilt(logger, connectionString, null);

        /// <summary>
        /// If the log level is set to Information, logs when a parameter attribute exists but was not found in the parameters collection.
        /// </summary>
        /// <param name="logger">The logging instance for this extension method.</param>
        /// <param name="parameterName">The name of the expected parameter.</param>
        /// <param name="propertyType">The type of the property expected said parameter.</param>
        public static void SqlParameterNotFound(this ILogger logger, string parameterName, Type propertyType)
            => _sqlParameterNotFound(logger, parameterName, propertyType, null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="columnName"></param>
        /// <param name="TModel"></param>
        public static void SqlFieldNotFound(this ILogger logger, string columnName, Type TModel)
            => _sqlFieldNotFound(logger, columnName, TModel, null);
        public static void SqlInParametersCacheHit(this ILogger logger, Type TModel)
            => _sqlInParameterCacheHit(logger, TModel, null);

        public static void SqlInParametersCacheMiss(this ILogger logger, Type TModel)
            => _sqlInParameterCacheMiss(logger, TModel, null);

        public static void SqlSetOutParametersCacheHit(this ILogger logger, Type TModel)
            => _sqlSetOutParameterCacheHit(logger, TModel, null);

        public static void SqlSetOutParametersCacheMiss(this ILogger logger, Type TModel)
            => _sqlSetOutParameterCacheMiss(logger, TModel, null);

        public static void SqlReadOutParametersCacheHit(this ILogger logger, Type TModel)
            => _sqlReadOutParameterCacheHit(logger, TModel, null);

        public static void SqlReadOutParametersCacheMiss(this ILogger logger, Type TModel)
            => _sqlReadOutParameterCacheMiss(logger, TModel, null);

        public static void SqlReaderCacheMiss(this ILogger logger, Type TModel)
            => _sqlReaderCacheMiss(logger, TModel, null);

        public static void SqlReaderCacheHit(this ILogger logger, Type TModel)
            => _sqlReaderCacheHit(logger, TModel, null);

        public static void TraceInMapperProperty(this ILogger logger, string propertyName)
            => _sqlMapperInTrace(logger, propertyName, null);

        public static void TraceSetOutMapperProperty(this ILogger logger, string propertyName)
            => _sqlMapperSetOutTrace(logger, propertyName, null);

        public static void TraceGetOutMapperProperty(this ILogger logger, string propertyName)
            => _sqlMapperGetOutTrace(logger, propertyName, null);

        public static void TraceRdrMapperProperty(this ILogger logger, string propertyName)
            => _sqlMapperRdrTrace(logger, propertyName, null);

        public static void NullShardKeyArguments<TShard, TRecord>(this ILogger logger, string propertyName, ShardKey<TShard, TRecord> shardKey) where TShard : IComparable where TRecord : IComparable
        {
            if (shardKey.ShardId.Equals(null) || shardKey.RecordId.Equals(null))
            {
                _sqlShardKeyNull(logger, propertyName, shardKey.ToString(), null);
            }
        }
        public static void NullShardChildArguments<TShard, TRecord, TChild>(this ILogger logger, string propertyName, ShardChild<TShard, TRecord, TChild> shardChild) where TShard : IComparable where TRecord : IComparable where TChild : IComparable
        {
            if (shardChild.Key.ShardId.Equals(null) || shardChild.Key.RecordId.Equals(null) || shardChild.ChildId.Equals(null))
            {
                _sqlShardChildNull(logger, propertyName, shardChild.ToString(), null);
            }
        }
        public static IDisposable BuildSqlResultsHandlerScope(this ILogger logger, string procedureName, Type model)
            => _buildSqlResultsHandlerScope(logger, procedureName, model);

        public static void TraceDbCmdExecuted(this ILogger logger, string commandName, string connectionName, long milliseconds)
            => _sqlDbCmdExecutedTrace(logger, commandName, connectionName, milliseconds, null);

        public static void TraceShardCmdExecuted<TShard>(this ILogger logger, string commandName, string shardSetKey, TShard shardId, long milliseconds) where TShard : IComparable
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _sqlShardCmdExecutedTrace(logger, commandName, shardSetKey, shardId.ToString(), milliseconds, null);
            }
        }
        public static void RetryingDbConnection(this ILogger logger, string connectionName, int attemptCount, Exception exception)
            => _sqlConnectRetry(logger, connectionName, attemptCount, exception);

        public static void RetryingDbCommand(this ILogger logger, string commandName, string connectionName, int attemptCount, Exception exception)
            => _sqlCommandRetry(logger, commandName, connectionName, attemptCount, exception);

        public static void CiruitBreakingDbConnection(this ILogger logger, string connectionName)
            => _sqlConnectionCircuitBreakerOn(logger, connectionName, null);

        public static void CiruitBreakingDbCommand(this ILogger logger, string commandName, string connectionName)
            => _sqlCommandCircuitBreakerOn(logger, commandName, connectionName, null);

        public static void CiruitBrokenDbConnectionTest(this ILogger logger, string connectionName)
            => _sqlConnectionCircuitBreakerTest(logger, connectionName, null);

        public static void CiruitBrokenDbCommandTest(this ILogger logger, string commandName, string connectionName)
            => _sqlCommandCircuitBreakerTest(logger, commandName, connectionName, null);

        public static void CiruitBrokenDbConnectionRestored(this ILogger logger, string connectionName)
            => _sqlConnectionCircuitBreakerOff(logger, connectionName, null);

        public static void CiruitBrokenDbCommandRestored(this ILogger logger, string commandName, string connectionName)
            => _sqlCommandCircuitBreakerOff(logger, commandName, connectionName, null);

        public static void DataReaderIsNull(this ILogger logger, string sprocName, string connectionName)
            => _sqlMapperReaderIsNull(logger, sprocName, connectionName, null);

        public static void DataReaderIsClosed(this ILogger logger, string sprocName, string connectionName)
            => _sqlMapperReaderIsClosed(logger, sprocName, connectionName, null);

        public static void RequiredPropertyIsDbNull(this ILogger logger, string modelName, string parameterName)
            => _sqlRequiredValueIsDbNull(logger, modelName, parameterName, null);

        public static void CreatedExpressionTreeForSetInParameters(this ILogger logger, Type model, Expression codeBlock)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                using (System.IO.StringWriter writer = new System.IO.StringWriter(CultureInfo.CurrentCulture))
                {
                    DebugViewWriter.WriteTo(codeBlock, writer);
                    _sqlGetInExpressionTreeCreation(logger, model.ToString(), writer.ToString(), null);
                }
            }
        }
        public static void CreatedExpressionTreeForSetOutParameters(this ILogger logger, Type model, Expression codeBlock)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                using (System.IO.StringWriter writer = new System.IO.StringWriter(CultureInfo.CurrentCulture))
                {
                    DebugViewWriter.WriteTo(codeBlock, writer);
                    _sqlSetOutExpressionTreeCreation(logger, model.ToString(), writer.ToString(), null);
                }
            }
        }
        public static void CreatedExpressionTreeForReadOutParameters(this ILogger logger, Type model, Expression codeBlock)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                using (System.IO.StringWriter writer = new System.IO.StringWriter(CultureInfo.CurrentCulture))
                {
                    DebugViewWriter.WriteTo(codeBlock, writer);
                    _sqlReadOutExpressionTreeCreation(logger, model.ToString(), writer.ToString(), null);
                }
            }
        }
        public static void CreatedExpressionTreeForReaderOrdinals(this ILogger logger, Type model, Expression codeBlock)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                using (System.IO.StringWriter writer = new System.IO.StringWriter(CultureInfo.CurrentCulture))
                {
                    DebugViewWriter.WriteTo(codeBlock, writer);
                    _sqlReaderExpressionTreeDataRowCreation(logger, model.ToString(), writer.ToString(), null);
                }
            }
        }
        public static void CreatedExpressionTreeForReaderRowData(this ILogger logger, Type model, Expression codeBlock)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                using (System.IO.StringWriter writer = new System.IO.StringWriter(CultureInfo.CurrentCulture))
                {
                    DebugViewWriter.WriteTo(codeBlock, writer);
                    _sqlReaderExpressionTreeDataRowCreation(logger, model.ToString(), writer.ToString(), null);
                }
            }
        }
        public static void CreatedExpressionTreeForModel(this ILogger logger, Type model, string procedureName, Expression codeBlock)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                using (System.IO.StringWriter writer = new System.IO.StringWriter(CultureInfo.CurrentCulture))
                {
                    DebugViewWriter.WriteTo(codeBlock, writer);
                    _sqlObjectExpressionTreeCreation(logger, model.ToString(), procedureName, writer.ToString(), null);
                }
            }
        }
    }
}