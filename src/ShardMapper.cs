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

namespace ArgentSea
{

//	public static class ShardMapper<TShard>
	public static class ShardMapper
	{
		//private static ConcurrentDictionary<Type, Action<DbParameterCollection, HashSet<string>, ILogger, object>> _cacheInParamSet = new ConcurrentDictionary<Type, Action<DbParameterCollection, HashSet<string>, ILogger, object>>();
		//private static ConcurrentDictionary<Type, Action<DbParameterCollection, HashSet<string>, ILogger>> _cacheOutParamSet = new ConcurrentDictionary<Type, Action<DbParameterCollection, HashSet<string>, ILogger>>();
		private static ConcurrentDictionary<Type, Func<dynamic, DbDataReader, ILogger, object>> _getRdrParamCache = new ConcurrentDictionary<Type, Func<dynamic, DbDataReader, ILogger, object>>();
		private static ConcurrentDictionary<Type, Func<dynamic, DbParameterCollection, ILogger, object>> _getOutParamReadCache = new ConcurrentDictionary<Type, Func<dynamic, DbParameterCollection, ILogger, object>>();
		private static ConcurrentDictionary<string, Func<dynamic, string, object, object, object, object, object, object, object, object, object, ILogger, object>> _getObjectCache = new ConcurrentDictionary<string, Func<dynamic, string, object, object, object, object, object, object, object, object, object, ILogger, object>>();
		//private static ConcurrentDictionary<Type, Func<TShard, DbDataReader, ILogger, object>> _getRdrParamCache = new ConcurrentDictionary<Type, Func<TShard, DbDataReader, ILogger, object>>();
		//private static ConcurrentDictionary<Type, Func<TShard, DbParameterCollection, ILogger, object>> _getOutParamReadCache = new ConcurrentDictionary<Type, Func<TShard, DbParameterCollection, ILogger, object>>();
		//private static ConcurrentDictionary<string, Func<TShard, string, object, object, object, object, object, object, object, object, object, ILogger, object>> _getObjectCache = new ConcurrentDictionary<string, Func<TShard, string, object, object, object, object, object, object, object, object, object, ILogger, object>>();

		#region Public methods

		///// <summary>
		///// Accepts a Sql Parameter collection and appends Sql input parameters whose values correspond to the provided object properties and MapTo attributes.
		///// </summary>
		///// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql metadata and columns.</typeparam>
		///// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object.</param>
		///// <param name="model">An object model instance. The property values are use as parameter values.</param>
		///// <param name="logger">The logger instance to write any processing or debug information to.</param>
		//public static DbParameterCollection MapToInParameters<T>(DbParameterCollection parameters, T model, ILogger logger) where T : class
		//	=> MapToInParameters<T>(parameters, model, null, logger);

		///// <summary>
		///// Accepts a Sql Parameter collection and appends Sql input parameters whose values correspond to the provided object properties and MapTo attributes.
		///// </summary>
		///// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql metadata and columns.</typeparam>
		///// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object.</param>
		///// <param name="model">An object model instance. The property values are use as parameter values.</param>
		///// <param name="ignoreParameters">A lists of parameter names that should not be created. Parameter names should exactly match, including casing and prefix (if any).</param>
		///// <param name="logger">The logger instance to write any processing or debug information to.</param>
		//public static DbParameterCollection MapToInParameters<T>(DbParameterCollection parameters, T model, HashSet<string> ignoreParameters, ILogger logger) where T : class
		//{
		//	var TModel = typeof(T);
		//	if (!_cacheInParamSet.TryGetValue(TModel, out var SqlParameterDelegates))
		//	{
		//		SqlLoggerExtensions.SqlInParametersCacheMiss(logger, TModel);
		//		SqlParameterDelegates = BuildInMapDelegate(TModel, logger);
		//		if (!_cacheInParamSet.TryAdd(TModel, SqlParameterDelegates))
		//		{
		//			SqlParameterDelegates = _cacheInParamSet[TModel];
		//		}
		//	}
		//	else
		//	{
		//		SqlLoggerExtensions.SqlInParametersCacheHit(logger, TModel);
		//	}
		//	foreach (DbParameter prm in parameters)
		//	{
		//		if (!ignoreParameters.Contains(prm.ParameterName))
		//		{
		//			ignoreParameters.Add(prm.ParameterName);
		//		}
		//	}
		//	SqlParameterDelegates(parameters, ignoreParameters, logger, model);
		//	return parameters;
		//}

		///// <summary>
		///// Accepts a Sql Parameter collection and appends Sql output parameters corresponding to the MapTo attributes.
		///// </summary>
		///// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql parameter types.</typeparam>
		///// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object.</param>
		///// <param name="model">The type of the model.</param>
		///// <param name="logger">The logger instance to write any processing or debug information to.</param>
		//public static DbParameterCollection MapToOutParameters(DbParameterCollection parameters, Type TModel, ILogger logger)
		//	=> MapToOutParameters(parameters, TModel, null, logger);

		///// <summary>
		///// Accepts a Sql Parameter collection and appends Sql output parameters corresponding to the MapTo attributes.
		///// </summary>
		///// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql parameter types.</typeparam>
		///// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object.</param>
		///// <param name="model">The type of the model.</param>
		///// <param name="ignoreParameters">A lists of parameter names that should not be created. Parameter names should exactly match, including casing and prefix (if any).</param>
		///// <param name="logger">The logger instance to write any processing or debug information to.</param>
		//public static DbParameterCollection MapToOutParameters(DbParameterCollection parameters, Type TModel, HashSet<string> ignoreParameters, ILogger logger)
		//{
		//	if (!_cacheOutParamSet.TryGetValue(TModel, out var SqlParameterDelegates))
		//	{
		//		SqlLoggerExtensions.SqlSetOutParametersCacheMiss(logger, TModel);
		//		SqlParameterDelegates = BuildOutSetDelegate(TModel, logger);
		//		if (!_cacheOutParamSet.TryAdd(TModel, SqlParameterDelegates))
		//		{
		//			SqlParameterDelegates = _cacheOutParamSet[TModel];
		//			SqlLoggerExtensions.SqlSetOutParametersCacheMiss(logger, TModel);
		//		}
		//	}
		//	else
		//	{
		//		SqlLoggerExtensions.SqlSetOutParametersCacheHit(logger, TModel);
		//	}
		//	foreach (DbParameter prm in parameters)
		//	{

		//		if (!ignoreParameters.Contains(prm.ParameterName))
		//		{
		//			ignoreParameters.Add(prm.ParameterName);
		//		}
		//	}
		//	SqlParameterDelegates(parameters, ignoreParameters, logger);
		//	return parameters;
		//}

		/// <summary>
		/// Creates a new object with property values based upon the provided output parameters which correspond to the MapTo attributes.
		/// </summary>
		/// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to read the Sql parameter collection values.</typeparam>
		/// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object after a database query.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		/// <returns>An object of the specified type, with properties set to parameter values.</returns>
		public static TModel ReadOutParameters<TShard, TModel>(this DbParameterCollection parameters, TShard shardNumber, ILogger logger) where TModel : class, new() where TShard: IComparable
		{
			var T = typeof(TModel);
			if (!_getOutParamReadCache.TryGetValue(T, out var SqlRdrDelegate))
			{
				LoggingExtensions.SqlReadOutParametersCacheMiss(logger, T);
				SqlRdrDelegate = BuildOutGetDelegate<TShard>(parameters, typeof(TModel), logger);
				if (!_getOutParamReadCache.TryAdd(typeof(TModel), SqlRdrDelegate))
				{
					SqlRdrDelegate = _getOutParamReadCache[typeof(TModel)];
				}
			}
			else
			{
				LoggingExtensions.SqlReadOutParametersCacheHit(logger, T);
			}
			return (TModel)SqlRdrDelegate(shardNumber, parameters, logger);
		}

		/// <summary>
		/// Accepts a data reader object and returns a list of objects of the specified type, one for each record.
		/// </summary>
		/// <typeparam name="TModel">The type of the list result</typeparam>
		/// <param name="rdr">The data reader, set to the current result set.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		/// <returns>A list of objects of the specified type, one for each result.</returns>
		public static IList<TModel> FromDataReader<TShard, TModel>(TShard shardNumber, DbDataReader rdr, ILogger logger) where TModel : class, new() where TShard: IComparable
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
				SqlRdrDelegate = BuildReaderMapDelegate<TShard, TModel>(logger);
				if (!_getRdrParamCache.TryAdd(typeof(TModel), SqlRdrDelegate))
				{
					SqlRdrDelegate = _getRdrParamCache[typeof(TModel)];
				}
			}
			else
			{
				LoggingExtensions.SqlReaderCacheHit(logger, T);
			}
			return (List<TModel>)SqlRdrDelegate(shardNumber, rdr, logger);
		}

		#endregion
		#region delegate builders
		//private static Action<DbParameterCollection, HashSet<string>, ILogger, object> BuildInMapDelegate(Type TModel, ILogger logger)
		//{
		//	var expressions = new List<Expression>();
		//	//Create the two delegate parameters: in: sqlParametersCollection and model instance; out: sqlParametersCollection
		//	ParameterExpression prmSqlPrms = Expression.Variable(typeof(DbParameterCollection), "parameters");
		//	ParameterExpression prmObjInstance = Expression.Parameter(typeof(object), "model");
		//	ParameterExpression expIgnoreParameters = Expression.Parameter(typeof(HashSet<string>), "ignoreParameters");
		//	ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
		//	var exprInPrms = new ParameterExpression[] { prmSqlPrms, expIgnoreParameters, expLogger, prmObjInstance };

		//	using (logger.BuildInParameterScope(TModel))
		//	{
		//		var prmTypedInstance = Expression.TypeAs(prmObjInstance, TModel);  //Our cached delegates accept neither generic nor dynamic arguments. We have to pass object then cast.
		//		var noDupPrmNameList = new HashSet<string>();
		//		//var miLogTrace = typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.TraceInMapperProperty));
		//		logger.SqlExpressionBlockStart(nameof(ShardMapper.MapToInParameters), exprInPrms);
		//		expressions.Add(prmTypedInstance);
		//		logger.SqlExpressionLog(prmTypedInstance);
		//		IterateInMapProperties(TModel, expressions, prmSqlPrms, prmTypedInstance, expIgnoreParameters, expLogger, noDupPrmNameList, logger);
		//		logger.SqlExpressionBlockEnd(nameof(ShardMapper.MapToInParameters));
		//	}
		//	var inBlock = Expression.Block(expressions);
		//	var lmbIn = Expression.Lambda<Action<DbParameterCollection, HashSet<string>, ILogger, object>>(inBlock, exprInPrms);
		//	return lmbIn.Compile();
		//}
		//private static bool PropertyTypeIsShardType(Type propertyType)
		//{
		//	if (propertyType == typeof(Nullable<>))
		//	{
		//		propertyType = Nullable.GetUnderlyingType(propertyType);
		//	}
		//	if (propertyType == typeof(ShardKey<,>) || propertyType == typeof(ShardChild<,,>))
		//	{
		//		return true;
		//	}
		//	else
		//	{
		//		return false;
		//	}
		//}
		//private static void IterateInMapProperties(Type TModel, List<Expression> expressions, ParameterExpression prmSqlPrms, Expression prmTypedInstance, ParameterExpression expIgnoreParameters, ParameterExpression expLogger, HashSet<string> noDupPrmNameList, ILogger logger)
		//{
		//	var miLogTrace = typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.TraceInMapperProperty));
		//	foreach (var prop in TModel.GetProperties())
		//	{
		//		//Does property have our SqlMapAttribute attribute?
		//		if (prop.IsDefined(typeof(ParameterMapAttribute), true))
		//		{
		//			//Instantiate our SqlMapAttribute attribute
		//			var attrPMs = prop.GetCustomAttributes<ParameterMapAttribute>(true);
		//			foreach (var attrPM in attrPMs)
		//			{
		//				var expLog = Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name));
		//				expressions.Add(expLog);
		//				logger.SqlExpressionLog(expLog);
		//				if (!attrPM.IsValidType(prop.PropertyType))
		//				{
		//					throw new InvalidMapTypeException(prop, attrPM.SqlType);
		//				}
		//				MemberExpression expProperty = Expression.Property(prmTypedInstance, prop);
		//				attrPM.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expProperty, prop.PropertyType, expLogger, logger);
		//			}
		//		}
		//		else if (prop.IsDefined(typeof(MapToModel)) && !prop.PropertyType.IsValueType)
		//		{
		//			MemberExpression expProperty = Expression.Property(prmTypedInstance, prop);
		//			IterateInMapProperties(prop.PropertyType, expressions, prmSqlPrms, expProperty, expIgnoreParameters, expLogger, noDupPrmNameList, logger);
		//		}
		//	}
		//}

		//private static Action<DbParameterCollection, HashSet<string>, ILogger> BuildOutSetDelegate(Type TModel, ILogger logger)
		//{
		//	var expressions = new List<Expression>();

		//	//Create the two delegate parameters: in: sqlParametersCollection and model instance; out: sqlParametersCollection
		//	ParameterExpression prmSqlPrms = Expression.Variable(typeof(DbParameterCollection), "parameters");
		//	ParameterExpression expIgnoreParameters = Expression.Parameter(typeof(HashSet<string>), "ignoreParameters");
		//	ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
		//	var noDupPrmNameList = new HashSet<string>();

		//	var exprOutPrms = new ParameterExpression[] { prmSqlPrms, expIgnoreParameters, expLogger };

		//	IterateSetOutParameters(TModel, expressions, prmSqlPrms, expIgnoreParameters, expLogger, noDupPrmNameList, logger);

		//	if (logger.IsEnabled(LogLevel.Debug))
		//	{
		//		using (logger.BuildSetOutParameterScope(TModel))
		//		{
		//			logger.SqlExpressionBlockStart(nameof(ShardMapper.MapToOutParameters), exprOutPrms);
		//			foreach (var exp in expressions)
		//			{
		//				logger.SqlExpressionLog(exp);
		//			}
		//			logger.SqlExpressionBlockEnd(nameof(ShardMapper.MapToOutParameters));
		//		}
		//	}
		//	var outBlock = Expression.Block(expressions);
		//	var lmbOut = Expression.Lambda<Action<DbParameterCollection, HashSet<string>, ILogger>>(outBlock, exprOutPrms);
		//	return lmbOut.Compile();
		//}
		//private static void IterateSetOutParameters(Type TModel, List<Expression> expressions, ParameterExpression prmSqlPrms, ParameterExpression expIgnoreParameters, ParameterExpression expLogger, HashSet<string> noDupPrmNameList, ILogger logger)
		//{
		//	var miLogTrace = typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.TraceSetOutMapperProperty));
		//	//Loop through all object properties:
		//	foreach (var prop in TModel.GetProperties())
		//	{
		//		//Does property have our SqlMapAttribute attribute?
		//		if (prop.IsDefined(typeof(ParameterMapAttribute), true))
		//		{
		//			var attrPMs = prop.GetCustomAttributes<ParameterMapAttribute>(true);
		//			foreach (var attrPM in attrPMs)
		//			{
		//				expressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name)));
		//				if (!attrPM.IsValidType(prop.PropertyType))
		//				{
		//					throw new InvalidMapTypeException(prop, attrPM.SqlType);
		//				}
		//				attrPM.AppendSetOutParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, prop.PropertyType, expLogger, logger);
		//			}
		//		}
		//		else if (prop.IsDefined(typeof(MapToModel)) && !prop.PropertyType.IsValueType)
		//		{
		//			IterateSetOutParameters(prop.PropertyType, expressions, prmSqlPrms, expIgnoreParameters, expLogger, noDupPrmNameList, logger);
		//		}
		//	}
		//}


		private static Func<dynamic, DbParameterCollection, ILogger, object> BuildOutGetDelegate<TShard>(DbParameterCollection parameters, Type TModel, ILogger logger) where TShard: IComparable
		{
			ParameterExpression prmShardNumber = Expression.Variable(typeof(TShard), "shardNumber");
			ParameterExpression expPrms = Expression.Parameter(typeof(DbParameterCollection), "parameters");
			ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var variableExpressions = new List<ParameterExpression>();
			var expModel = Expression.Variable(TModel, "model"); //list result variable
			variableExpressions.Add(expModel);
			var expPrm = Expression.Variable(typeof(DbParameter), "prm");
			variableExpressions.Add(expPrm);
			var expressionPrms = new ParameterExpression[] { prmShardNumber, expPrms, expLogger };


			var expressions = new List<Expression>()
			{
				Expression.Assign(expModel, Expression.New(TModel)) // var result = new <T>;
            };
			//Loop through all object properties:
			IterateGetOutParameters(TModel, prmShardNumber, expPrms, expPrm, variableExpressions, expressions, expModel, expLogger, logger);
			expressions.Add(expModel); //return value;
			var expBlock = Expression.Block(variableExpressions, expressions);
			var lambda = Expression.Lambda<Func<dynamic, DbParameterCollection, ILogger, object>>(expBlock, expressionPrms);
			return lambda.Compile();
		}
		private static void IterateGetOutParameters(Type TModel, ParameterExpression prmShardNumber, ParameterExpression expPrms, ParameterExpression expPrm, List<ParameterExpression> variableExpressions, List<Expression> expressions, Expression expModel, ParameterExpression expLogger, ILogger logger)
		{
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceGetOutMapperProperty));

			foreach (var prop in TModel.GetProperties())
			{
				if (prop.IsDefined(typeof(ParameterMapAttribute), true))
				{
					var attrPMs = prop.GetCustomAttributes<ParameterMapAttribute>(true);
					//ParameterExpression expVarShardNumber = null;
					//ParameterExpression expVarRecordId = null;
					//ParameterExpression expVarChildId = null;
					//ParameterExpression expVarConcurrency = null;
					//bool isPropShardKey = false;
					//bool isPropShardChild = false;

					//var baseType = prop.PropertyType;
					//if (baseType == typeof(Nullable<>))
					//{
					//	baseType = Nullable.GetUnderlyingType(baseType);
					//}
					//if (baseType == typeof(ShardKey<,>))
					//{
					//	isPropShardKey = true;
					//	var shardTypes = baseType.GetGenericArguments();
					//	if (shardTypes[0].IsValueType && shardTypes[0] != typeof(Nullable<>))
					//	{

					//		shardTypes[0] = typeof(Nullable).MakeGenericType(shardTypes[0]);
					//	}
					//	if (shardTypes[1].IsValueType && shardTypes[1] != typeof(Nullable<>))
					//	{

					//		shardTypes[1] = typeof(Nullable).MakeGenericType(shardTypes[1]);
					//	}
					//	expVarShardNumber = Expression.Variable(shardTypes[0], prop.Name + "_shardNumber");
					//	variableExpressions.Add(expVarShardNumber);
					//	expressions.Add(Expression.Assign(expVarShardNumber, prmShardNumber));

					//	expVarRecordId = Expression.Variable(shardTypes[1], prop.Name + "_recordId");
					//	expressions.Add(Expression.Assign(expVarRecordId, Expression.Constant(null)));
					//	variableExpressions.Add(expVarRecordId);

					//	expVarConcurrency = Expression.Variable(typeof(DateTime?), prop.Name + "_concurrency");
					//	expressions.Add(Expression.Assign(expVarConcurrency, Expression.Constant(null)));
					//	variableExpressions.Add(expVarShardNumber);
					//}
					//else if (baseType == typeof(ShardChild<,,>))
					//{
					//	isPropShardChild = true;
					//	var shardTypes = baseType.GetGenericArguments();
					//	if (shardTypes[0].IsValueType && shardTypes[0] != typeof(Nullable<>))
					//	{

					//		shardTypes[0] = typeof(Nullable).MakeGenericType(shardTypes[0]);
					//	}
					//	if (shardTypes[1].IsValueType && shardTypes[1] != typeof(Nullable<>))
					//	{

					//		shardTypes[1] = typeof(Nullable).MakeGenericType(shardTypes[1]);
					//	}
					//	expVarShardNumber = Expression.Variable(shardTypes[0], prop.Name + "_shardNumber");
					//	variableExpressions.Add(expVarShardNumber);
					//	expressions.Add(Expression.Assign(expVarShardNumber, prmShardNumber));

					//	expVarRecordId = Expression.Variable(shardTypes[1], prop.Name + "_recordId");
					//	expressions.Add(Expression.Assign(expVarRecordId, Expression.Constant(null)));
					//	variableExpressions.Add(expVarRecordId);

					//	expVarChildId = Expression.Variable(shardTypes[1], prop.Name + "_childId");
					//	expressions.Add(Expression.Assign(expVarChildId, Expression.Constant(null)));
					//	variableExpressions.Add(expVarChildId);

					//	expVarConcurrency = Expression.Variable(typeof(DateTime?), prop.Name + "_concurrency");
					//	expressions.Add(Expression.Assign(expVarConcurrency, Expression.Constant(null)));
					//	variableExpressions.Add(expVarShardNumber);
					//}
					MemberExpression expProperty = Expression.Property(expModel, prop);

					foreach (var attrPM in attrPMs)
					{
						expressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name)));

						if (!attrPM.IsValidType(prop.PropertyType))
						{
							throw new InvalidMapTypeException(prop, attrPM.SqlType);
						}


						//var baseProperty = prop;
						//switch (attrPM.ShardPosition)
						//{
						//	case ParameterMapAttribute.ShardUsage.IsShardNumber:
						//		if (!isPropShardKey && !isPropShardChild)
						//		{
						//			throw new Exception("The property attribute is flagged as a Shard Number, but the property type is not ShardKey or ShardChild.");
						//		}
						//		attrPM.AppendReadOutParameterExpressions(prmShardNumber, expVarShardNumber, expressions, expPrms, expPrm, prop, attrPM.ShardPosition, expLogger, logger);
						//		break;
						//	case ParameterMapAttribute.ShardUsage.IsRecordId:
						//		if (!isPropShardKey && !isPropShardChild)
						//		{
						//			throw new Exception("The property attribute is flagged as a Shard RecordId, but the property type is not ShardKey or ShardChild.");
						//		}
						//		attrPM.AppendReadOutParameterExpressions(prmShardNumber, expVarRecordId, expressions, expPrms, expPrm, prop, attrPM.ShardPosition, expLogger, logger);
						//		break;
						//	case ParameterMapAttribute.ShardUsage.IsChildId:
						//		if (!isPropShardKey && !isPropShardChild)
						//		{
						//			throw new Exception("The property attribute is flagged as a Shard Child, but the property type is not a ShardChild.");
						//		}
						//		attrPM.AppendReadOutParameterExpressions(prmShardNumber, expVarChildId, expressions, expPrms, expPrm, prop, attrPM.ShardPosition, expLogger, logger);
						//		break;
						//	case ParameterMapAttribute.ShardUsage.IsConcurrencyStamp:
						//		if (!isPropShardKey && !isPropShardChild)
						//		{
						//			throw new Exception("The property attribute is flagged as a concurrency value, but the property type is not ShardKey or ShardChild.");
						//		}
						//		attrPM.AppendReadOutParameterExpressions(prmShardNumber, expVarConcurrency, expressions, expPrms, expPrm, prop, attrPM.ShardPosition, expLogger, logger);
						//		break;
						//	default:
						attrPM.AppendReadOutParameterExpressions(expProperty, expressions, expPrms, expPrm, prop, expLogger, logger);
						//		break;
						//}
					}
					//if (isPropShardKey)
					//{
					//	var expDataOrigin = Expression.New(typeof(DataOrigin).GetConstructor(new[] { typeof(char) }), new[] { Expression.Constant(originSource, typeof(char)) });

					//	//nullable
					//	Expression.IfThenElse(
					//	Expression.AndAlso(Expression.NotEqual(expVarShardNumber, Expression.Constant(null)),
					//		Expression.NotEqual(expVarRecordId, Expression.Constant(null))),
					//	//true
					//	Expression.Assign(expProperty, Expression.New(prop.PropertyType.GetConstructor(new Type[] { typeof(DataOrigin), shardTypes[0], shardTypes[1], typeof(DateTime?) }), new Expression[] { expDataOrigin, expVarShardNumber, expVarRecordId, expVarConcurrency })),
					//	//false
					//	Expression.Assign(expProperty, Expression.Constant(null)));



					//	//nullable
					//	Expression.AndAlso(Expression.Property(expVarShardNumber, typeof(byte?).GetProperty(nameof(Nullable<byte>.HasValue))),
					//	Expression.Property(expVarRecordId, typeof(int?).GetProperty(nameof(Nullable<int>.HasValue))),
					//	Expression.Assign(expProperty, 
					//		Expression.New(prop.PropertyType, new Expression[] 
					//			{ Expression.Property(expVarShardNumber, shardTypes[1].GetProperty(nameof(Nullable<int>.Value), 
					//		expVarShardNumber, typeof(byte?).GetProperty(nameof(Nullable<int>.Value), tree })),
					//	Expression.Assign(expProperty, Expression.Constant(null)));


					//	var expNewShardKey = ;
					//	expressions.Add(Expression.Assign(expProperty, expNewShardKey);
					//}
					//else if (isPropShardChild)
					//{

					//}
				}
				else if (prop.IsDefined(typeof(MapToModel)) && !prop.PropertyType.IsValueType)
				{
					MemberExpression expProperty = Expression.Property(expModel, prop);
					IterateGetOutParameters(prop.PropertyType, prmShardNumber, expPrms, expPrm, variableExpressions, expressions, expProperty, expLogger, logger);
				}
			}
		}

		private static Func<dynamic, DbDataReader, ILogger, List<T>> BuildReaderMapDelegate<TShard, T>(ILogger logger)
		{
			var TModel = typeof(T);

			var expressions = new List<Expression>();
			var columnLookupExpressions = new List<MethodCallExpression>();

			ParameterExpression prmShardNumber = Expression.Variable(typeof(TShard), "shardNumber");
			ParameterExpression prmSqlRdr = Expression.Parameter(typeof(DbDataReader), "rdr"); //input param
			ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var expressionPrms = new ParameterExpression[] { prmShardNumber, prmSqlRdr, expLogger };

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
			expressions.Add(Expression.Assign(expModel, Expression.New(typeof(T)))); //var model = new T

			//Loop through all object properties:
			IterateRdrColumns(TModel, prmShardNumber, expModel, columnLookupExpressions, expressions, prmSqlRdr, expOrdinals, expOrdinal, ref propIndex, expLogger, logger);

			expressions.Add(Expression.Call(expListResult, miListAdd, expModel)); //ResultList.Add(model);
			resultExpressions.Add(Expression.Assign(expOrdinals, Expression.NewArrayInit(typeof(int), columnLookupExpressions.ToArray())));

			var loopLabel = Expression.Label("readNextRow");
			resultExpressions.Add(Expression.Assign(expListResult, Expression.New(typeof(List<T>)))); // var result = new List<T>;
			resultExpressions.Add(Expression.Loop(
					Expression.IfThenElse(Expression.Call(prmSqlRdr, miRead),
						Expression.Block(expressions),
						Expression.Break(loopLabel)
					), loopLabel));
			resultExpressions.Add(expListResult); //return type

			var expBlock = Expression.Block(typeof(List<T>), new ParameterExpression[] { expModel, expListResult, expOrdinals, expOrdinal }, resultExpressions);
			var lambda = Expression.Lambda<Func<dynamic, DbDataReader, ILogger, List<T>>>(expBlock, expressionPrms);
			return lambda.Compile();
		}
		private static void IterateRdrColumns(Type TModel, ParameterExpression prmShardNumber, Expression expModel, List<MethodCallExpression> columnLookupExpressions, List<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, ParameterExpression expLogger, ILogger logger)
		{
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceRdrMapperProperty));
			foreach (var prop in TModel.GetProperties())
			{
				if (prop.IsDefined(typeof(ParameterMapAttribute), true))
				{
					var attrPMs = prop.GetCustomAttributes<ParameterMapAttribute>(true);
					//TODO: if ShardKey/ShardChild create memory variables and add to expression
					foreach (var attrPM in attrPMs)
					{
						expressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name)));

						if (!attrPM.IsValidType(prop.PropertyType))
						{
							throw new InvalidMapTypeException(prop, attrPM.SqlType);
						}
						MemberExpression expProperty = Expression.Property(expModel, prop);
						attrPM.AppendReaderExpressions(expProperty, columnLookupExpressions, expressions, prmSqlRdr, expOrdinals, expOrdinal, ref propIndex, prop, expLogger, logger);
					}
					//TODO: if ShardKey/ShardChild combined memory variables to make ShardKey/Child and add to expression
					propIndex++;
				}
				else if (prop.IsDefined(typeof(MapToModel)) && !prop.PropertyType.IsValueType)
				{
					MemberExpression expProperty = Expression.Property(expModel, prop);
					IterateRdrColumns(prop.PropertyType, prmShardNumber, expProperty, columnLookupExpressions, expressions, prmSqlRdr, expOrdinals, expOrdinal, ref propIndex, expLogger, logger);
				}
			}

		}
		#endregion
		#region Convert Sql result to object(s)
		public static TResult SqlResultsHandler<TShard, TReaderResult, TOutParameters, TResult>
			(
			DataOrigin dataOrigin,
			TShard shardNumber,
			string proceedureName,
			DbDataReader rdr,
			DbParameterCollection parameters,
			ILogger logger)
			where TShard: IComparable
			where TReaderResult : class, new()
			where TOutParameters : class, new()
			where TResult : class, new()
			=> SqlResultsHandler<TShard, TReaderResult, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TOutParameters, TResult>(dataOrigin, shardNumber, proceedureName, rdr, parameters, logger);


		public static TResult SqlResultsHandler<TShard, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutParameters, TResult>
			(
			DataOrigin dataOrigin,
			TShard shardNumber,
			string procedureName,
			DbDataReader rdr,
			DbParameterCollection parameters,
			ILogger logger)
			where TShard: IComparable
			where TReaderResult0 : class, new()
			where TReaderResult1 : class, new()
			where TReaderResult2 : class, new()
			where TReaderResult3 : class, new()
			where TReaderResult4 : class, new()
			where TReaderResult5 : class, new()
			where TReaderResult6 : class, new()
			where TReaderResult7 : class, new()
			where TOutParameters : class, new()
			where TResult : class, new()
		{
			if (rdr is null)
			{
				logger.LogError($"Null reader object provided to query handler for {procedureName} on shard {shardNumber.ToString()}, returning null result.");
				return null;
			}
			if (rdr.IsClosed)
			{
				logger.LogError($"Data reader object was closed for {procedureName} on shard {shardNumber.ToString()}, returning null result.");
				return null;
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
			TOutParameters resultOutPrms = null;

			var dummy = typeof(Mapper.DummyType);
			var hasNextResult = true;
			if (typeof(TReaderResult0) != dummy)
			{
				resultList0 = ShardMapper.FromDataReader<TShard, TReaderResult0>(shardNumber, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult1) != dummy)
			{
				resultList1 = ShardMapper.FromDataReader<TShard, TReaderResult1>(shardNumber, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult2) != dummy)
			{
				resultList2 = ShardMapper.FromDataReader<TShard, TReaderResult2>(shardNumber, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult3) != dummy)
			{
				resultList3 = ShardMapper.FromDataReader<TShard, TReaderResult3>(shardNumber, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult4) != dummy)
			{
				resultList4 = ShardMapper.FromDataReader<TShard, TReaderResult4>(shardNumber, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult5) != dummy)
			{
				resultList5 = ShardMapper.FromDataReader<TShard, TReaderResult5>(shardNumber, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult6) != dummy)
			{
				resultList6 = ShardMapper.FromDataReader<TShard, TReaderResult6>(shardNumber, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult7) != dummy)
			{
				resultList7 = ShardMapper.FromDataReader<TShard, TReaderResult7>(shardNumber, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (typeof(TOutParameters) != dummy)
			{
				resultOutPrms = ShardMapper.ReadOutParameters<TShard, TOutParameters>(parameters, shardNumber, logger);
			}


			var queryKey = typeof(TResult).ToString() + procedureName;
			if (!_getObjectCache.TryGetValue(queryKey, out var sqlObjectDelegate))
			{
				sqlObjectDelegate = BuildExpressionSqlResultsHandler<TShard, TResult,
					TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6,
					TReaderResult7, TOutParameters>(procedureName, logger);
				if (!_getObjectCache.TryAdd(queryKey, sqlObjectDelegate))
				{
					sqlObjectDelegate = _getObjectCache[queryKey];
				}
			}

			return (TResult)sqlObjectDelegate(shardNumber, procedureName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultList6, resultList7, resultOutPrms, logger);
		}
		private static TResult AssignRootToResult<TShard, TEval, TResult>(TShard shardNumber, string procedureName, IList<TEval> resultList, ILogger logger) where TResult : class, new() where TEval : class, new() where TShard: IComparable
		{
			if (resultList is null)
			{
				logger.LogError($"Procedure {procedureName} on shard {shardNumber.ToString()} failed to return an expected base recordset result.");
				return null;
			}
			else if (resultList.Count == 0)
			{
				logger.LogDebug($"Procedure {procedureName} on shard {shardNumber.ToString()} returned an empty base result.");
				return null;
			}
			else if (resultList.Count > 1)
			{
				logger.LogError($"Procedure {procedureName} on shard {shardNumber.ToString()} returned multiple base recordset results.");
				return null;
			}
			else
			{
				var result = resultList[0] as TResult;
				return result;
			}
		}
		private static Func<dynamic, string, object, object, object, object, object, object, object, object, object, ILogger, object> BuildExpressionSqlResultsHandler<
			TShard, TResult, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutParameters>
			(string procedureName, ILogger logger)
			where TShard: IComparable
			where TReaderResult0 : class, new()
			where TReaderResult1 : class, new()
			where TReaderResult2 : class, new()
			where TReaderResult3 : class, new()
			where TReaderResult4 : class, new()
			where TReaderResult5 : class, new()
			where TReaderResult6 : class, new()
			where TReaderResult7 : class, new()
			where TOutParameters : class, new()
			where TResult : class, new()
		{
			// Build return object
			TResult result = null;
			var expressions = new List<Expression>();

			var expShardNumber = Expression.Parameter(typeof(TShard), "shardNumber");
			var expProcName = Expression.Parameter(typeof(string), "sprocName");
			var expResultSet0 = Expression.Parameter(typeof(TReaderResult0), "rst0");
			var expResultSet1 = Expression.Parameter(typeof(TReaderResult1), "rst1");
			var expResultSet2 = Expression.Parameter(typeof(TReaderResult2), "rst2");
			var expResultSet3 = Expression.Parameter(typeof(TReaderResult3), "rst3");
			var expResultSet4 = Expression.Parameter(typeof(TReaderResult4), "rst4");
			var expResultSet5 = Expression.Parameter(typeof(TReaderResult5), "rst5");
			var expResultSet6 = Expression.Parameter(typeof(TReaderResult6), "rst6");
			var expResultSet7 = Expression.Parameter(typeof(TReaderResult7), "rst7");
			var expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var expPrmOut = Expression.Parameter(typeof(TOutParameters), "parameters");
			var expResult = Expression.Variable(typeof(TResult), "result");
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
			var miLogError = typeof(ILogger).GetMethod(nameof(LoggerExtensions.LogError));
			var miCount = typeof(IList<>).GetMethod(nameof(IList<int>.Count));
			var miSetResultSetGeneric = typeof(ShardMapper).GetMethod(nameof(ShardMapper.AssignRootToResult), BindingFlags.Static);
			var miFormat = typeof(string).GetMethod(nameof(string.Concat), BindingFlags.Static);
			var resultType = typeof(TResult);

			using (logger.BuildSqlResultsHandlerScope(procedureName, resultType))
			{
				//Set base type to some result value, if we can.
				if (resultType == typeof(TOutParameters))
				{
					expressions.Add(Expression.Assign(expResult, expPrmOut));
					isPrmOutUsed = true;
				}
				else if (resultType == typeof(TReaderResult0))
				{
					//result = AssignRootToResult<TReaderResult0, TResult>(shardNumber, procedureName, resultList0, logger)
					expressions.Add(Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult0), resultType }), expShardNumber, expProcName, expResultSet0, expLogger)));
					expressions.Add(Expression.IfThen(
						Expression.Equal(expResult, expNull), //if (result == null)
						Expression.Return(expExitTarget, expNull, typeof(TResult)) //return null;
						));
					isRdrResult0Used = true;
				}
				else if (result is TReaderResult1)
				{
					expressions.Add(Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult1), resultType }), expShardNumber, expProcName, expResultSet1, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expResult, expNull), Expression.Return(expExitTarget, expNull, typeof(TResult))));
					isRdrResult1Used = true;
				}
				else if (result is TReaderResult2)
				{
					expressions.Add(Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult2), resultType }), expShardNumber, expProcName, expResultSet2, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expResult, expNull), Expression.Return(expExitTarget, expNull, typeof(TResult))));
					isRdrResult2Used = true;
				}
				else if (result is TReaderResult3)
				{
					expressions.Add(Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult3), resultType }), expShardNumber, expProcName, expResultSet3, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expResult, expNull), Expression.Return(expExitTarget, expNull, typeof(TResult))));
					isRdrResult3Used = true;
				}
				else if (result is TReaderResult4)
				{
					expressions.Add(Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult4), resultType }), expShardNumber, expProcName, expResultSet4, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expResult, expNull), Expression.Return(expExitTarget, expNull, typeof(TResult))));
					isRdrResult4Used = true;
				}
				else if (result is TReaderResult5)
				{
					expressions.Add(Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult5), resultType }), expShardNumber, expProcName, expResultSet5, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expResult, expNull), Expression.Return(expExitTarget, expNull, typeof(TResult))));
					isRdrResult5Used = true;
				}
				else if (result is TReaderResult6)
				{
					expressions.Add(Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult6), resultType }), expShardNumber, expProcName, expResultSet6, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expResult, expNull), Expression.Return(expExitTarget, expNull, typeof(TResult))));
					isRdrResult6Used = true;
				}
				else if (result is TReaderResult7)
				{
					expressions.Add(Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult7), resultType }), expShardNumber, expProcName, expResultSet7, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expResult, expNull), Expression.Return(expExitTarget, expNull, typeof(TResult))));
					isRdrResult7Used = true;
				}
				else
				{
					//match not found, so just create a new instance and we'll try again on properties
					expressions.Add(Expression.Assign(expResult, Expression.New(resultType)));
				}

				var props = resultType.GetProperties();

				//Iterate over any List<> properties and set any List<resultSet> that match.
				foreach (var prop in props)
				{
					if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult0>)) && !isRdrResult0Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expResult, prop), expResultSet0));
						isRdrResult0Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult1>)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expResult, prop), expResultSet1));
						isRdrResult1Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult2>)) && !isRdrResult2Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expResult, prop), expResultSet2));
						isRdrResult2Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult3>)) && !isRdrResult3Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expResult, prop), expResultSet3));
						isRdrResult3Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult4>)) && !isRdrResult4Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expResult, prop), expResultSet4));
						isRdrResult4Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult5>)) && !isRdrResult5Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expResult, prop), expResultSet5));
						isRdrResult5Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult6>)) && !isRdrResult6Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expResult, prop), expResultSet6));
						isRdrResult6Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult7>)) && !isRdrResult7Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expResult, prop), expResultSet7));
						isRdrResult7Used = true;
					}
				}
				//Iterate over any object (non-list) properties and set any resultSet that match.
				foreach (var prop in props)
				{
					if (prop.PropertyType.IsAssignableFrom(typeof(TOutParameters)) && !isPrmOutUsed)
					{
						expressions.Add(Expression.Assign(Expression.Property(expResult, prop), expPrmOut));
						isPrmOutUsed = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult0)) && !isRdrResult0Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));

						expressions.Add(Expression.IfThen(                                      //if (resultSet0 != null)
							Expression.NotEqual(expResultSet0, expNull),                        //{ 
							Expression.Assign(expCount, Expression.Property(expResultSet0, miCount))    //count = resultSet0.Count;
							));
						expressions.Add(Expression.IfThenElse(                                  //if (count == 1)
							Expression.Equal(expCount, expOne),                                 //{ result.prop = resultSet0[0]; }
							Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet0, "Item", Expression.Constant(0))),
							Expression.IfThen(                                                  //else if (count > 1)
								Expression.GreaterThan(expCount, expOne),                       //{       logger.LogError("");
								Expression.Call(miLogError, Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount)))
								));
						isRdrResult0Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult0)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet0, expNull), Expression.Assign(expCount, Expression.Property(expResultSet0, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet0, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount)))));
						isRdrResult0Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult1)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet1, expNull), Expression.Assign(expCount, Expression.Property(expResultSet1, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet1, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount)))));
						isRdrResult1Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult2)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet2, expNull), Expression.Assign(expCount, Expression.Property(expResultSet2, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet2, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount)))));
						isRdrResult2Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult3)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet3, expNull), Expression.Assign(expCount, Expression.Property(expResultSet3, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet3, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount)))));
						isRdrResult3Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult4)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet4, expNull), Expression.Assign(expCount, Expression.Property(expResultSet4, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet4, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount)))));
						isRdrResult4Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult5)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet5, expNull), Expression.Assign(expCount, Expression.Property(expResultSet5, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet5, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount)))));
						isRdrResult5Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult6)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet6, expNull), Expression.Assign(expCount, Expression.Property(expResultSet6, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet6, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount)))));
						isRdrResult6Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult7)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet7, expNull), Expression.Assign(expCount, Expression.Property(expResultSet7, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet7, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount)))));
						isRdrResult7Used = true;
					}

				}
				expressions.Add(Expression.Label(expExitTarget)); //Exit procedure
			}
			var expBlock = Expression.Block(resultType, new ParameterExpression[] { expCount, expResult }, expressions); //+variables
			var lambda = Expression.Lambda<Func<dynamic, string, object, object, object, object, object, object, object, object, object, ILogger, object>>
				(expBlock, new ParameterExpression[] { expShardNumber, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expResultSet6, expResultSet7, expPrmOut, expLogger }); //+parameters
			return lambda.Compile();
		}
		#endregion
	}
}
