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
            ConnectionStringBuilt = 0,
            ExpressionTreeCreation = 1,
            MapperSqlParameterNotFound = 2,
            MapperSqlColumnNotFound = 3,
            MapperCacheStatus = 4,
            MapperProcessTrace = 5,
            UnexpectedDbNull = 6,
            MapperResultsReaderInvalid = 7,
            CmdExecuted = 8,
            ConnectRetry = 9,
            CommandRetry = 10,
            CircuitBreaker = 11,
            BatchStep = 12
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
        private static readonly Action<ILogger, string, string, Exception> _sqlFieldNotFound;
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
        //private static readonly Action<ILogger, string, string, int, Exception> _sqlCommandRetry;
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
        private static readonly Action<ILogger, string, Exception> _emptyResult;
        private static readonly Action<ILogger, int, string, Exception> _batchStepStart;
        private static readonly Action<ILogger, int, string, Exception> _batchStepEnd;

        static LoggingExtensions()
        {
            //_sqlCreate = LoggerMessage.Define<string, Type, string>(LogLevel.Debug, new EventId((int)EventIdentifier.EventDelegate, nameof(SqlDelegateCreated)), "Created delegate for {source} for object type {type}: \r\n{{{text}}}");

            _sqlConnectionStringBuilt = LoggerMessage.Define<string>(LogLevel.Debug, new EventId((int)EventIdentifier.ConnectionStringBuilt, nameof(SqlConnectionStringBuilt)), "A new connection string was built with a value of {ConnectString}.");
            _sqlInParameterCacheMiss = LoggerMessage.Define<Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperCacheStatus, nameof(SqlInParametersCacheMiss)), "No cached delegate for creating input parameters was initialized for type {TModel}; this is normal for the first execution.");
            _sqlInParameterCacheHit = LoggerMessage.Define<Type>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperCacheStatus, nameof(SqlInParametersCacheHit)), "The cached delegate for creating input parameters was already initialized for type {TModel}.");
            _sqlSetOutParameterCacheMiss = LoggerMessage.Define<Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperCacheStatus, nameof(SqlSetOutParametersCacheMiss)), "No cached delegate for creating output parameters was initialized for type {TModel}; this is normal for the first execution.");
            _sqlSetOutParameterCacheHit = LoggerMessage.Define<Type>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperCacheStatus, nameof(SqlSetOutParametersCacheHit)), "The cached delegate for creating output parameters was already initialized for type {TModel}.");
            _sqlReadOutParameterCacheMiss = LoggerMessage.Define<Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperCacheStatus, nameof(SqlSetOutParametersCacheMiss)), "No cached delegate for creating output parameters was initialized for type {TModel}; this is normal for the first execution.");
            _sqlReadOutParameterCacheHit = LoggerMessage.Define<Type>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperCacheStatus, nameof(SqlSetOutParametersCacheHit)), "The cached delegate for creating output parameters was already initialized for type {TModel}.");
            _sqlReaderCacheMiss = LoggerMessage.Define<Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperCacheStatus, nameof(SqlReaderCacheMiss)), "No cached delegate for mapping a data reader was initialized for type {TModel}; this is normal for the first execution.");
            _sqlReaderCacheHit = LoggerMessage.Define<Type>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperCacheStatus, nameof(SqlReaderCacheHit)), "The cached delegate for mapping a data reader was already initialized for type {TModel}.");
            _sqlParameterNotFound = LoggerMessage.Define<string, Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperSqlParameterNotFound, nameof(SqlParameterNotFound)), "Sql Parameter {parameterName} was defined on {Type} but was not found among the provided parameters.");
            _sqlFieldNotFound = LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperSqlColumnNotFound, nameof(SqlFieldNotFound)), "Column {columnName} was defined on {modelName} but was not found in the result set.");
            _sqlMapperInTrace = LoggerMessage.Define<string>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperProcessTrace, nameof(TraceInMapperProperty)), "In-parameter mapper is now processing property {name}.");
            _sqlMapperSetOutTrace = LoggerMessage.Define<string>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperProcessTrace, nameof(TraceSetOutMapperProperty)), "Set out-parameter mapper is now processing property {name}.");
            _sqlMapperGetOutTrace = LoggerMessage.Define<string>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperProcessTrace, nameof(TraceGetOutMapperProperty)), "Get out-parameter mapper is now processing property {name}.");
            _sqlMapperRdrTrace = LoggerMessage.Define<string>(LogLevel.Trace, new EventId((int)EventIdentifier.MapperProcessTrace, nameof(TraceRdrMapperProperty)), "Data reader field mapper is now processing property {name}.");
            _sqlShardKeyNull = LoggerMessage.Define<string, string>(LogLevel.Information, new EventId((int)EventIdentifier.UnexpectedDbNull, nameof(TraceRdrMapperProperty)), "The {name} shard key could not be built because one of the input values was dbNull. The shard key value was {shardKey}.");
            _sqlShardChildNull = LoggerMessage.Define<string, string>(LogLevel.Information, new EventId((int)EventIdentifier.UnexpectedDbNull, nameof(TraceRdrMapperProperty)), "The {name} shard child could not be built because one or two of the input values was dbNull. The shard child value was {shardChild}.");
            _buildSqlResultsHandlerScope = LoggerMessage.DefineScope<string, Type>("Build logic to convert sql procedure {name} results to result {type}");
            _sqlDbCmdExecutedTrace = LoggerMessage.Define<string, string, long>(LogLevel.Trace, new EventId((int)EventIdentifier.CmdExecuted, nameof(TraceDbCmdExecuted)), "Executed command {name} on Db connection {connectionName} in {milliseconds} milliseconds.");
            _sqlShardCmdExecutedTrace = LoggerMessage.Define<string, string, string, long>(LogLevel.Trace, new EventId((int)EventIdentifier.CmdExecuted, nameof(TraceShardCmdExecuted)), "Executed command {name} on ShardSet {shardSet} connection {shardId} in {milliseconds} milliseconds.");
            _sqlConnectRetry = LoggerMessage.Define<string, int>(LogLevel.Warning, new EventId((int)EventIdentifier.ConnectRetry, nameof(RetryingDbConnection)), "Initiating automatic connection retry for transient error on Db connection {connectionName}. This is attempt number {attempt}.");
            //_sqlCommandRetry = LoggerMessage.Define<string, string, int>(LogLevel.Warning, new EventId((int)EventIdentifier.CommandRetry, nameof(RetryingDbCommand)), "Initiating automatic command retry for transient error on command {name} on Db connection {connectionName}. This is attempt number {attempt}.");
            _sqlConnectionCircuitBreakerOn = LoggerMessage.Define<string>(LogLevel.Critical, new EventId((int)EventIdentifier.CircuitBreaker, nameof(CiruitBreakingDbConnection)), "Circuit breaking failing connection on Db connection {connectionName}. Most subsequent calls to this connection will fail.");
            _sqlCommandCircuitBreakerOn = LoggerMessage.Define<string, string>(LogLevel.Critical, new EventId((int)EventIdentifier.CircuitBreaker, nameof(CiruitBreakingDbCommand)), "Circuit breaking failing command {name} on Db connection {connectionName}.");
            _sqlConnectionCircuitBreakerTest = LoggerMessage.Define<string>(LogLevel.Information, new EventId((int)EventIdentifier.CircuitBreaker, nameof(CiruitBrokenDbConnectionTest)), "Circuit broken connection {connectionName} is being retested.");
            _sqlCommandCircuitBreakerTest = LoggerMessage.Define<string, string>(LogLevel.Information, new EventId((int)EventIdentifier.CircuitBreaker, nameof(CiruitBrokenDbCommandTest)), "Circuit broken command {name} on Db connection {connectionName} is being retested.");
            _sqlConnectionCircuitBreakerOff = LoggerMessage.Define<string>(LogLevel.Information, new EventId((int)EventIdentifier.CircuitBreaker, nameof(CiruitBrokenDbConnectionRestored)), "Circuit broken connection {connectionName} is restored.");
            _sqlCommandCircuitBreakerOff = LoggerMessage.Define<string, string>(LogLevel.Information, new EventId((int)EventIdentifier.CircuitBreaker, nameof(CiruitBrokenDbCommandRestored)), "Circuit broken command {name} on Db connection {connectionName} is restored.");
            _sqlMapperReaderIsNull = LoggerMessage.Define<string, string>(LogLevel.Error, new EventId((int)EventIdentifier.MapperResultsReaderInvalid, nameof(DataReaderIsNull)), "Expected data reader object was null from procedure {sproc} on connection {connectionName}.");
            _sqlMapperReaderIsClosed = LoggerMessage.Define<string, string>(LogLevel.Error, new EventId((int)EventIdentifier.MapperResultsReaderInvalid, nameof(DataReaderIsClosed)), "Expected data reader object was closed from procedure {sproc} on connection {connectionName}.");
            _sqlRequiredValueIsDbNull = LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId((int)EventIdentifier.UnexpectedDbNull, nameof(RequiredPropertyIsDbNull)), "Request for object {model} returned null because database parameter {parameterName} was null. This may mean the object was not found in the database.");

            _sqlGetInExpressionTreeCreation = LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId((int)EventIdentifier.ExpressionTreeCreation, nameof(CreatedExpressionTreeForSetInParameters)), "Compiled code to map model {model} to input parameters as:\r\n{code}.");
            _sqlSetOutExpressionTreeCreation = LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId((int)EventIdentifier.ExpressionTreeCreation, nameof(CreatedExpressionTreeForSetOutParameters)), "Compiled code to map model {model} to set output parameters as:\r\n{code}.");
            _sqlReadOutExpressionTreeCreation = LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId((int)EventIdentifier.ExpressionTreeCreation, nameof(CreatedExpressionTreeForReadOutParameters)), "Compiled code to map model {model} to read output parameters as:\r\n{code}.");
            _sqlReaderExpressionTreeDataRowCreation = LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId((int)EventIdentifier.ExpressionTreeCreation, nameof(CreatedExpressionTreeForReaderRowData)), "Compiled code to map model {model} to data reader row values as:\r\n{code}.");
            _sqlReaderExpressionTreeOrdinalsCreation = LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId((int)EventIdentifier.ExpressionTreeCreation, nameof(CreatedExpressionTreeForReaderRowData)), "Compiled code to map model {model} ordinals to data reader values as:\r\n{code}.");
            _sqlObjectExpressionTreeCreation = LoggerMessage.Define<string, string, string>(LogLevel.Debug, new EventId((int)EventIdentifier.ExpressionTreeCreation, nameof(CreatedExpressionTreeForModel)), "Compiled code to map model {model} to stored procedure {sproc} as:\r\n{code}.");
            _emptyResult = LoggerMessage.Define<string>(LogLevel.Debug, new EventId((int)EventIdentifier.UnexpectedDbNull, nameof(EmptyResult)), "The base object could not be built because the query returned an empty result. The query was: “{ queryName }” ");
            _batchStepStart = LoggerMessage.Define<int, string>(LogLevel.Debug, new EventId((int)EventIdentifier.BatchStep, nameof(BatchStepStart)), "Starting batch step { stepNumber.ToString() } on connection “{ connection }” ");
            _batchStepEnd = LoggerMessage.Define<int, string>(LogLevel.Debug, new EventId((int)EventIdentifier.BatchStep, nameof(BatchStepEnd)), "Completed batch step { stepNumber.ToString() } on connection “{ connection }” ");
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
        public static void SqlFieldNotFound(this ILogger logger, string columnName, string modelName)
            => _sqlFieldNotFound(logger, columnName, modelName, null);
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

        public static void NullShardChildArguments<TRecord, TChild>(this ILogger logger, string propertyName, ShardKey<TRecord, TChild> shardChild) where TRecord : IComparable where TChild : IComparable
        {
            if (shardChild.ShardId.Equals(null) || shardChild.RecordId.Equals(null) || shardChild.ChildId.Equals(null))
            {
                _sqlShardChildNull(logger, propertyName, shardChild.ToString(), null);
            }
        }
        public static IDisposable BuildSqlResultsHandlerScope(this ILogger logger, string procedureName, Type model)
            => _buildSqlResultsHandlerScope(logger, procedureName, model);

        public static void TraceDbCmdExecuted(this ILogger logger, string commandName, string connectionName, long milliseconds)
            => _sqlDbCmdExecutedTrace(logger, commandName, connectionName, milliseconds, null);

        public static void TraceShardCmdExecuted(this ILogger logger, string commandName, string shardSetKey, short shardId, long milliseconds)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _sqlShardCmdExecutedTrace(logger, commandName, shardSetKey, shardId.ToString(), milliseconds, null);
            }
        }
        public static void RetryingDbConnection(this ILogger logger, string connectionName, int attemptCount, Exception exception)
            => _sqlConnectRetry(logger, connectionName, attemptCount, exception);

        //public static void RetryingDbCommand(this ILogger logger, string commandName, string connectionName, int attemptCount, Exception exception)
        //    => _sqlCommandRetry(logger, commandName, connectionName, attemptCount, exception);

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
        public static void EmptyResult(this ILogger logger, string queryName)
            => _emptyResult(logger, queryName, null);
        internal static void BatchStepStart(this ILogger logger, int stepIndex, string connectionName)
            => _batchStepStart(logger, stepIndex, connectionName, null);
        internal static void BatchStepEnd(this ILogger logger, int stepIndex, string connectionName)
            => _batchStepStart(logger, stepIndex, connectionName, null);
    }
}