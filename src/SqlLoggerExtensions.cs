using System;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Linq.Expressions;
using System.Text;

namespace ArgentSea
{

    public static class SqlLoggerExtensions
    {
        public enum EventIdentifier
        {
            LogExpression,
            MapperSqlParameterNotFound,
            MapperSqlColumnNotFound,
            MapperInParameterCacheStatus,
            MapperSetOutParameterCache,
            MapperSetOutParameterCacheStatus,
            MapperReadOutParameterCacheStatus,
            MapperReaderCacheStatus,
            MapperTvpCacheStatus,
            MapperInTrace,
            MapperTvpTrace,
            MapperSetOutTrace,
            MapperGetOutTrace,
            MapperRdrTrace,
            MapperShardKeyNull,
            MapperShardChildNull,
            LogCmdExecuted,
            LogConnectRetry,
            LogCommandRetry,
            LogCircuitBreakerOn,
            LogCircuitBreakerTest,
            LogCircuitBreakerOff
        }

        private static readonly Action<ILogger, Type, Exception> _sqlInParameterCacheMiss;
        private static readonly Action<ILogger, Type, Exception> _sqlInParameterCacheHit;
        private static readonly Action<ILogger, Type, Exception> _sqlSetOutParameterCacheMiss;
        private static readonly Action<ILogger, Type, Exception> _sqlSetOutParameterCacheHit;
        private static readonly Action<ILogger, Type, Exception> _sqlReadOutParameterCacheMiss;
        private static readonly Action<ILogger, Type, Exception> _sqlReadOutParameterCacheHit;
        private static readonly Action<ILogger, Type, Exception> _sqlReaderCacheMiss;
        private static readonly Action<ILogger, Type, Exception> _sqlReaderCacheHit;
        private static readonly Action<ILogger, Type, Exception> _sqlTvpCacheMiss;
        private static readonly Action<ILogger, Type, Exception> _sqlTvpCacheHit;
        //private static readonly Action<ILogger, string, Type, string, Exception> _sqlDelegateCreate;
        private static readonly Action<ILogger, string, PropertyInfo, Exception> _sqlDelegateParameterNotFound;
        private static readonly Action<ILogger, string, Type, Exception> _sqlDelegateFieldNotFound;
        private static readonly Action<ILogger, string, Exception> _sqlMapperInTrace;
        private static readonly Action<ILogger, string, Exception> _sqlMapperTvpTrace;
        private static readonly Action<ILogger, string, Exception> _sqlMapperSetOutTrace;
        private static readonly Action<ILogger, string, Exception> _sqlMapperGetOutTrace;
        private static readonly Action<ILogger, string, Exception> _sqlMapperRdrTrace;
        private static readonly Action<ILogger, string, byte?, int?, Exception> _sqlShardKeyNull;
        private static readonly Action<ILogger, string, byte?, int?, short?, Exception> _sqlShardChildNull;
        private static readonly Func<ILogger, Type, IDisposable> _buildInPrmExpressionsScope;
        private static readonly Func<ILogger, Type, IDisposable> _buildSetOutPrmExpressionsScope;
        private static readonly Func<ILogger, Type, IDisposable> _buildGetOutPrmExpressionsScope;
        private static readonly Func<ILogger, Type, IDisposable> _buildTvpExpressionsScope;
        private static readonly Func<ILogger, Type, IDisposable> _buildRdrExpressionsScope;
        private static readonly Func<ILogger, string, Type, IDisposable> _buildSqlResultsToObjectScope;
        private static readonly Action<ILogger, Expression, Exception> _logExpression;
        private static readonly Action<ILogger, string, Exception> _logBlockStart;
        private static readonly Action<ILogger, string, Exception> _logBlockEnd;
        private static readonly Action<ILogger, string, Exception> _logBlockNote;
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


        static SqlLoggerExtensions()
        {
            //_sqlDelegateCreate = LoggerMessage.Define<string, Type, string>(LogLevel.Debug, new EventId((int)EventIdentifier.EventDelegate, nameof(SqlDelegateCreated)), "Created delegate for {source} for object type {type}: \r\n{{{text}}}");

            _sqlInParameterCacheMiss = LoggerMessage.Define<Type>(LogLevel.Information, new EventId((int)EventIdentifier.MapperInParameterCacheStatus, nameof(SqlInParametersCacheMiss)), "No cached delegate for creating input parameters was found for type {modelT}; this is normal for the first execution.");
            _sqlInParameterCacheHit = LoggerMessage.Define<Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperInParameterCacheStatus, nameof(SqlInParametersCacheHit)), "The cached delegate for creating input parameters was found for type {modelT}.");
            _sqlSetOutParameterCacheMiss = LoggerMessage.Define<Type>(LogLevel.Information, new EventId((int)EventIdentifier.MapperSetOutParameterCacheStatus, nameof(SqlSetOutParametersCacheMiss)), "No cached delegate for creating output parameters was found for type {modelT}; this is normal for the first execution."); 
            _sqlSetOutParameterCacheHit = LoggerMessage.Define<Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperSetOutParameterCacheStatus, nameof(SqlSetOutParametersCacheHit)), "The cached delegate for creating output parameters was found for type {modelT}.");
            _sqlReadOutParameterCacheMiss = LoggerMessage.Define<Type>(LogLevel.Information, new EventId((int)EventIdentifier.MapperReadOutParameterCacheStatus, nameof(SqlSetOutParametersCacheMiss)), "No cached delegate for creating output parameters was found for type {modelT}; this is normal for the first execution."); 
            _sqlReadOutParameterCacheHit = LoggerMessage.Define<Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperReadOutParameterCacheStatus, nameof(SqlSetOutParametersCacheHit)), "The cached delegate for creating output parameters was found for type {modelT}.");
            _sqlReaderCacheMiss = LoggerMessage.Define<Type>(LogLevel.Information, new EventId((int)EventIdentifier.MapperReaderCacheStatus, nameof(SqlReaderCacheMiss)), "No cached delegate for mapping a data reader was found for type {modelT}; this is normal for the first execution.");
            _sqlReaderCacheHit = LoggerMessage.Define<Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperReaderCacheStatus, nameof(SqlReaderCacheHit)), "The cached delegate for mapping a data reader was found for type {modelT}.");
            _sqlTvpCacheMiss = LoggerMessage.Define<Type>(LogLevel.Information, new EventId((int)EventIdentifier.MapperTvpCacheStatus, nameof(SqlTvpCacheMiss)), "No cached delegate was found for mapping type {modelT} to Sql row metadata; this is normal for the first execution.");
            _sqlTvpCacheHit = LoggerMessage.Define<Type>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperTvpCacheStatus, nameof(SqlTvpCacheHit)), "A cached delegate for mapping type {modelT} to Sql row metadata was found.");
            _sqlDelegateParameterNotFound = LoggerMessage.Define<string, PropertyInfo>(LogLevel.Information, new EventId((int)EventIdentifier.MapperSqlParameterNotFound, nameof(SqlParameterNotFound)), "Sql Parameter {parameterName} was defined on {PropertyInfo.ReflectedType} but was not found among the provided output parameters.");
            _sqlDelegateFieldNotFound = LoggerMessage.Define<string, Type>(LogLevel.Information, new EventId((int)EventIdentifier.MapperSqlColumnNotFound, nameof(SqlFieldNotFound)), "Sql Parameter {parameterName} was defined on {modelT} but was not found in output parameters.");
            _sqlMapperInTrace = LoggerMessage.Define<string>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperInTrace, nameof(TraceInMapperProperty)), "In-parameter mapper is now processing property {name}.");
            _sqlMapperTvpTrace = LoggerMessage.Define<string>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperTvpTrace, nameof(TraceTvpMapperProperty)), "Tvp mapper is now processing property {name}.");
            _sqlMapperSetOutTrace = LoggerMessage.Define<string>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperSetOutTrace, nameof(TraceSetOutMapperProperty)), "Set out-parameter mapper is now processing property {name}.");
            _sqlMapperGetOutTrace = LoggerMessage.Define<string>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperGetOutTrace, nameof(TraceGetOutMapperProperty)), "Get out-parameter mapper is now processing property {name}.");
            _sqlMapperRdrTrace = LoggerMessage.Define<string>(LogLevel.Debug, new EventId((int)EventIdentifier.MapperRdrTrace, nameof(TraceRdrMapperProperty)), "Data reader field mapper is now processing property {name}.");
            _sqlShardKeyNull = LoggerMessage.Define<string, byte?, int?>(LogLevel.Information, new EventId((int)EventIdentifier.MapperShardKeyNull, nameof(TraceRdrMapperProperty)), "The {name} shard key could not be built because one of the input values was dbNull. The Shard value was {shardNumber} and the record value was {recordID}.");
            _sqlShardChildNull = LoggerMessage.Define<string, byte?, int?, short?>(LogLevel.Information, new EventId((int)EventIdentifier.MapperShardChildNull, nameof(TraceRdrMapperProperty)), "The {name} shard child could not be built because one or two of the input values was dbNull. The Shard value was {shardNumber}, the record value was {recordID}, and the child value was {childId}.");
            _buildInPrmExpressionsScope = LoggerMessage.DefineScope<Type>("Building logic to handle input parameters for model {type}");
            _buildSetOutPrmExpressionsScope = LoggerMessage.DefineScope<Type>("Building logic to set output parameters for model {type}");
            _buildGetOutPrmExpressionsScope = LoggerMessage.DefineScope<Type>("Building logic to read output parameters for model {type}");
            _buildTvpExpressionsScope = LoggerMessage.DefineScope<Type>("Building SqlMetadata convertion logic for model {type}");
            _buildRdrExpressionsScope = LoggerMessage.DefineScope<Type>("Build logic to handle a datareader for model {type}");
            _buildSqlResultsToObjectScope = LoggerMessage.DefineScope<string, Type>("Build logic to convert sql procedure {name} results to result {type}");
            _logExpression = LoggerMessage.Define<Expression>(LogLevel.Debug, new EventId((int)EventIdentifier.LogExpression, nameof(SqlExpressionLog)), "{expression.ToString()}");
            _logBlockStart = LoggerMessage.Define<string>(LogLevel.Debug, new EventId((int)EventIdentifier.LogExpression, nameof(SqlExpressionBlockStart)), "{block}");
            _logBlockEnd = LoggerMessage.Define<string>(LogLevel.Debug, new EventId((int)EventIdentifier.LogExpression, nameof(SqlExpressionBlockEnd)), "================= END {block} =================");
            _logBlockNote = LoggerMessage.Define<string>(LogLevel.Debug, new EventId((int)EventIdentifier.LogExpression, nameof(SqlExpressionBlockEnd)), "{block}");
            _sqlDbCmdExecutedTrace = LoggerMessage.Define<string, string, long>(LogLevel.Trace, new EventId((int)EventIdentifier.LogCmdExecuted, nameof(TraceDbCmdExecuted)), "Executed command {name} on Db connection {connectionName} in {milliseconds} milliseconds.");
            _sqlShardCmdExecutedTrace = LoggerMessage.Define<string, string, string, long>(LogLevel.Trace, new EventId((int)EventIdentifier.LogCmdExecuted, nameof(TraceShardCmdExecuted)), "Executed command {name} on ShardSet {shardSet} connection {shardNumber} in {milliseconds} milliseconds.");
            _sqlConnectRetry = LoggerMessage.Define<string, int>(LogLevel.Information, new EventId((int)EventIdentifier.LogConnectRetry, nameof(RetryingDbConnection)), "Initiating automatic connection retry for transient error on Db connection {connectionName}. This is attempt number {attempt}.");
            _sqlCommandRetry = LoggerMessage.Define<string, string, int>(LogLevel.Information, new EventId((int)EventIdentifier.LogCommandRetry, nameof(RetryingDbCommand)), "Initiating automatic command retry for transient error on command {name} on Db connection {connectionName}. This is attempt number {attempt}.");
            _sqlConnectionCircuitBreakerOn = LoggerMessage.Define<string>(LogLevel.Warning, new EventId((int)EventIdentifier.LogCircuitBreakerOn, nameof(CiruitBreakingDbConnection)), "Circuit breaking failing connection on Db connection {connectionName}. Most subsequent calls to this connection will fail.");
            _sqlCommandCircuitBreakerOn = LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId((int)EventIdentifier.LogCircuitBreakerOn, nameof(CiruitBreakingDbCommand)), "Circuit breaking failing command {name} on Db connection {connectionName}.");
            _sqlConnectionCircuitBreakerTest = LoggerMessage.Define<string>(LogLevel.Information, new EventId((int)EventIdentifier.LogCircuitBreakerTest, nameof(CiruitBrokenDbConnectionTest)), "Circuit broken connection {connectionName} is being retested.");
            _sqlCommandCircuitBreakerTest = LoggerMessage.Define<string, string>(LogLevel.Information, new EventId((int)EventIdentifier.LogCircuitBreakerTest, nameof(CiruitBrokenDbCommandTest)), "Circuit broken command {name} on Db connection {connectionName} is being retested.");
            _sqlConnectionCircuitBreakerOff = LoggerMessage.Define<string>(LogLevel.Information, new EventId((int)EventIdentifier.LogCircuitBreakerOff, nameof(CiruitBrokenDbConnectionRestored)), "Circuit broken connection {connectionName} is restored.");
            _sqlCommandCircuitBreakerOff = LoggerMessage.Define<string, string>(LogLevel.Information, new EventId((int)EventIdentifier.LogCircuitBreakerOff, nameof(CiruitBrokenDbCommandRestored)), "Circuit broken command {name} on Db connection {connectionName} is restored.");

        }
        public static void SqlExpressionLog(this ILogger logger, Expression expression)
        {
            _logExpression(logger, expression, null);
        }
        public static void SqlExpressionBlockStart(this ILogger logger, string blockName, ParameterExpression[] parameters)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                var sb = new StringBuilder();
                sb.Append("start procedure ");
                sb.Append(blockName);
                sb.Append(" (");
                var seperator = string.Empty;
                foreach(var prm in parameters)
                {
                    sb.Append(seperator);
                    seperator = ", ";
                    sb.Append(prm.Type.ToString());
                    sb.Append(" ");
                    sb.Append(prm.Name);
                }
                sb.Append(") {");
                _logBlockStart(logger, sb.ToString(), null);
            }
        }
        public static void SqlExpressionBlockEnd(this ILogger logger, string blockName)
        {
                _logBlockEnd(logger, blockName, null);
        }
        public static void SqlExpressionNote(this ILogger logger, string sectionName)
        {
            _logBlockNote(logger, sectionName, null);
        }
        public static void SqlParameterNotFound(this ILogger logger, string parameterName, PropertyInfo prop)
        {
            _sqlDelegateParameterNotFound(logger, parameterName, prop, null);
        }
        public static void SqlFieldNotFound(this ILogger logger, string columnName, Type modelT)
        {
            _sqlDelegateFieldNotFound(logger, columnName, modelT, null);
        }
        public static void SqlInParametersCacheHit(this ILogger logger, Type modelT)
        {
            _sqlInParameterCacheHit(logger, modelT, null);
        }
        public static void SqlInParametersCacheMiss(this ILogger logger, Type modelT)
        {
            _sqlInParameterCacheMiss(logger, modelT, null);
        }
        public static void SqlSetOutParametersCacheHit(this ILogger logger, Type modelT)
        {
            _sqlSetOutParameterCacheHit(logger, modelT, null);
        }
        public static void SqlSetOutParametersCacheMiss(this ILogger logger, Type modelT)
        {
            _sqlSetOutParameterCacheMiss(logger, modelT, null);
        }
        public static void SqlReadOutParametersCacheHit(this ILogger logger, Type modelT)
        {
            _sqlReadOutParameterCacheHit(logger, modelT, null);
        }
        public static void SqlReadOutParametersCacheMiss(this ILogger logger, Type modelT)
        {
            _sqlReadOutParameterCacheMiss(logger, modelT, null);
        }
        public static void SqlReaderCacheMiss(this ILogger logger, Type modelT)
        {
            _sqlReaderCacheMiss(logger, modelT, null);
        }
        public static void SqlReaderCacheHit(this ILogger logger, Type modelT)
        {
            _sqlReaderCacheHit(logger, modelT, null);
        }
        public static void SqlTvpCacheMiss(this ILogger logger, Type modelT)
        {
            _sqlTvpCacheMiss(logger, modelT, null);
        }
        public static void SqlTvpCacheHit(this ILogger logger, Type modelT)
        {
            _sqlTvpCacheHit(logger, modelT, null);
        }
        public static void TraceInMapperProperty(this ILogger logger, string propertyName)
        {
            _sqlMapperInTrace(logger, propertyName, null);
        }
        public static void TraceTvpMapperProperty(this ILogger logger, string propertyName)
        {
            _sqlMapperTvpTrace(logger, propertyName, null);
        }
        public static void TraceSetOutMapperProperty(this ILogger logger, string propertyName)
        {
            _sqlMapperSetOutTrace(logger, propertyName, null);
        }
        public static void TraceGetOutMapperProperty(this ILogger logger, string propertyName)
        {
            _sqlMapperGetOutTrace(logger, propertyName, null);
        }
        public static void TraceRdrMapperProperty(this ILogger logger, string propertyName)
        {
            _sqlMapperRdrTrace(logger, propertyName, null);
        }
        public static void NullShardKeyArguments(this ILogger logger, string propertyName, byte? shardNumber, int? recordID)
        {
            if (shardNumber.HasValue != recordID.HasValue) //if some args have values and some do not, we should log (i.e. we'd expect all nulls or all values)
            {
                _sqlShardKeyNull(logger, propertyName, shardNumber, recordID, null);
            }
        }
        public static void NullShardChildArguments(this ILogger logger, string propertyName, byte? shardNumber, int? recordID, short? childId)
        {
            if (!((shardNumber.HasValue && recordID.HasValue && childId.HasValue) || (!shardNumber.HasValue && !recordID.HasValue && !childId.HasValue))) //if some args have values and some do not, we should log (i.e. we'd expect all nulls or all values)
            {
                _sqlShardChildNull(logger, propertyName, shardNumber, recordID, childId, null);
            }
        }
        public static IDisposable BuildInParameterScope (this ILogger logger, Type model)
        {
            return _buildInPrmExpressionsScope(logger, model);
        }
        public static IDisposable BuildSetOutParameterScope(this ILogger logger, Type model)
        {
            return _buildSetOutPrmExpressionsScope(logger, model);
        }
        public static IDisposable BuildGetOutParameterScope(this ILogger logger, Type model)
        {
            return _buildGetOutPrmExpressionsScope(logger, model);
        }
        public static IDisposable BuildTvpScope(this ILogger logger, Type model)
        {
            return _buildTvpExpressionsScope(logger, model);
        }
        public static IDisposable BuildRdrScope(this ILogger logger, Type model)
        {
            return _buildRdrExpressionsScope(logger, model);
        }
        public static IDisposable BuildSqlResultsToObjectScope(this ILogger logger, string procedureName, Type model)
        {
            return _buildSqlResultsToObjectScope(logger, procedureName, model);
        }
        public static void TraceDbCmdExecuted(this ILogger logger, string commandName, string connectionName, long milliseconds)
        {
            _sqlDbCmdExecutedTrace(logger, commandName, connectionName, milliseconds, null);
        }
        public static void TraceShardCmdExecuted<TShard>(this ILogger logger, string commandName, string shardSetKey, TShard shardNumber, long milliseconds)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                _sqlShardCmdExecutedTrace(logger, commandName, shardSetKey, shardNumber.ToString(), milliseconds, null);
            }
        }
        public static void RetryingDbConnection(this ILogger logger, string connectionName, int attemptCount, Exception exception)
        {
            _sqlConnectRetry(logger, connectionName, attemptCount, exception);
        }
        public static void RetryingDbCommand(this ILogger logger, string commandName, string connectionName, int attemptCount, Exception exception)
        {
            _sqlCommandRetry(logger, commandName, connectionName, attemptCount, exception);
        }
        public static void CiruitBreakingDbConnection(this ILogger logger, string connectionName)
        {
            _sqlConnectionCircuitBreakerOn(logger, connectionName, null);
        }
        public static void CiruitBreakingDbCommand(this ILogger logger, string commandName, string connectionName)
        {
            _sqlCommandCircuitBreakerOn(logger, commandName, connectionName, null);
        }
        public static void CiruitBrokenDbConnectionTest(this ILogger logger, string connectionName)
        {
            _sqlConnectionCircuitBreakerTest(logger, connectionName, null);
        }
        public static void CiruitBrokenDbCommandTest(this ILogger logger, string commandName, string connectionName)
        {
            _sqlCommandCircuitBreakerTest(logger, commandName, connectionName, null);
        }
        public static void CiruitBrokenDbConnectionRestored(this ILogger logger, string connectionName)
        {
            _sqlConnectionCircuitBreakerOff(logger, connectionName, null);
        }
        public static void CiruitBrokenDbCommandRestored(this ILogger logger, string commandName, string connectionName)
        {
            _sqlCommandCircuitBreakerOff(logger, commandName, connectionName, null);
        }
    }
}
