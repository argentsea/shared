using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Globalization;
using Microsoft.Extensions.Logging;
using System.Linq;
using ArgentSea;

namespace ArgentSea
{
    public static class Mapper
    {
		private static ConcurrentDictionary<Type, Action<DbParameterCollection, HashSet<string>, ILogger, object>> _cacheInParamSet = new ConcurrentDictionary<Type, Action<DbParameterCollection, HashSet<string>, ILogger, object>>();
		private static ConcurrentDictionary<Type, Action<DbParameterCollection, HashSet<string>, ILogger>> _cacheOutParamSet = new ConcurrentDictionary<Type, Action<DbParameterCollection, HashSet<string>, ILogger>>();
		private static ConcurrentDictionary<Type, Func<DbDataReader, ILogger, object>> _getRdrParamCache = new ConcurrentDictionary<Type, Func<DbDataReader, ILogger, object>>();
		private static ConcurrentDictionary<Type, Func<DbParameterCollection, ILogger, object>> _getOutParamReadCache = new ConcurrentDictionary<Type, Func<DbParameterCollection, ILogger, object>>();
		//private static ConcurrentDictionary<string, Func<string, dynamic, dynamic, dynamic, dynamic, dynamic, dynamic, dynamic, dynamic, dynamic, ILogger, dynamic>> _getObjectCache = new ConcurrentDictionary<string, Func<string, dynamic, dynamic, dynamic, dynamic, dynamic, dynamic, dynamic, dynamic, dynamic, ILogger, dynamic>>();
		private static ConcurrentDictionary<string, Delegate> _getObjectCache = new ConcurrentDictionary<string, Delegate>();

		#region Public methods

		/// <summary>
		/// Accepts a Sql Parameter collection and appends Sql input parameters whose values correspond to the provided object properties and MapTo attributes.
		/// </summary>
		/// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql metadata and columns.</typeparam>
		/// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object.</param>
		/// <param name="model">An object model instance. The property values are use as parameter values.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		public static DbParameterCollection MapToInParameters<T>(this DbParameterCollection parameters, T model, ILogger logger) where T : class
			=> MapToInParameters<T>(parameters, model, null, logger);

		/// <summary>
		/// Accepts a Sql Parameter collection and appends Sql input parameters whose values correspond to the provided object properties and MapTo attributes.
		/// </summary>
		/// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql metadata and columns.</typeparam>
		/// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object.</param>
		/// <param name="model">An object model instance. The property values are use as parameter values.</param>
		/// <param name="ignoreParameters">A lists of parameter names that should not be created. Each entry must exactly match the parameter name, including prefix and casing.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		public static DbParameterCollection MapToInParameters<TModel>(this DbParameterCollection parameters, TModel model, HashSet<string> ignoreParameters, ILogger logger) where TModel : class
		{
			if (ignoreParameters is null)
			{
				ignoreParameters = new HashSet<string>();
			}
			var typeModel = typeof(TModel);
			if (!_cacheInParamSet.TryGetValue(typeModel, out var SqlParameterDelegates))
			{
				LoggingExtensions.SqlInParametersCacheMiss(logger, typeModel);
				SqlParameterDelegates = BuildInMapDelegate(typeModel, logger);
				if (!_cacheInParamSet.TryAdd(typeModel, SqlParameterDelegates))
				{
					SqlParameterDelegates = _cacheInParamSet[typeModel];
				}
			}
			else
			{
				LoggingExtensions.SqlInParametersCacheHit(logger, typeModel);
			}
			foreach (DbParameter prm in parameters)
			{
				if (!ignoreParameters.Contains(prm.ParameterName))
				{
					ignoreParameters.Add(prm.ParameterName);
				}
			}
			SqlParameterDelegates(parameters, ignoreParameters, logger, model);
			return parameters;
		}

		/// <summary>
		/// Accepts a Sql Parameter collection and appends Sql output parameters corresponding to the MapTo attributes.
		/// </summary>
		/// <param name="parameters">A parameter collection, possibly belonging to a ADO.Net Command object or a QueryParmaters object.</param>
		/// <param name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql parameter types.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		/// <returns></returns>
		public static DbParameterCollection MapToOutParameters(this DbParameterCollection parameters, Type TModel, ILogger logger)
			=> MapToOutParameters(parameters, TModel, null, logger);

		/// <summary>
		/// Accepts a Sql Parameter collection and appends Sql output parameters corresponding to the MapTo attributes.
		/// </summary>
		/// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql parameter types.</typeparam>
		/// <param name="parameters">A parameter collection, possibly belonging to a ADO.Net Command object or a QueryParmaters object.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		/// <returns>The DbParameterCollection, enabling a fluent API.</returns>
		public static DbParameterCollection MapToOutParameters<TModel>(this DbParameterCollection parameters, ILogger logger)
			=> MapToOutParameters(parameters, typeof(TModel), null, logger);

		/// <summary>
		/// Accepts a Sql Parameter collection and appends Sql output parameters corresponding to the MapTo attributes.
		/// </summary>
		/// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql parameter types.</typeparam>
		/// <param name="parameters">A parameter collection, possibly belonging to a ADO.Net Command object or a QueryParmaters object.</param>
		/// <param name="ignoreParameters">A lists of parameter names that should not be created.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		/// <returns>The DbParameterCollection, enabling a fluent API.</returns>
		public static DbParameterCollection MapToOutParameters<TModel>(this DbParameterCollection parameters, HashSet<string> ignoreParameters, ILogger logger)
			=> MapToOutParameters(parameters, typeof(TModel), null, logger);

		/// <summary>
		/// Accepts a Sql Parameter collection and appends Sql output parameters corresponding to the MapTo attributes.
		/// </summary>
		/// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql parameter types.</typeparam>
		/// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object.</param>
		/// <param name="model">The type of the model.</param>
		/// <param name="ignoreParameters">A lists of parameter names that should not be created.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		public static DbParameterCollection MapToOutParameters(this DbParameterCollection parameters, Type TModel, HashSet<string> ignoreParameters, ILogger logger)
		{
			//For each paramater, Expression Tree does the following:
			//ArgentSea.LoggingExtensions.TraceSetOutMapperProperty(logger, "ParameterName");
			//if (ArgentSea.ExpressionHelpers.DontIgnoreThisParameter("@ParameterName", ignoreParameters))
			//{
			//	ArgentSea.Sql.SqlParameterCollectionExtensions.AddSql---OutParameter(parameters, "@ParameterName");
			//}

			if (ignoreParameters is null)
			{
				ignoreParameters = new HashSet<string>();
			}
			if (!_cacheOutParamSet.TryGetValue(TModel, out var SqlParameterDelegates))
			{
				LoggingExtensions.SqlSetOutParametersCacheMiss(logger, TModel);
				SqlParameterDelegates = BuildOutSetDelegate(TModel, logger);
				if (!_cacheOutParamSet.TryAdd(TModel, SqlParameterDelegates))
				{
					SqlParameterDelegates = _cacheOutParamSet[TModel];
					LoggingExtensions.SqlSetOutParametersCacheMiss(logger, TModel);
				}
			}
			else
			{
				LoggingExtensions.SqlSetOutParametersCacheHit(logger, TModel);
			}
			foreach (DbParameter prm in parameters)
			{

				if (!ignoreParameters.Contains(prm.ParameterName))
				{
					ignoreParameters.Add(prm.ParameterName);
				}
			}
			SqlParameterDelegates(parameters, ignoreParameters, logger);
			return parameters;
		}

		/// <summary>
		/// Creates a new object with property values based upon the provided output parameters which correspond to the MapTo attributes.
		/// </summary>
		/// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to read the Sql parameter collection values.</typeparam>
		/// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object after a database query.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		/// <returns>An object of the specified type, with properties set to parameter values.</returns>
		public static TModel ReadOutParameters<TModel>(this DbParameterCollection parameters, ILogger logger) where TModel : class, new()
		{
			var T = typeof(TModel);
			if (!_getOutParamReadCache.TryGetValue(T, out var SqlOutDelegate))
			{
				LoggingExtensions.SqlReadOutParametersCacheMiss(logger, T);
				SqlOutDelegate = BuildOutGetDelegate(parameters, typeof(TModel), logger);
				if (!_getOutParamReadCache.TryAdd(typeof(TModel), SqlOutDelegate))
				{
					SqlOutDelegate = _getOutParamReadCache[typeof(TModel)];
				}
			}
			else
			{
				LoggingExtensions.SqlReadOutParametersCacheHit(logger, T);
			}
			return (TModel)SqlOutDelegate(parameters, logger);
		}

		/// <summary>
		/// Accepts a data reader object and returns a list of objects of the specified type, one for each record.
		/// </summary>
		/// <typeparam name="TModel">The type of the list result</typeparam>
		/// <param name="rdr">The data reader, set to the current result set.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		/// <returns>A list of objects of the specified type, one for each result.</returns>
		public static IList<TModel> FromDataReader<TModel>(DbDataReader rdr, ILogger logger) where TModel : class, new()
		{
			//Q: why don't we accept a List<T> argument and populate that, rather than create a new list?
			//  This would allow us to pass in list properties of parent objects.
			//A: Because the parent object is probably created with output parameters, which come at the end of thd TDS stream.
			//  So the parent object data wouldn't exist when we needed to populate the properties
			//  This way: just collect the list data in a variable set the property when its ready.
			if (rdr is null)
			{
				throw new ArgumentNullException(nameof(rdr));
			}
			if (rdr.IsClosed)
			{
				throw new Exception("The data reader has been closed.");
			}
			if (!rdr.HasRows)
			{
				return new List<TModel>();
			}
			var T = typeof(TModel);
			if (!_getRdrParamCache.TryGetValue(typeof(TModel), out var SqlRdrDelegate))
			{
				LoggingExtensions.SqlReaderCacheMiss(logger, T);
				SqlRdrDelegate = BuildReaderMapDelegate<TModel>(logger);
				if (!_getRdrParamCache.TryAdd(typeof(TModel), SqlRdrDelegate))
				{
					SqlRdrDelegate = _getRdrParamCache[typeof(TModel)];
				}
			}
			else
			{
				LoggingExtensions.SqlReaderCacheHit(logger, T);
			}
			return (List<TModel>)SqlRdrDelegate(rdr, logger);
		}

		#endregion
		#region delegate builders
		private static Action<DbParameterCollection, HashSet<string>, ILogger, object> BuildInMapDelegate(Type TModel, ILogger logger)
		{
			var expressions = new List<Expression>();
			//Create the two delegate parameters: in: sqlParametersCollection and model instance; out: sqlParametersCollection
			ParameterExpression prmSqlPrms = Expression.Variable(typeof(DbParameterCollection), "parameters");
			ParameterExpression prmObjInstance = Expression.Parameter(typeof(object), "model");
			ParameterExpression expIgnoreParameters = Expression.Parameter(typeof(HashSet<string>), "ignoreParameters");
			ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var exprInPrms = new ParameterExpression[] { prmSqlPrms, expIgnoreParameters, expLogger, prmObjInstance };

			using (logger.BuildInParameterScope(TModel))
			{
				var prmTypedInstance = Expression.TypeAs(prmObjInstance, TModel);  //Our cached delegates accept neither generic nor dynamic arguments. We have to pass object then cast.
				var noDupPrmNameList = new HashSet<string>();
				//var miLogTrace = typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.TraceInMapperProperty));
				logger.SqlExpressionBlockStart(nameof(Mapper.MapToInParameters), exprInPrms);
				expressions.Add(prmTypedInstance);
				logger.SqlExpressionLog(prmTypedInstance);
				IterateInMapProperties(TModel, expressions, prmSqlPrms, prmTypedInstance, expIgnoreParameters, expLogger, noDupPrmNameList, logger);
				logger.SqlExpressionBlockEnd(nameof(Mapper.MapToInParameters));
			}
			var inBlock = Expression.Block(expressions);
			var lmbIn = Expression.Lambda<Action<DbParameterCollection, HashSet<string>, ILogger, object>>(inBlock, exprInPrms);
			return lmbIn.Compile();
		}
		private static void IterateInMapProperties(Type TModel, List<Expression> expressions, ParameterExpression prmSqlPrms, Expression prmTypedInstance, ParameterExpression expIgnoreParameters, ParameterExpression expLogger, HashSet<string> noDupPrmNameList, ILogger logger)
		{
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceInMapperProperty));
			foreach (var prop in TModel.GetProperties())
			{
				//Does property have our SqlMapAttribute attribute?
				if (prop.IsDefined(typeof(ParameterMapAttribute), true))
				{
					//Instantiate our SqlMapAttribute attribute
					var attrPMs = prop.GetCustomAttributes<ParameterMapAttribute>(true);
					foreach (var attrPM in attrPMs)
					{
						var expLog = Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name));
						expressions.Add(expLog);
						logger.SqlExpressionLog(expLog);
						if (!attrPM.IsValidType(prop.PropertyType))
						{
							throw new InvalidMapTypeException(prop, attrPM.SqlType);
						}
						MemberExpression expProperty = Expression.Property(prmTypedInstance, prop);
						attrPM.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expProperty, prop.PropertyType, expLogger, logger);
					}
				}
				else if (prop.IsDefined(typeof(MapToModel)) && !prop.PropertyType.IsValueType)
				{
					MemberExpression expProperty = Expression.Property(prmTypedInstance, prop);
					IterateInMapProperties(prop.PropertyType, expressions, prmSqlPrms, expProperty, expIgnoreParameters, expLogger, noDupPrmNameList, logger);
				}
			}
		}

		private static Action<DbParameterCollection, HashSet<string>, ILogger> BuildOutSetDelegate(Type TModel, ILogger logger)
		{
			var expressions = new List<Expression>();

			//Create the two delegate parameters: in: sqlParametersCollection and model instance; out: sqlParametersCollection
			ParameterExpression prmSqlPrms = Expression.Variable(typeof(DbParameterCollection), "parameters");
			ParameterExpression expIgnoreParameters = Expression.Parameter(typeof(HashSet<string>), "ignoreParameters");
			ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var noDupPrmNameList = new HashSet<string>();

			var exprOutPrms = new ParameterExpression[] { prmSqlPrms, expIgnoreParameters, expLogger };

			IterateSetOutParameters(TModel, expressions, prmSqlPrms, expIgnoreParameters, expLogger, noDupPrmNameList, logger);

			if (logger.IsEnabled(LogLevel.Debug))
			{
				using (logger.BuildSetOutParameterScope(TModel))
				{
					logger.SqlExpressionBlockStart(nameof(Mapper.MapToOutParameters), exprOutPrms);
					foreach (var exp in expressions)
					{
						logger.SqlExpressionLog(exp);
					}
					logger.SqlExpressionBlockEnd(nameof(Mapper.MapToOutParameters));
				}
			}
			var outBlock = Expression.Block(expressions);
			var lmbOut = Expression.Lambda<Action<DbParameterCollection, HashSet<string>, ILogger>>(outBlock, exprOutPrms);
			return lmbOut.Compile();
		}
		private static void IterateSetOutParameters(Type TModel, List<Expression> expressions, ParameterExpression prmSqlPrms, ParameterExpression expIgnoreParameters, ParameterExpression expLogger, HashSet<string> noDupPrmNameList, ILogger logger)
		{
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceSetOutMapperProperty));
			//Loop through all object properties:
			foreach (var prop in TModel.GetProperties())
			{
				//Does property have our SqlMapAttribute attribute?
				if (prop.IsDefined(typeof(ParameterMapAttribute), true))
				{
					var attrPMs = prop.GetCustomAttributes<ParameterMapAttribute>(true);
					foreach (var attrPM in attrPMs)
					{
						expressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name)));
						if (!attrPM.IsValidType(prop.PropertyType))
						{
							throw new InvalidMapTypeException(prop, attrPM.SqlType);
						}
						attrPM.AppendSetOutParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, prop.PropertyType, expLogger, logger);
					}
				}
				else if (prop.IsDefined(typeof(MapToModel)) && !prop.PropertyType.IsValueType)
				{
					IterateSetOutParameters(prop.PropertyType, expressions, prmSqlPrms, expIgnoreParameters, expLogger, noDupPrmNameList, logger);
				}
			}
		}


		private static Func<DbParameterCollection, ILogger, object> BuildOutGetDelegate(DbParameterCollection parameters, Type TModel, ILogger logger)
		{
			ParameterExpression expPrms = Expression.Parameter(typeof(DbParameterCollection), "parameters");
			ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var variableExpressions = new List<ParameterExpression>();
			var expModel = Expression.Variable(TModel, "model"); //list result variable
			variableExpressions.Add(expModel);
			var expPrm = Expression.Variable(typeof(DbParameter), "prm");
			variableExpressions.Add(expPrm);
			var expressionPrms = new ParameterExpression[] { expPrms, expLogger };


			var expressions = new List<Expression>()
			{
				Expression.Assign(expModel, Expression.New(TModel)) // var result = new <T>;
            };
			using (logger.BuildGetOutParameterScope(TModel))
			{
				logger.SqlExpressionBlockStart(nameof(Mapper.ReadOutParameters), expressionPrms);
				//Loop through all object properties:

				IterateGetOutParameters(TModel, expPrms, expPrm, variableExpressions, expressions, expModel, expLogger, logger);

				expressions.Add(expModel); //return value;
				logger.SqlExpressionLog(expModel);
				logger.SqlExpressionBlockEnd(nameof(Mapper.ReadOutParameters));
			}
			var expBlock = Expression.Block(variableExpressions, expressions);
			var lambda = Expression.Lambda<Func<DbParameterCollection, ILogger, object>>(expBlock, expressionPrms);
			return lambda.Compile();
		}
		private static void IterateGetOutParameters(Type TModel, ParameterExpression expPrms, ParameterExpression expPrm, List<ParameterExpression> variableExpressions, List<Expression> expressions, Expression expModel, ParameterExpression expLogger, ILogger logger)
		{
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceGetOutMapperProperty));

			foreach (var prop in TModel.GetProperties())
			{
				if (prop.IsDefined(typeof(ParameterMapAttribute), true))
				{
					var attrPMs = prop.GetCustomAttributes<ParameterMapAttribute>(true);
					MemberExpression expProperty = Expression.Property(expModel, prop);

					foreach (var attrPM in attrPMs)
					{
						var expCallLog = Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name));
						expressions.Add(expCallLog);
						logger.SqlExpressionLog(expCallLog);

						if (!attrPM.IsValidType(prop.PropertyType))
						{
							throw new InvalidMapTypeException(prop, attrPM.SqlType);
						}

						attrPM.AppendReadOutParameterExpressions(expProperty, expressions, expPrms, expPrm, prop, expLogger, logger);
					}
				}
				else if (prop.IsDefined(typeof(MapToModel)) && !prop.PropertyType.IsValueType)
				{
					MemberExpression expProperty = Expression.Property(expModel, prop);
					IterateGetOutParameters(prop.PropertyType, expPrms, expPrm, variableExpressions, expressions, expProperty, expLogger, logger);
				}
			}
		}

		private static Func<DbDataReader, ILogger, List<T>> BuildReaderMapDelegate<T>(ILogger logger)
		{
			var TModel = typeof(T);

			var expressions = new List<Expression>();
			var columnLookupExpressions = new List<MethodCallExpression>();

			ParameterExpression prmSqlRdr = Expression.Parameter(typeof(DbDataReader), "rdr"); //input param
			ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var expressionPrms = new ParameterExpression[] { prmSqlRdr, expLogger };

			//MethodInfos for subsequent Expression calls
			var miGetFieldOrdinal = typeof(ExpressionHelpers).GetMethod(nameof(ExpressionHelpers.GetFieldOrdinal), BindingFlags.NonPublic | BindingFlags.Static);
			var miRead = typeof(DbDataReader).GetMethod(nameof(DbDataReader.Read));
			var miGetFieldValue = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue));
			var miListAdd = typeof(List<T>).GetMethod(nameof(List<T>.Add));

			//create List<TModel> result
			var expModel = Expression.Parameter(typeof(T), "model"); //model variable
			var expListResult = Expression.Parameter(typeof(List<T>), "result"); //list result variable
			var expOrdinal = Expression.Variable(typeof(int), "ordinal");
			var expOrdinals = Expression.Variable(typeof(int[]), "ordinals");

			var propIndex = 0;
			var resultExpressions = new List<Expression>();
			logger.SqlExpressionNote("while (rdr.Read())");
			using (logger.BuildRdrScope(TModel))
			{
				logger.SqlExpressionBlockStart(nameof(Mapper.FromDataReader), expressionPrms);
				var expAssign = Expression.Assign(expModel, Expression.New(typeof(T)));
				expressions.Add(expAssign); //var model = new T
				logger.SqlExpressionLog(expAssign);

				//Loop through all object properties:
				IterateRdrColumns(TModel, expModel, columnLookupExpressions, expressions, prmSqlRdr, expOrdinals, expOrdinal, ref propIndex, expLogger, logger);

				var expAddList = Expression.Call(expListResult, miListAdd, expModel);
				expressions.Add(expAddList); //ResultList.Add(model);
				logger.SqlExpressionLog(expAddList);

				logger.SqlExpressionNote($"Out-of-order expressions which should appear at the beginning of {nameof(Mapper.FromDataReader)}:");
				resultExpressions.Add(Expression.Assign(expOrdinals, Expression.NewArrayInit(typeof(int), columnLookupExpressions.ToArray())));
				logger.SqlExpressionNote($"End Out-of-order expression");

				var loopLabel = Expression.Label("readNextRow");
				resultExpressions.Add(Expression.Assign(expListResult, Expression.New(typeof(List<T>)))); // var result = new List<T>;
				resultExpressions.Add(Expression.Loop(
						Expression.IfThenElse(Expression.Call(prmSqlRdr, miRead),
							Expression.Block(expressions),
							Expression.Break(loopLabel)
						), loopLabel));
				logger.SqlExpressionNote("end while");
				resultExpressions.Add(expListResult); //return type
				logger.SqlExpressionLog(expListResult);
			}

			var expBlock = Expression.Block(typeof(List<T>), new ParameterExpression[] { expModel, expListResult, expOrdinals, expOrdinal }, resultExpressions);
			var lambda = Expression.Lambda<Func<DbDataReader, ILogger, List<T>>>(expBlock, expressionPrms);
			return lambda.Compile();
		}
		private static void IterateRdrColumns(Type TModel, Expression expModel, List<MethodCallExpression> columnLookupExpressions, List<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, ParameterExpression expLogger, ILogger logger)
		{
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceRdrMapperProperty));
			foreach (var prop in TModel.GetProperties())
			{
				if (prop.IsDefined(typeof(ParameterMapAttribute), true))
				{
					var attrPMs = prop.GetCustomAttributes<ParameterMapAttribute>(true);
					foreach (var attrPM in attrPMs)
					{
						var expTrace = Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name));
						expressions.Add(expTrace);
						logger.SqlExpressionLog(expTrace);

						if (!attrPM.IsValidType(prop.PropertyType))
						{
							throw new InvalidMapTypeException(prop, attrPM.SqlType);
						}
						MemberExpression expProperty = Expression.Property(expModel, prop);
						attrPM.AppendReaderExpressions(expProperty, columnLookupExpressions, expressions, prmSqlRdr, expOrdinals, expOrdinal, ref propIndex, prop, expLogger, logger);
					}
					propIndex++;
				}
				else if (prop.IsDefined(typeof(MapToModel)) && !prop.PropertyType.IsValueType)
				{
					MemberExpression expProperty = Expression.Property(expModel, prop);
					IterateRdrColumns(prop.PropertyType, expProperty, columnLookupExpressions, expressions, prmSqlRdr, expOrdinals, expOrdinal, ref propIndex, expLogger, logger);
				}
			}

		}
		#endregion
		#region Convert Sql result to object(s)

		//Make model out of out parameters only
		public static TModel QueryResultsHandler<TShard, TModel>
			(
			TShard shardNumber,
			string sprocName,
			object notUsed,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TModel : class, new()
			=> QueryResultsHandler<TShard, TModel, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, TModel>(shardNumber, sprocName, null, rdr, parameters, connectionDescription, logger);

		//To Make model out of reader result, set TOutParamaters type to DummyType or something like it.
		public static TModel QueryResultsHandler<TShard, TModel, TReaderResult, TOutParameters>
			(
			TShard shardNumber,
			string sprocName,
			object notUsed,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TReaderResult : class, new()
			where TOutParameters : class, new()
			where TModel : class, new()
			=> QueryResultsHandler<TShard, TModel, TReaderResult, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, TOutParameters>(shardNumber, sprocName, null, rdr, parameters, connectionDescription, logger);

		public static TModel QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TOutParameters>
			(
			TShard shardNumber,
			string sprocName,
			object notUsed,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TReaderResult0 : class, new()
			where TReaderResult1 : class, new()
			where TOutParameters : class, new()
			where TModel : class, new()
			=> QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, TOutParameters>(shardNumber, sprocName, null, rdr, parameters, connectionDescription, logger);

		public static TModel QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TOutParameters>
			(
			TShard shardNumber,
			string sprocName,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TReaderResult0 : class, new()
			where TReaderResult1 : class, new()
			where TReaderResult2 : class, new()
			where TOutParameters : class, new()
			where TModel : class, new()
			=> QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, DummyType, DummyType, DummyType, DummyType, DummyType, TOutParameters>(shardNumber, sprocName, null, rdr, parameters, connectionDescription, logger);

		public static TModel QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TOutParameters>
			(
			TShard shardNumber,
			string sprocName,
			object notUsed,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TReaderResult0 : class, new()
			where TReaderResult1 : class, new()
			where TReaderResult2 : class, new()
			where TReaderResult3 : class, new()
			where TOutParameters : class, new()
			where TModel : class, new()
			=> QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, DummyType, DummyType, DummyType, DummyType, TOutParameters>(shardNumber, sprocName, null, rdr, parameters, connectionDescription, logger);

		public static TModel QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TOutParameters>
			(
			TShard shardNumber,
			string sprocName,
			object notUsed,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TReaderResult0 : class, new()
			where TReaderResult1 : class, new()
			where TReaderResult2 : class, new()
			where TReaderResult3 : class, new()
			where TReaderResult4 : class, new()
			where TOutParameters : class, new()
			where TModel : class, new()
			=> QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, DummyType, DummyType, DummyType, TOutParameters>(shardNumber, sprocName, null, rdr, parameters, connectionDescription, logger);

		public static TModel QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TOutParameters>
			(
			TShard shardNumber,
			string sprocName,
			object notUsed,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TReaderResult0 : class, new()
			where TReaderResult1 : class, new()
			where TReaderResult2 : class, new()
			where TReaderResult3 : class, new()
			where TReaderResult4 : class, new()
			where TReaderResult5 : class, new()
			where TOutParameters : class, new()
			where TModel : class, new()
			=> QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, DummyType, DummyType, TOutParameters>(shardNumber, sprocName, null, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel>
		//	(
		//	TShard shardNumber,
		//	string sprocName,
		//	TArg optionalArgument,
		//	DbDataReader rdr,
		//	DbParameterCollection parameters,
		//	string connectionDescription,
		//	ILogger logger)
		//	where TModel : class, new()
		//	=> QueryResultsHandler<TShard, TArg, TModel, TModel, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType>(shardNumber, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1>
		//	(
		//	TShard shardNumber,
		//	string sprocName,
		//	TArg optionalArgument,
		//	DbDataReader rdr,
		//	DbParameterCollection parameters,
		//	string connectionDescription,
		//	ILogger logger)
		//	where TReaderResult0 : class, new()
		//	where TReaderResult1 : class, new()
		//	where TModel : class, new()
		//	=> QueryResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType>(shardNumber, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2>
		//	(
		//	TShard shardNumber,
		//	string sprocName,
		//	TArg optionalArgument,
		//	DbDataReader rdr,
		//	DbParameterCollection parameters,
		//	string connectionDescription,
		//	ILogger logger)
		//	where TReaderResult0 : class, new()
		//	where TReaderResult1 : class, new()
		//	where TReaderResult2 : class, new()
		//	where TModel : class, new()
		//	=> QueryResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType>(shardNumber, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
		//	(
		//	TShard shardNumber,
		//	string sprocName,
		//	TArg optionalArgument,
		//	DbDataReader rdr,
		//	DbParameterCollection parameters,
		//	string connectionDescription,
		//	ILogger logger)
		//	where TReaderResult0 : class, new()
		//	where TReaderResult1 : class, new()
		//	where TReaderResult2 : class, new()
		//	where TReaderResult3 : class, new()
		//	where TModel : class, new()
		//	=> QueryResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, DummyType, DummyType, DummyType, DummyType, DummyType>(shardNumber, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
		//	(
		//	TShard shardNumber,
		//	string sprocName,
		//	TArg optionalArgument,
		//	DbDataReader rdr,
		//	DbParameterCollection parameters,
		//	string connectionDescription,
		//	ILogger logger)
		//	where TReaderResult0 : class, new()
		//	where TReaderResult1 : class, new()
		//	where TReaderResult2 : class, new()
		//	where TReaderResult3 : class, new()
		//	where TReaderResult4 : class, new()
		//	where TModel : class, new()
		//	=> QueryResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, DummyType, DummyType, DummyType, DummyType>(shardNumber, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
		//	(
		//	TShard shardNumber,
		//	string sprocName,
		//	TArg optionalArgument,
		//	DbDataReader rdr,
		//	DbParameterCollection parameters,
		//	string connectionDescription,
		//	ILogger logger)
		//	where TReaderResult0 : class, new()
		//	where TReaderResult1 : class, new()
		//	where TReaderResult2 : class, new()
		//	where TReaderResult3 : class, new()
		//	where TReaderResult4 : class, new()
		//	where TReaderResult5 : class, new()
		//	where TModel : class, new()
		//	=> QueryResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, DummyType, DummyType, DummyType>(shardNumber, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
		//	(
		//	TShard shardNumber,
		//	string sprocName,
		//	TArg optionalArgument,
		//	DbDataReader rdr,
		//	DbParameterCollection parameters,
		//	string connectionDescription,
		//	ILogger logger)
		//	where TReaderResult0 : class, new()
		//	where TReaderResult1 : class, new()
		//	where TReaderResult2 : class, new()
		//	where TReaderResult3 : class, new()
		//	where TReaderResult4 : class, new()
		//	where TReaderResult5 : class, new()
		//	where TReaderResult6 : class, new()
		//	where TModel : class, new()
		//	=> QueryResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, DummyType, DummyType>(shardNumber, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
		//	(
		//	TShard shardNumber,
		//	string sprocName,
		//	TArg optionalArgument,
		//	DbDataReader rdr,
		//	DbParameterCollection parameters,
		//	string connectionDescription,
		//	ILogger logger)
		//	where TReaderResult0 : class, new()
		//	where TReaderResult1 : class, new()
		//	where TReaderResult2 : class, new()
		//	where TReaderResult3 : class, new()
		//	where TReaderResult4 : class, new()
		//	where TReaderResult5 : class, new()
		//	where TReaderResult6 : class, new()
		//	where TReaderResult7 : class, new()
		//	where TModel : class, new()
		//	=> QueryResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, DummyType>(shardNumber, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//Full implementation for up to 8 results (plus output) 

		/// <summary>
		/// A function whose signature cooresponds to delegate QueryResultModelHandler and is used to map the provided model type(s) to query results.
		/// </summary>
		/// <typeparam name="TShard">The type of the shardNumber value. Can be any value type if not used.</typeparam>
		/// <typeparam name="TModel">This is the expected return type of the handler.</typeparam>
		/// <typeparam name="TReaderResult0">The first result set from data reader will be mapped an object or property of this type. Set to Mapper.DummyType if not used.</typeparam>
		/// <typeparam name="TReaderResult1">The second result set from data reader will be mapped an object or property of this type. Set to Mapper.DummyType if not used.</typeparam>
		/// <typeparam name="TReaderResult2">The third result set from data reader will be mapped an object or property of this type. Set to Mapper.DummyType if not used.</typeparam>
		/// <typeparam name="TReaderResult3">The forth result set from data reader will be mapped an object or property of this type. Set to Mapper.DummyType if not used.</typeparam>
		/// <typeparam name="TReaderResult4">The fifth result set from data reader will be mapped an object or property of this type. Set to Mapper.DummyType if not used.</typeparam>
		/// <typeparam name="TReaderResult5">The sixth result set from data reader will be mapped an object or property of this type. Set to Mapper.DummyType if not used.</typeparam>
		/// <typeparam name="TReaderResult6">The seventh result set from data reader will be mapped an object or property of this type. Set to Mapper.DummyType if not used.</typeparam>
		/// <typeparam name="TReaderResult7">The eighth result set from data reader will be mapped an object or property of this type. Set to Mapper.DummyType if not used.</typeparam>
		/// <typeparam name="TOutResult">This must be either type TModel or Mapper.DummyType. If set to TModel the TModel properties will be mapped to cooresponding output parameters; if set to DummyType, the output parameters are ignored.</typeparam>
		/// <param name="shardNumber">This value will be provided to ShardKey or ShardChild objects. If not using sharded data, any provided value will be ignored.</param>
		/// <param name="sprocName">The name of the stored procedure is used to cache the mapping metadata and also for provide richer logging information.</param>
		/// <param name="notUsed">This parameter is required to conform to the QueryResultModelHandler delegate signature. This argument should be null.</param>
		/// <param name="rdr">The data reader returned by the query.</param>
		/// <param name="parameters">The output parameters returned by the query.</param>
		/// <param name="connectionDescription">The connection description is used to enrich logging information.</param>
		/// <param name="logger">The logging instance to use for any logging requirements.</param>
		/// <returns>An instance of TResult, with properties matching the provided data.</returns>
		public static TModel QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutResult>
			(
			TShard shardNumber,
			string sprocName,
			object notUsed,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TReaderResult0 : class, new()
			where TReaderResult1 : class, new()
			where TReaderResult2 : class, new()
			where TReaderResult3 : class, new()
			where TReaderResult4 : class, new()
			where TReaderResult5 : class, new()
			where TReaderResult6 : class, new()
			where TReaderResult7 : class, new()
			where TOutResult : class, new()
			where TModel : class, new()
		{
			if (typeof(TOutResult) == typeof(DummyType))
			{
				if (rdr is null)
				{
					logger.DataReaderIsNull(sprocName, connectionDescription);
					return null;
				}
				if (rdr.IsClosed)
				{
					logger.DataReaderIsClosed(sprocName, connectionDescription);
					return null;
				}
			}
			else
			{
				if (typeof(TModel) != typeof(TOutResult))
				{
					throw new Exception("If a TOutParameters type is provided, it must be the result type.");
				}
			}
			// Get results from query
			IList<TReaderResult0> resultList0 = null;
			IList<TReaderResult1> resultList1 = null;
			IList<TReaderResult2> resultList2 = null;
			IList<TReaderResult3> resultList3 = null;
			IList<TReaderResult4> resultList4 = null;
			IList<TReaderResult5> resultList5 = null;
			IList<TReaderResult6> resultList6 = null;
			IList<TReaderResult7> resultList7 = null;
			TOutResult resultOutPrms = null;

			var dummy = typeof(DummyType);
			var hasNextResult = true;
			if (typeof(TReaderResult0) != dummy)
			{
				resultList0 = Mapper.FromDataReader<TReaderResult0>(rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult1) != dummy)
			{
				resultList1 = Mapper.FromDataReader<TReaderResult1>(rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult2) != dummy)
			{
				resultList2 = Mapper.FromDataReader<TReaderResult2>(rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult3) != dummy)
			{
				resultList3 = Mapper.FromDataReader<TReaderResult3>(rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult4) != dummy)
			{
				resultList4 = Mapper.FromDataReader<TReaderResult4>(rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult5) != dummy)
			{
				resultList5 = Mapper.FromDataReader<TReaderResult5>(rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult6) != dummy)
			{
				resultList6 = Mapper.FromDataReader<TReaderResult6>(rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult7) != dummy)
			{
				resultList7 = Mapper.FromDataReader<TReaderResult7>(rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (typeof(TOutResult) != dummy)
			{
				resultOutPrms = Mapper.ReadOutParameters<TOutResult>(parameters, logger);
			}


			var queryKey = typeof(TModel).ToString() + sprocName;
			if (!_getObjectCache.TryGetValue(queryKey, out var sqlObjectDelegate))
			{
				sqlObjectDelegate = BuildModelFromResultsExpressions<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutResult>(sprocName, logger);
				if (!_getObjectCache.TryAdd(queryKey, sqlObjectDelegate))
				{
					sqlObjectDelegate = _getObjectCache[queryKey];
				}
			}
			var sqlObjectDelegate2 = (Func<string, IList<TReaderResult0>, IList<TReaderResult1>, IList<TReaderResult2>, IList<TReaderResult3>, IList<TReaderResult4>, IList<TReaderResult5>, IList<TReaderResult6>, IList<TReaderResult7>, TOutResult, ILogger, TModel>)sqlObjectDelegate;
			//return (TModel)sqlObjectDelegate(sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultList6, resultList7, resultOutPrms, logger);
			return (TModel)sqlObjectDelegate2(sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultList6, resultList7, resultOutPrms, logger);

		}
		private static TResult AssignRootToResult<TEval, TResult>(string procedureName, IList<TEval> resultList, ILogger logger) where TResult : class, new() where TEval : class, new()
		{
			if (resultList is null)
			{
				logger.LogError($"Procedure {procedureName} failed to return an expected base recordset result.");
				return null;
			}
			else if (resultList.Count == 0)
			{
				logger.LogDebug($"Procedure {procedureName} returned an empty base result.");
				return null;
			}
			else if (resultList.Count > 1)
			{
				logger.LogError($"Procedure {procedureName} returned multiple base recordset results.");
				return null;
			}
			else
			{
				var result = resultList[0] as TResult;
				return result;
			}
		}
		//private static Func<string, dynamic, dynamic, dynamic, dynamic, dynamic, dynamic, dynamic, dynamic, dynamic, ILogger, dynamic> BuildModelFromResultsExpressions<
		//	TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutResult>
		private static Func<string, IList<TReaderResult0>, IList<TReaderResult1>, IList<TReaderResult2>, IList<TReaderResult3>, IList<TReaderResult4>, IList<TReaderResult5>, IList<TReaderResult6>, IList<TReaderResult7>, TOutResult, ILogger, TModel> BuildModelFromResultsExpressions<
			TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutResult>
			(string procedureName, ILogger logger)
			where TReaderResult0 : class, new()
			where TReaderResult1 : class, new()
			where TReaderResult2 : class, new()
			where TReaderResult3 : class, new()
			where TReaderResult4 : class, new()
			where TReaderResult5 : class, new()
			where TReaderResult6 : class, new()
			where TReaderResult7 : class, new()
			where TOutResult : class, new()
			where TModel : class, new()
		{
			// Build return object
			var expressions = new List<Expression>();

			var expProcName = Expression.Parameter(typeof(string), "sprocName");
			var expResultSet0 = Expression.Parameter(typeof(IList<TReaderResult0>), "rstResult0");
			var expResultSet1 = Expression.Parameter(typeof(IList<TReaderResult1>), "rstResult1");
			var expResultSet2 = Expression.Parameter(typeof(IList<TReaderResult2>), "rstResult2");
			var expResultSet3 = Expression.Parameter(typeof(IList<TReaderResult2>), "rstResult3");
			var expResultSet4 = Expression.Parameter(typeof(IList<TReaderResult4>), "rstResult4");
			var expResultSet5 = Expression.Parameter(typeof(IList<TReaderResult5>), "rstResult5");
			var expResultSet6 = Expression.Parameter(typeof(IList<TReaderResult6>), "rstResult6");
			var expResultSet7 = Expression.Parameter(typeof(IList<TReaderResult7>), "rstResult7");
			var expResultOut = Expression.Parameter(typeof(TOutResult), "outPrmsResult");
			var expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var expModel = Expression.Variable(typeof(TModel), "model");
			var expCount = Expression.Variable(typeof(int), "count");
			var expExitTarget = Expression.Label("return");
			var expNull = Expression.Constant(null);
			var expOne = Expression.Constant(1);
			var isPrmOutUsed = false;
			var isRdrResult0Used = false;
			var isRdrResult1Used = false;
			var isRdrResult2Used = false;
			var isRdrResult3Used = false;
			var isRdrResult4Used = false;
			var isRdrResult5Used = false;
			var isRdrResult6Used = false;
			var isRdrResult7Used = false;
			//var miLogInfo = typeof(ILogger).GetMethod(nameof(LoggerExtensions.LogInformation));
			var miLogError = typeof(ILogger).GetMethod(nameof(Microsoft.Extensions.Logging.LoggerExtensions.LogError));
			var miCount = typeof(IList<>).GetMethod(nameof(IList<int>.Count));
			var miSetResultSetGeneric = typeof(Mapper).GetMethod(nameof(Mapper.AssignRootToResult), BindingFlags.Static);
			var miFormat = typeof(string).GetMethod(nameof(string.Concat), BindingFlags.Static);
			var resultType = typeof(TModel);

			// 1. Try to create an instance of our result class.
			using (logger.BuildSqlResultsHandlerScope(procedureName, resultType))
			{
				//Set base type to some result value, if we can.
				if (resultType == typeof(TOutResult))
				{
					//var miReadOut = typeof(Mapper).GetMethod(nameof(Mapper.ReadOutParameters)).MakeGenericMethod(resultType);

					//var result = Mapper.ReadOutParameters<TModel>(prms, logger);
					//var expAssign = Expression.Assign(expResult, Expression.Call(miReadOut, expOutParams, expLogger));
					var expAssign = Expression.Assign(expModel, expResultOut);
					expressions.Add(expAssign);
					logger.SqlExpressionLog(expAssign);
					isPrmOutUsed = true;
				}
				else if (resultType == typeof(TReaderResult0))
				{
					var expAssign = Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult0), resultType }), expProcName, expResultSet0, expLogger));
					expressions.Add(expAssign);
					logger.SqlExpressionLog(expAssign);

					var expIf = Expression.IfThen(
						Expression.Equal(expModel, expNull), //if (result == null)
						Expression.Return(expExitTarget, expNull, typeof(TModel)) //return null;
						);

					expressions.Add(expIf);
					logger.SqlExpressionLog(expIf);
					isRdrResult0Used = true;
				}
				else if (resultType == typeof(TReaderResult1))
				{
					var expAssign = Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult1), resultType }), expProcName, expResultSet1, expLogger));
					expressions.Add(expAssign);
					logger.SqlExpressionLog(expAssign);
					var expIf = Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel)));
					expressions.Add(expIf);
					logger.SqlExpressionLog(expIf);
					isRdrResult1Used = true;
				}
				else if (resultType == typeof(TReaderResult2))
				{
					var expAssign = Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult2), resultType }), expProcName, expResultSet2, expLogger));
					expressions.Add(expAssign);
					logger.SqlExpressionLog(expAssign);
					var expIf = Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel)));
					expressions.Add(expIf);
					logger.SqlExpressionLog(expIf);
					isRdrResult2Used = true;
				}
				else if (resultType == typeof(TReaderResult3))
				{
					var expAssign = Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult3), resultType }), expProcName, expResultSet3, expLogger));
					expressions.Add(expAssign);
					logger.SqlExpressionLog(expAssign);
					var expIf = Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel)));
					expressions.Add(expIf);
					logger.SqlExpressionLog(expIf);
					isRdrResult3Used = true;
				}
				else if (resultType == typeof(TReaderResult4))
				{
					var expAssign = Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult4), resultType }), expProcName, expResultSet4, expLogger));
					expressions.Add(expAssign);
					logger.SqlExpressionLog(expAssign);
					var expIf = Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel)));
					expressions.Add(expIf);
					logger.SqlExpressionLog(expIf);
					isRdrResult4Used = true;
				}
				else if (resultType == typeof(TReaderResult5))
				{
					var expAssign = Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult5), resultType }), expProcName, expResultSet5, expLogger));
					expressions.Add(expAssign);
					logger.SqlExpressionLog(expAssign);
					var expIf = Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel)));
					expressions.Add(expIf);
					logger.SqlExpressionLog(expIf);
					isRdrResult5Used = true;
				}
				else if (resultType == typeof(TReaderResult6))
				{
					var expAssign = Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult6), resultType }), expProcName, expResultSet6, expLogger));
					expressions.Add(expAssign);
					logger.SqlExpressionLog(expAssign);
					var expIf = Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel)));
					expressions.Add(expIf);
					logger.SqlExpressionLog(expIf);
					isRdrResult6Used = true;
				}
				else if (resultType == typeof(TReaderResult7))
				{
					var expAssign = Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult7), resultType }), expProcName, expResultSet7, expLogger));
					expressions.Add(expAssign);
					logger.SqlExpressionLog(expAssign);
					var expIf = Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel)));
					expressions.Add(expIf);
					logger.SqlExpressionLog(expIf);
					isRdrResult7Used = true;
				}
				else
				{
					//match not found, so just create a new instance and we'll try again on properties
					var expAssign = Expression.Assign(expModel, Expression.New(resultType));
					expressions.Add(expAssign);
					logger.SqlExpressionLog(expAssign);
				}

				var props = resultType.GetProperties();

				//Iterate over any List<> properties and set any List<resultSet> that match.
				foreach (var prop in props)
				{
					if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult0>)) && !isRdrResult0Used)
					{
						var expAssign = Expression.Assign(Expression.Property(expModel, prop), expResultSet0);
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						isRdrResult0Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult1>)) && !isRdrResult1Used)
					{
						var expAssign = Expression.Assign(Expression.Property(expModel, prop), expResultSet1);
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						isRdrResult1Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult2>)) && !isRdrResult2Used)
					{
						var expAssign = Expression.Assign(Expression.Property(expModel, prop), expResultSet2);
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						isRdrResult2Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult3>)) && !isRdrResult3Used)
					{
						var expAssign = Expression.Assign(Expression.Property(expModel, prop), expResultSet3);
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						isRdrResult3Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult4>)) && !isRdrResult4Used)
					{
						var expAssign = Expression.Assign(Expression.Property(expModel, prop), expResultSet4);
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						isRdrResult4Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult5>)) && !isRdrResult5Used)
					{
						var expAssign = Expression.Assign(Expression.Property(expModel, prop), expResultSet5);
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						isRdrResult5Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult6>)) && !isRdrResult6Used)
					{
						var expAssign = Expression.Assign(Expression.Property(expModel, prop), expResultSet6);
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						isRdrResult6Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult7>)) && !isRdrResult7Used)
					{
						var expAssign = Expression.Assign(Expression.Property(expModel, prop), expResultSet7);
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						isRdrResult7Used = true;
					}
				}
				//Iterate over any object (non-list) properties and set any resultSet that match.
				foreach (var prop in props)
				{
					if (prop.PropertyType.IsAssignableFrom(typeof(TOutResult)) && !isPrmOutUsed)
					{
						var expAssign = Expression.Assign(Expression.Property(expModel, prop), expResultOut);
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						isPrmOutUsed = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult0)) && !isRdrResult0Used)
					{
						var expAssign = Expression.Assign(expCount, Expression.Constant(0));   //var count = 0;
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);

						var expIfNotNull = Expression.IfThen(                                      //if (resultSet0 != null)
							Expression.NotEqual(expResultSet0, expNull),                        //{ 
							Expression.Assign(expCount, Expression.Property(expResultSet0, miCount))    //count = resultSet0.Count;
							);                                                                 //}
						expressions.Add(expIfNotNull);
						logger.SqlExpressionLog(expIfNotNull);
						var expErrIfMultiple = Expression.IfThenElse(                                  //if (count == 1)
							Expression.Equal(expCount, expOne),                                 //{ result.prop = resultSet0[0]; }
							Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet0, "Item", Expression.Constant(0))),
							Expression.IfThen(                                                  //else if (count > 1)
								Expression.GreaterThan(expCount, expOne),                       //{       logger.LogError("");
								Expression.Call(miLogError, Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))
								);                                                               //}
						expressions.Add(expErrIfMultiple);
						logger.SqlExpressionLog(expErrIfMultiple);
						isRdrResult0Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult0)) && !isRdrResult1Used)
					{
						var expAssign = Expression.Assign(expCount, Expression.Constant(0));
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						var expIfNotNull = Expression.IfThen(Expression.NotEqual(expResultSet0, expNull), Expression.Assign(expCount, Expression.Property(expResultSet0, miCount)));
						expressions.Add(expIfNotNull);
						logger.SqlExpressionLog(expIfNotNull);
						var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet0, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on unexpectedly returned {{0}} results instead of one."), expCount))));
						expressions.Add(expErrIfMultiple);
						logger.SqlExpressionLog(expErrIfMultiple);
						isRdrResult0Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult1)) && !isRdrResult1Used)
					{
						var expAssign = Expression.Assign(expCount, Expression.Constant(0));
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						var expIfNotNull = Expression.IfThen(Expression.NotEqual(expResultSet1, expNull), Expression.Assign(expCount, Expression.Property(expResultSet1, miCount)));
						expressions.Add(expIfNotNull);
						logger.SqlExpressionLog(expIfNotNull);
						var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet1, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount))));
						expressions.Add(expErrIfMultiple);
						logger.SqlExpressionLog(expErrIfMultiple);
						isRdrResult1Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult2)) && !isRdrResult1Used)
					{
						var expAssign = Expression.Assign(expCount, Expression.Constant(0));
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						var expIfNotNull = Expression.IfThen(Expression.NotEqual(expResultSet2, expNull), Expression.Assign(expCount, Expression.Property(expResultSet2, miCount)));
						expressions.Add(expIfNotNull);
						logger.SqlExpressionLog(expIfNotNull);
						var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet2, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount))));
						expressions.Add(expErrIfMultiple);
						logger.SqlExpressionLog(expErrIfMultiple);
						isRdrResult2Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult3)) && !isRdrResult1Used)
					{
						var expAssign = Expression.Assign(expCount, Expression.Constant(0));
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						var expIfNotNull = Expression.IfThen(Expression.NotEqual(expResultSet3, expNull), Expression.Assign(expCount, Expression.Property(expResultSet3, miCount)));
						expressions.Add(expIfNotNull);
						logger.SqlExpressionLog(expIfNotNull);
						var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet3, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount))));
						expressions.Add(expErrIfMultiple);
						logger.SqlExpressionLog(expErrIfMultiple);
						isRdrResult3Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult4)) && !isRdrResult1Used)
					{
						var expAssign = Expression.Assign(expCount, Expression.Constant(0));
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						var expIfNotNull = Expression.IfThen(Expression.NotEqual(expResultSet4, expNull), Expression.Assign(expCount, Expression.Property(expResultSet4, miCount)));
						expressions.Add(expIfNotNull);
						logger.SqlExpressionLog(expIfNotNull);
						var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet4, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount))));
						expressions.Add(expErrIfMultiple);
						logger.SqlExpressionLog(expErrIfMultiple);
						isRdrResult4Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult5)) && !isRdrResult1Used)
					{
						var expAssign = Expression.Assign(expCount, Expression.Constant(0));
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						var expIfNotNull = Expression.IfThen(Expression.NotEqual(expResultSet5, expNull), Expression.Assign(expCount, Expression.Property(expResultSet5, miCount)));
						expressions.Add(expIfNotNull);
						logger.SqlExpressionLog(expIfNotNull);
						var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet5, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount))));
						expressions.Add(expErrIfMultiple);
						logger.SqlExpressionLog(expErrIfMultiple);
						isRdrResult5Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult6)) && !isRdrResult1Used)
					{
						var expAssign = Expression.Assign(expCount, Expression.Constant(0));
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						var expIfNotNull = Expression.IfThen(Expression.NotEqual(expResultSet6, expNull), Expression.Assign(expCount, Expression.Property(expResultSet6, miCount)));
						expressions.Add(expIfNotNull);
						logger.SqlExpressionLog(expIfNotNull);
						var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet6, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount))));
						expressions.Add(expErrIfMultiple);
						logger.SqlExpressionLog(expErrIfMultiple);
						isRdrResult6Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult7)) && !isRdrResult1Used)
					{
						var expAssign = Expression.Assign(expCount, Expression.Constant(0));
						expressions.Add(expAssign);
						logger.SqlExpressionLog(expAssign);
						var expIfNotNull = Expression.IfThen(Expression.NotEqual(expResultSet7, expNull), Expression.Assign(expCount, Expression.Property(expResultSet7, miCount)));
						expressions.Add(expIfNotNull);
						logger.SqlExpressionLog(expIfNotNull);
						var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet7, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount))));
						expressions.Add(expErrIfMultiple);
						logger.SqlExpressionLog(expErrIfMultiple);
						isRdrResult7Used = true;
					}

				}
				var expExit = Expression.Label(expExitTarget);
				expressions.Add(expExit); //Exit procedure
				logger.SqlExpressionLog(expExit);
			}

			expressions.Add(expModel); //return model
			var expBlock = Expression.Block(resultType, new ParameterExpression[] { expModel, expCount }, expressions); //+variables

			var lambda = Expression.Lambda<Func<string, IList<TReaderResult0>, IList<TReaderResult1>, IList<TReaderResult2>, IList<TReaderResult3>, IList<TReaderResult4>, IList<TReaderResult5>, IList<TReaderResult6>, IList<TReaderResult7>, TOutResult, ILogger, TModel>>
				(expBlock, new ParameterExpression[] { expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expResultSet6, expResultSet7, expResultOut, expLogger }); //+parameters
			return lambda.Compile();
		}
		#endregion
		public class DummyType
		{
		}
	}
}
