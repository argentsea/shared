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
		private static ConcurrentDictionary<Type, Delegate> _cacheInParamSet = new ConcurrentDictionary<Type, Delegate>();
		private static ConcurrentDictionary<Type, Action<DbParameterCollection, HashSet<string>, ILogger>> _cacheOutParamSet = new ConcurrentDictionary<Type, Action<DbParameterCollection, HashSet<string>, ILogger>>();
		private static ConcurrentDictionary<Type, Delegate> _getRdrParamCache = new ConcurrentDictionary<Type, Delegate>();
		private static ConcurrentDictionary<Type, Delegate> _getOutParamReadCache = new ConcurrentDictionary<Type, Delegate>();
		private static ConcurrentDictionary<string, Delegate> _getObjectCache = new ConcurrentDictionary<string, Delegate>();

		#region Public methods


		#region Map Input Parameters

		/// <summary>
		/// Accepts a Sql Parameter collection and appends Sql input parameters whose values correspond to the provided object properties and MapTo attributes.
		/// </summary>
		/// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql metadata and columns.</typeparam>
		/// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object.</param>
		/// <param name="model">An object model instance. The property values are use as parameter values.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		public static DbParameterCollection MapToInParameters<TModel>(this DbParameterCollection parameters, TModel model, ILogger logger) where TModel : class
			=> MapToInParameters<BadShardType, TModel>(parameters, null, model, null, logger);

		/// <summary>
		/// Accepts a Sql Parameter collection and appends Sql input parameters whose values correspond to the provided object properties and MapTo attributes.
		/// </summary>
		/// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql metadata and columns.</typeparam>
		/// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object.</param>
		/// <param name="model">An object model instance. The property values are use as parameter values.</param>
		/// <param name="ignoreParameters">A lists of parameter names that should not be created. Each entry must exactly match the parameter name, including prefix and casing.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		public static DbParameterCollection MapToInParameters<TModel>(this DbParameterCollection parameters, TModel model, HashSet<string> ignoreParameters, ILogger logger) where TModel : class
			=> MapToInParameters<BadShardType, TModel>(parameters, null, model, ignoreParameters, logger);

		/// <summary>
		/// Accepts a Sql Parameter collection and appends Sql input parameters whose values correspond to the provided object properties and MapTo attributes.
		/// </summary>
		/// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql metadata and columns.</typeparam>
		/// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object.</param>
		/// <param name="model">An object model instance. The property values are use as parameter values.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		public static DbParameterCollection MapToInParameters<TShard, TModel>(this DbParameterCollection parameters, TShard shardId, TModel model, ILogger logger) where TModel : class where TShard : IComparable
			=> MapToInParameters<TShard, TModel>(parameters, shardId, model, null, logger);

		/// <summary>
		/// Accepts a Sql Parameter collection and appends Sql input parameters whose values correspond to the provided object properties and MapTo attributes.
		/// </summary>
		/// <typeparam name="TShard">The type of the shard number.</typeparam>
		/// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql metadata and columns.</typeparam>
		/// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object.</param>
		/// <param name="model">An object model instance. The property values are use as parameter values.</param>
		/// <param name="ignoreParameters">A lists of parameter names that should not be created. Each entry must exactly match the parameter name, including prefix and casing.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		public static DbParameterCollection MapToInParameters<TShard, TModel>(this DbParameterCollection parameters, TShard shardId, TModel model, HashSet<string> ignoreParameters, ILogger logger) where TModel : class where TShard : IComparable
		{
			if (ignoreParameters is null)
			{
				ignoreParameters = new HashSet<string>();
			}
			var typeModel = typeof(TModel);
			if (!_cacheInParamSet.TryGetValue(typeModel, out var SqlParameterDelegates))
			{
				LoggingExtensions.SqlInParametersCacheMiss(logger, typeModel);
				SqlParameterDelegates = BuildInMapDelegate<TShard>(typeModel, logger);
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
			((Action<TShard, DbParameterCollection, HashSet<string>, ILogger, object>)SqlParameterDelegates)(shardId, parameters, ignoreParameters, logger, model);
			return parameters;
		}
		#endregion

		#region Set Mapped Output Parameters
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
		#endregion

		#region Get Map Output Parameters
		/// <summary>
		/// Creates a new object with property values based upon the provided output parameters which correspond to the MapTo attributes.
		/// </summary>
		/// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to read the Sql parameter collection values.</typeparam>
		/// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object after a database query.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		/// <returns>An object of the specified type, with properties set to parameter values.</returns>
		public static TModel ReadOutParameters<TModel>(this DbParameterCollection parameters, ILogger logger) where TModel : class, new()
			=> ReadOutParameters<BadShardType, TModel>(parameters, null, logger);

		/// <summary>
		/// Creates a new object with property values based upon the provided output parameters which correspond to the MapTo attributes.
		/// </summary>
		/// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to read the Sql parameter collection values.</typeparam>
		/// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object after a database query.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		/// <returns>An object of the specified type, with properties set to parameter values.</returns>
		public static TModel ReadOutParameters<TShard, TModel>(this DbParameterCollection parameters, TShard shardId, ILogger logger) where TModel : class, new() where TShard : IComparable
		{
			var T = typeof(TModel);
			if (!_getOutParamReadCache.TryGetValue(T, out var SqlOutDelegate))
			{
				LoggingExtensions.SqlReadOutParametersCacheMiss(logger, T);
				SqlOutDelegate = BuildOutGetDelegate<TShard>(parameters, typeof(TModel), logger);
				if (!_getOutParamReadCache.TryAdd(typeof(TModel), SqlOutDelegate))
				{
					SqlOutDelegate = _getOutParamReadCache[typeof(TModel)];
				}
			}
			else
			{
				LoggingExtensions.SqlReadOutParametersCacheHit(logger, T);
			}
			return (TModel)((Func<TShard, DbParameterCollection, ILogger, object>)SqlOutDelegate)(shardId, parameters, logger);
		}

		/// <summary>
		/// Accepts a data reader object and returns a list of objects of the specified type, one for each record.
		/// </summary>
		/// <typeparam name="TModel">The type of the list result</typeparam>
		/// <param name="rdr">The data reader, set to the current result set.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		/// <returns>A list of objects of the specified type, one for each result.</returns>
		public static IList<TModel> FromDataReader<TModel>(DbDataReader rdr, ILogger logger) where TModel : class, new()
			=> FromDataReader<BadShardType, TModel>(null, rdr, logger);

		/// <summary>
		/// Accepts a data reader object and returns a list of objects of the specified type, one for each record.
		/// </summary>
		/// <typeparam name="TModel">The type of the list result</typeparam>
		/// <param name="rdr">The data reader, set to the current result set.</param>
		/// <param name="logger">The logger instance to write any processing or debug information to.</param>
		/// <returns>A list of objects of the specified type, one for each result.</returns>
		public static IList<TModel> FromDataReader<TShard, TModel>(TShard shardId, DbDataReader rdr, ILogger logger) where TModel : class, new() where TShard : IComparable
		{
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
			return ((Func<TShard, DbDataReader, ILogger, List<TModel>>)SqlRdrDelegate)(shardId, rdr, logger);
		}
		#endregion

		#endregion
		#region delegate builders
		private static Action<TShard, DbParameterCollection, HashSet<string>, ILogger, object> BuildInMapDelegate<TShard>(Type tModel, ILogger logger) where TShard : IComparable
		{
			var tShard = typeof(TShard);
			var expressions = new List<Expression>();
			//Create the two delegate parameters: in: sqlParametersCollection and model instance; out: sqlParametersCollection
			ParameterExpression expShard = Expression.Variable(tShard, "shardId");
			ParameterExpression prmSqlPrms = Expression.Variable(typeof(DbParameterCollection), "parameters");
			ParameterExpression prmObjInstance = Expression.Parameter(typeof(object), "model");
			ParameterExpression expIgnoreParameters = Expression.Parameter(typeof(HashSet<string>), "ignoreParameters");
			ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var exprInPrms = new ParameterExpression[] { expShard, prmSqlPrms, expIgnoreParameters, expLogger, prmObjInstance };
			var prmTypedInstance = Expression.TypeAs(prmObjInstance, tModel);  //Our cached delegates accept neither generic nor dynamic arguments. We have to pass object then cast.
			var noDupPrmNameList = new HashSet<string>();
			expressions.Add(prmTypedInstance);
			IterateInMapProperties(tShard, tModel, expressions, prmSqlPrms, prmTypedInstance, expIgnoreParameters, expShard, expLogger, noDupPrmNameList, logger);
			var inBlock = Expression.Block(expressions);
			var lmbIn = Expression.Lambda<Action<TShard, DbParameterCollection, HashSet<string>, ILogger, object>>(inBlock, exprInPrms);
			logger.CreatedExpressionTreeForSetInParameters(tModel, inBlock);
			return lmbIn.Compile();
		}
		private static void IterateInMapProperties(Type tShard, Type tModel, List<Expression> expressions, ParameterExpression prmSqlPrms, Expression expModel, ParameterExpression expIgnoreParameters, ParameterExpression expShardArgument, ParameterExpression expLogger, HashSet<string> noDupPrmNameList, ILogger logger)
		{
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceInMapperProperty));
			foreach (var prop in tModel.GetProperties())
			{
				MemberExpression expProperty = Expression.Property(expModel, prop);
				if (prop.IsDefined(typeof(ParameterMapAttribute), true))
				{
					bool alreadyFound = false;
					var attrPMs = prop.GetCustomAttributes<ParameterMapAttribute>(true);
					foreach (var attrPM in attrPMs)
					{
						if (alreadyFound)
						{
							throw new MultipleMapAttributesException(prop);
						}
						alreadyFound = true;
						expressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name)));
						if (!attrPM.IsValidType(prop.PropertyType))
						{
							throw new InvalidMapTypeException(prop, attrPM.SqlType);
						}
						//MemberExpression expProperty = Expression.Property(expModel, prop);
						attrPM.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expProperty, prop.PropertyType, expLogger, logger);
					}
				}
				else if (prop.IsDefined(typeof(MapToModel)) && !prop.PropertyType.IsValueType)
				{
					//MemberExpression expProperty = Expression.Property(expModel, prop);
					IterateInMapProperties(tShard, prop.PropertyType, expressions, prmSqlPrms, expProperty, expIgnoreParameters, expShardArgument, expLogger, noDupPrmNameList, logger);
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

			var outBlock = Expression.Block(expressions);
			var lmbOut = Expression.Lambda<Action<DbParameterCollection, HashSet<string>, ILogger>>(outBlock, exprOutPrms);
			logger.CreatedExpressionTreeForSetOutParameters(TModel, outBlock);
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
					bool alreadyFound = false;
					var attrPMs = prop.GetCustomAttributes<ParameterMapAttribute>(true);
					foreach (var attrPM in attrPMs)
					{
						if (alreadyFound)
						{
							throw new MultipleMapAttributesException(prop);
						}
						alreadyFound = true;
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


		private static Func<TShard, DbParameterCollection, ILogger, object> BuildOutGetDelegate<TShard>(DbParameterCollection parameters, Type tModel, ILogger logger) where TShard : IComparable
		{
			var tShard = typeof(TShard);
			ParameterExpression expShardArgument = Expression.Variable(tShard, "shardId");
			ParameterExpression expSprocParameters = Expression.Parameter(typeof(DbParameterCollection), "parameters");
			ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var variableExpressions = new List<ParameterExpression>();
			var expModel = Expression.Variable(tModel, "model"); //result variable
			variableExpressions.Add(expModel);
			var expPrm = Expression.Variable(typeof(DbParameter), "prm");
			variableExpressions.Add(expPrm);

			var expressionPrms = new ParameterExpression[] { expShardArgument, expSprocParameters, expLogger };
			var expExitLabel = Expression.Label(tModel);

			var initialExpressions = new List<Expression>()
			{
				Expression.Assign(expModel, Expression.New(tModel)) // var result = new <T>;
            };
			var subsequentExpressions = new List<Expression>();

			IterateGetOutParameters(tShard, tModel, expShardArgument, expSprocParameters, expPrm, variableExpressions, initialExpressions, subsequentExpressions, expModel, expLogger, expExitLabel, logger);

			subsequentExpressions.Add(Expression.Goto(expExitLabel, expModel)); //return value;

			initialExpressions.AddRange(subsequentExpressions);
			initialExpressions.Add(Expression.Label(expExitLabel, Expression.Constant(null, tModel)));
			var expBlock = Expression.Block(variableExpressions, initialExpressions);
			var lambda = Expression.Lambda<Func<TShard, DbParameterCollection, ILogger, object>>(expBlock, expressionPrms);
			logger.CreatedExpressionTreeForReadOutParameters(tModel, expBlock);
			return lambda.Compile();
		}
		private static void IterateGetOutParameters(Type tShard, Type tModel, ParameterExpression expShardArgument, ParameterExpression expSprocParameters, ParameterExpression expPrm, List<ParameterExpression> variableExpressions, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions, Expression expModel, ParameterExpression expLogger, LabelTarget exitLabel, ILogger logger)
		{

			foreach (var prop in tModel.GetProperties())
			{
				if ((prop.IsDefined(typeof(MapShardKeyAttributeBase), true) || prop.IsDefined(typeof(MapShardChildAttributeBase), true)) && prop.IsDefined(typeof(ParameterMapAttribute), true))
				{
					HandleOutPrmShardKeyChild(prop, tShard, tModel, expShardArgument, expSprocParameters, expPrm, variableExpressions, requiredExpressions, nonrequiredExpressions, expModel, expLogger, exitLabel, logger);
				}
				else if (prop.IsDefined(typeof(ParameterMapAttribute), true))
				{
					bool alreadyFound = false;
					var attrPMs = prop.GetCustomAttributes<ParameterMapAttribute>(true);

					foreach (var attrPM in attrPMs)
					{
						if (alreadyFound)
						{
							throw new MultipleMapAttributesException(prop);
						}
						alreadyFound = true;
						HandleOutProperty(attrPM, prop, tModel, expSprocParameters, expPrm, variableExpressions, requiredExpressions, nonrequiredExpressions, expModel, expLogger, exitLabel, logger);
					}
				}
				else if (prop.IsDefined(typeof(MapToModel)) && !prop.PropertyType.IsValueType)
				{
					MemberExpression expProperty = Expression.Property(expModel, prop);
					IterateGetOutParameters(tShard, prop.PropertyType, expShardArgument, expSprocParameters, expPrm, variableExpressions, requiredExpressions, nonrequiredExpressions, expProperty, expLogger, exitLabel, logger);
				}
			}
		}

		private static void HandleOutProperty(ParameterMapAttribute attrPM, PropertyInfo prop, Type tModel, ParameterExpression expSprocParameters, ParameterExpression expPrm, List<ParameterExpression> variableExpressions, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions, Expression expModel, ParameterExpression expLogger, LabelTarget exitLabel, ILogger logger)
		{
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceGetOutMapperProperty));
			var expCallLog = Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name));

			if (!attrPM.IsValidType(prop.PropertyType))
			{
				throw new InvalidMapTypeException(prop, attrPM.SqlType);
			}

			MemberExpression expProperty = Expression.Property(expModel, prop);

			var miGetParameter = typeof(ExpressionHelpers).GetMethod(nameof(ExpressionHelpers.GetParameter), BindingFlags.Static | BindingFlags.NonPublic);
			var expAssign = Expression.Assign(expPrm, Expression.Call(miGetParameter, expSprocParameters, Expression.Constant(attrPM.ParameterName, typeof(string))));

			if (attrPM.IsRequired)
			{
				requiredExpressions.Add(expCallLog);
				requiredExpressions.Add(expAssign);
				requiredExpressions.Add(ExpressionHelpers.ReturnNullIfDbNull(expPrm, attrPM.ParameterName, tModel, exitLabel, expLogger));

				attrPM.AppendReadOutParameterExpressions(expProperty, requiredExpressions, expSprocParameters, expPrm, prop.PropertyType, expLogger, logger);
			}
			else
			{
				nonrequiredExpressions.Add(expCallLog);
				nonrequiredExpressions.Add(expAssign);
				attrPM.AppendReadOutParameterExpressions(expProperty, nonrequiredExpressions, expSprocParameters, expPrm, prop.PropertyType, expLogger, logger);
			}

		}
		private static void HandleOutVariable(ParameterMapAttribute attrPM, ParameterExpression var, Type tModel, ParameterExpression expSprocParameters, ParameterExpression expPrm, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions, ParameterExpression expLogger, LabelTarget exitLabel, ILogger logger)
		{
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceGetOutMapperProperty));
			if (!attrPM.IsValidType(var.Type))
			{
				throw new InvalidMapTypeException(var.Name, var.Type, attrPM.SqlType);
			}

			var miGetParameter = typeof(ExpressionHelpers).GetMethod(nameof(ExpressionHelpers.GetParameter), BindingFlags.Static | BindingFlags.NonPublic);
			var expAssign = Expression.Assign(expPrm, Expression.Call(miGetParameter, expSprocParameters, Expression.Constant(attrPM.ParameterName, typeof(string))));
			if (attrPM.IsRequired)
			{
				requiredExpressions.Add(expAssign);
				attrPM.AppendReadOutParameterExpressions(var, requiredExpressions, expSprocParameters, expPrm, var.Type, expLogger, logger);
				requiredExpressions.Add(ExpressionHelpers.ReturnNullIfDbNull(expPrm, attrPM.ParameterName, tModel, exitLabel, expLogger));
			}
			else
			{
				nonrequiredExpressions.Add(expAssign);
				attrPM.AppendReadOutParameterExpressions(var, nonrequiredExpressions, expSprocParameters, expPrm, var.Type, expLogger, logger);
			}

		}

		private static void HandleOutPrmShardKeyChild(PropertyInfo prop, Type tArgShard, Type tModel, ParameterExpression expShardArgument, ParameterExpression expSprocParameters, ParameterExpression expPrm, List<ParameterExpression> variableExpressions, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions, Expression expModel, ParameterExpression expLogger, LabelTarget exitLabel, ILogger logger)
		{
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceGetOutMapperProperty));

			MemberExpression expShardProperty = Expression.Property(expModel, prop);
			var propType = prop.PropertyType;
			var isNullableShardType = (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>));

			if (isNullableShardType)
			{
				propType = Nullable.GetUnderlyingType(propType);
			}
			var isShardKey = propType.IsGenericType && propType.Name == "ShardKey`2" && prop.IsDefined(typeof(MapShardKeyAttributeBase), true);
			var isShardChild = propType.IsGenericType && propType.Name == "ShardChild`3" && prop.IsDefined(typeof(MapShardChildAttributeBase), true);

			if (isShardKey || isShardChild)
			{
				var tShardId = propType.GetProperty(nameof(ShardKey<int, int>.ShardId)).PropertyType;
				var tRecordId = propType.GetProperty(nameof(ShardKey<int, int>.RecordId)).PropertyType;
				Type tChildId = null;
				if (tArgShard != tShardId)
				{
					throw new Exception($"The ShardId data type found in property {prop.Name} on model {tModel.Name} is of type {tShardId.Name } but the caller expected type {tArgShard.Name}.");
				}
				if (isShardChild)
				{
					tChildId = propType.GetProperty(nameof(ShardChild<int, int, int>.ChildId)).PropertyType;
				}

				ParameterExpression expDataShardId;
				ParameterExpression expDataRecordId;
				ParameterExpression expDataChildId = null;

				if (tShardId.IsValueType)
				{
					expDataShardId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tShardId), prop.Name + "_ShardId");
				}
				else
				{
					expDataShardId = Expression.Variable(tShardId, "dataShardId_" + prop.Name);
				}
				variableExpressions.Add(expDataShardId);

				if (tRecordId.IsValueType)
				{
					expDataRecordId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tRecordId), prop.Name + "_RecordId");
				}
				else
				{
					expDataRecordId = Expression.Variable(tRecordId, "dataRecordId_" + prop.Name);
				}
				variableExpressions.Add(expDataRecordId);

				if (isShardChild)
				{
					if (tChildId.IsValueType)
					{
						expDataChildId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tChildId), prop.Name + "_ChildId");
					}
					else
					{
						expDataChildId = Expression.Variable(tChildId, "dataChildId_" + prop.Name);
					}
					variableExpressions.Add(expDataChildId);
				}

				string shardParameterName = null;
				string recordParameterName = null;
				string childParameterName = null;
				NewExpression expDataOrigin;
				if (isShardChild)
				{
					var shardPM = prop.GetCustomAttribute<MapShardChildAttributeBase>(true);
					shardParameterName = shardPM.ShardIdParameterName;
					recordParameterName = shardPM.RecordIdParameterName;
					childParameterName = shardPM.ChildIdParameterName;
					expDataOrigin = Expression.New(typeof(DataOrigin).GetConstructor(new[] { typeof(char) }), new[] { Expression.Constant(shardPM.Origin.SourceIndicator, typeof(char)) });
				}
				else
				{
					var shardPM = prop.GetCustomAttribute<MapShardKeyAttributeBase>(true);
					shardParameterName = shardPM.ShardIdParameterName;
					recordParameterName = shardPM.RecordIdParameterName;
					expDataOrigin = Expression.New(typeof(DataOrigin).GetConstructor(new[] { typeof(char) }), new[] { Expression.Constant(shardPM.Origin.SourceIndicator, typeof(char)) });
				}

				//ShardId
				var attrPMs = prop.GetCustomAttributes<ParameterMapAttribute>(true);
				bool shardIdFound = false;
				if (!string.IsNullOrEmpty(shardParameterName))
				{
					foreach (var attrPM in attrPMs)
					{
						List<Expression> expressions;
						if (attrPM.IsRequired)
						{
							expressions = requiredExpressions;
						}
						else
						{
							expressions = nonrequiredExpressions;
						}
						if (attrPM.ParameterName == shardParameterName)
						{
							expressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(expDataShardId.Name)));
							shardIdFound = true;
							if (!attrPM.IsValidType(tShardId))
							{
								throw new InvalidMapTypeException(expDataShardId.Name, tShardId, attrPM.SqlType);
							}
							var miGetParameter = typeof(ExpressionHelpers).GetMethod(nameof(ExpressionHelpers.GetParameter), BindingFlags.Static | BindingFlags.NonPublic);
							var expAssign = Expression.Assign(expPrm, Expression.Call(miGetParameter, expSprocParameters, Expression.Constant(attrPM.ParameterName, typeof(string))));
							//var lstShardExp = new List<Expression>();
							//lstShardExp.Add(expAssign);
							expressions.Add(expAssign);

							if (attrPM.IsRequired)
							{
								requiredExpressions.Add(ExpressionHelpers.ReturnNullIfDbNull(expPrm, attrPM.ParameterName, tModel, exitLabel, expLogger));
							}
							attrPM.AppendReadOutParameterExpressions(expDataShardId, expressions, expSprocParameters, expPrm, expDataShardId.Type, expLogger, logger);
							if (tShardId.IsValueType)
							{
								expressions.Add(Expression.IfThen(
									//if
									Expression.Not(Expression.Property(expDataShardId, expDataShardId.Type.GetProperty(nameof(Nullable<int>.HasValue)))),
									//then
									Expression.Assign(expDataShardId, Expression.Convert(expShardArgument, expDataShardId.Type))
									//Expression.Assign(Expression.Property(expDataShardId, expDataShardId.Type.GetProperty(nameof(Nullable<int>.Value))), expShardArgument)
									));
							}
							else
							{
								expressions.Add(Expression.IfThen(
									//if
									Expression.Not(Expression.Property(expDataShardId, expDataShardId.Type.GetProperty(nameof(Nullable<int>.HasValue)))),
									//then
									Expression.Assign(expDataShardId, expShardArgument)
									));
							}
							break;
						}
					}
					if (!shardIdFound)
					{
						throw new Exception($"The shard map attribute specifies a ShardId parameter name of \"{ shardParameterName }\" but no corresponding MapTo attribute was found with this parameter name.");
					}
				}
				else
				{
					if (tShardId.IsValueType)
					{
						nonrequiredExpressions.Add(Expression.Assign(expDataShardId, Expression.Convert(expShardArgument, expDataShardId.Type)));
					}
					else
					{
						nonrequiredExpressions.Add(Expression.Assign(expDataShardId, expShardArgument));
					}
				}
				//RecordId
				var recordIdFound = false;
				foreach (var attrPM in attrPMs)
				{
					if (attrPM.ParameterName == recordParameterName)
					{
						recordIdFound = true;
						if (attrPM.IsRequired)
						{
							requiredExpressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(expDataRecordId.Name)));
						}
						else
						{
							nonrequiredExpressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(expDataRecordId.Name)));
						}
						HandleOutVariable(attrPM, expDataRecordId, tModel, expSprocParameters, expPrm, requiredExpressions, nonrequiredExpressions, expLogger, exitLabel, logger);
						break;
					}
				}
				if (!recordIdFound)
				{
					throw new Exception($"The shard map attribute specifies a RecordId parameter name of \"{ recordParameterName }\" but no corresponding MapTo attribute was found with this parameter name.");
				}
				if (isShardChild)
				{
					var childIdFound = false;
					foreach (var attrPM in attrPMs)
					{
						if (attrPM.ParameterName == childParameterName)
						{
							if (attrPM.IsRequired)
							{
								requiredExpressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(expDataChildId.Name)));
							}
							else
							{
								nonrequiredExpressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(expDataChildId.Name)));
							}
							childIdFound = true;
							HandleOutVariable(attrPM, expDataChildId, tModel, expSprocParameters, expPrm, requiredExpressions, nonrequiredExpressions, expLogger, exitLabel, logger);
							break;
						}
					}
					if (!childIdFound)
					{
						throw new Exception($"The shard map attribute specifies a ChildId parameter name of \"{ childParameterName }\" but no corresponding MapTo attribute was found with this parameter name.");
					}
				}
				var expHasNoNulls = Expression.AndAlso(Expression.NotEqual(expDataShardId, Expression.Constant(null, expDataShardId.Type)),
						Expression.NotEqual(expDataRecordId, Expression.Constant(null, expDataRecordId.Type)));
				if (isShardChild)
				{
					expHasNoNulls = Expression.AndAlso(expHasNoNulls,
						Expression.NotEqual(expDataChildId, Expression.Constant(null, expDataChildId.Type)));
				}

				Type[] constructorTypes;
				Expression[] constructorArgs;
				//Type tShard;
				if (isShardChild)
				{
					constructorTypes = new[] { typeof(DataOrigin), tShardId, tRecordId, tChildId };
					constructorArgs = new Expression[4];
					//tShard = typeof(ShardChild<,,>).MakeGenericType(new Type[] { tShardId, tRecordId, tChildId });
				}
				else
				{
					constructorTypes = new[] { typeof(DataOrigin), tShardId, tRecordId };
					constructorArgs = new Expression[3];
					//tShard = typeof(ShardKey<,>).MakeGenericType(new Type[] { tShardId, tRecordId });
				}
				constructorArgs[0] = expDataOrigin;

				if (tShardId.IsValueType)
				{
					constructorArgs[1] = Expression.Property(expDataShardId, expDataShardId.Type.GetProperty(nameof(Nullable<int>.Value)));
				}
				else
				{
					constructorArgs[1] = expDataShardId;
				}
				if (tRecordId.IsValueType)
				{
					constructorArgs[2] = Expression.Property(expDataRecordId, expDataRecordId.Type.GetProperty(nameof(Nullable<int>.Value)));
				}
				else
				{
					constructorArgs[2] = expDataRecordId;
				}
				if (isShardChild)
				{
					if (tChildId.IsValueType)
					{
						constructorArgs[3] = Expression.Property(expDataChildId, expDataChildId.Type.GetProperty(nameof(Nullable<int>.Value)));
					}
					else
					{
						constructorArgs[3] = expDataChildId;
					}
				}
				var expNewShardInstance = Expression.New(propType.GetConstructor(constructorTypes), constructorArgs);
				Expression expActionIfNull;
				if (isNullableShardType)
				{
					expActionIfNull = Expression.Assign(expShardProperty, Expression.Constant(null, expShardProperty.Type));
					expNewShardInstance = Expression.New(typeof(Nullable<>).MakeGenericType(propType).GetConstructor(new Type[] { propType }), new Expression[] { expNewShardInstance });
				}
				else
				{
					expActionIfNull = Expression.Assign(expShardProperty, Expression.Property(null, propType.GetProperty(nameof(ShardKey<int, int>.Empty))));
				}
				nonrequiredExpressions.Add(Expression.IfThenElse(
					expHasNoNulls,
					Expression.Assign(expShardProperty, expNewShardInstance),
					expActionIfNull
					));
			}
		}


		private static Func<TShard, DbDataReader, ILogger, List<TModel>> BuildReaderMapDelegate<TShard, TModel>(ILogger logger) where TShard : IComparable
		{
			var tModel = typeof(TModel);
			var tShard = typeof(TShard);
			ParameterExpression expShardArgument = Expression.Variable(tShard, "shardId");
			ParameterExpression prmSqlRdr = Expression.Parameter(typeof(DbDataReader), "rdr"); //input param
			ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var variableExpressions = new List<ParameterExpression>();
			var expModel = Expression.Parameter(typeof(TModel), "model"); //model variable
			variableExpressions.Add(expModel);
			var expListResult = Expression.Parameter(typeof(List<TModel>), "result"); //list result variable
			variableExpressions.Add(expListResult);
			var expOrdinal = Expression.Variable(typeof(int), "ordinal");
			variableExpressions.Add(expOrdinal);
			var expOrdinals = Expression.Variable(typeof(int[]), "ordinals");
			variableExpressions.Add(expOrdinals);

			var expressions = new List<Expression>();
			var columnLookupExpressions = new List<MethodCallExpression>();
			var expressionPrms = new ParameterExpression[] { expShardArgument, prmSqlRdr, expLogger };


			//MethodInfos for subsequent Expression calls
			var miGetFieldOrdinal = typeof(ExpressionHelpers).GetMethod(nameof(ExpressionHelpers.GetFieldOrdinal), BindingFlags.NonPublic | BindingFlags.Static);
			var miRead = typeof(DbDataReader).GetMethod(nameof(DbDataReader.Read));
			var miGetFieldValue = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue));
			var miListAdd = typeof(List<TModel>).GetMethod(nameof(List<TModel>.Add));

			//create List<TModel> result

			var propIndex = 0;
			var resultExpressions = new List<Expression>();

			var expAssign = Expression.Assign(expModel, Expression.New(typeof(TModel)));
			expressions.Add(expAssign);

			//Loop through all object properties:
			IterateRdrColumns(tShard, tModel, expModel, columnLookupExpressions, expressions, prmSqlRdr, expOrdinals, expOrdinal, ref propIndex, expLogger, logger);

			var expAddList = Expression.Call(expListResult, miListAdd, expModel);
			expressions.Add(expAddList); //ResultList.Add(model);

			resultExpressions.Add(Expression.Assign(expOrdinals, Expression.NewArrayInit(typeof(int), columnLookupExpressions.ToArray())));

			var loopLabel = Expression.Label("readNextRow");
			resultExpressions.Add(Expression.Assign(expListResult, Expression.New(typeof(List<TModel>)))); // var result = new List<T>;
			resultExpressions.Add(Expression.Loop(
					Expression.IfThenElse(Expression.Call(prmSqlRdr, miRead),
						Expression.Block(expressions),
						Expression.Break(loopLabel)
					), loopLabel));
			resultExpressions.Add(expListResult); //return type

			var expBlock = Expression.Block(typeof(List<TModel>), variableExpressions, resultExpressions);
			var lambda = Expression.Lambda<Func<TShard, DbDataReader, ILogger, List<TModel>>>(expBlock, expressionPrms);
			logger.CreatedExpressionTreeForReader(tModel, expBlock);
			return lambda.Compile();
		}
		private static void IterateRdrColumns(Type tShard, Type TModel, Expression expModel, List<MethodCallExpression> columnLookupExpressions, List<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, ParameterExpression expLogger, ILogger logger)
		{
			//TODO: handle tShard
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceRdrMapperProperty));
			foreach (var prop in TModel.GetProperties())
			{
				if (prop.IsDefined(typeof(ParameterMapAttribute), true))
				{
					var alreadyFound = false;
					var attrPMs = prop.GetCustomAttributes<ParameterMapAttribute>(true);
					foreach (var attrPM in attrPMs)
					{
						if (alreadyFound)
						{
							throw new MultipleMapAttributesException(prop);
						}
						alreadyFound = true;
						expressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name)));

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
					IterateRdrColumns(tShard, prop.PropertyType, expProperty, columnLookupExpressions, expressions, prmSqlRdr, expOrdinals, expOrdinal, ref propIndex, expLogger, logger);
				}
			}

		}
		#endregion
		#region Convert Sql result to object(s)

		//Make model out of out parameters only
		public static TModel QueryResultsHandler<TShard, TModel>
			(
			TShard shardId,
			string sprocName,
			object notUsed,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TShard : IComparable
			where TModel : class, new()
			=> QueryResultsHandler<TShard, TModel, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, TModel>(shardId, sprocName, null, rdr, parameters, connectionDescription, logger);

		//To Make model out of reader result, set TOutParamaters type to DummyType or something like it.
		public static TModel QueryResultsHandler<TShard, TModel, TReaderResult, TOutParameters>
			(
			TShard shardId,
			string sprocName,
			object notUsed,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TShard : IComparable
			where TReaderResult : class, new()
			where TOutParameters : class, new()
			where TModel : class, new()
			=> QueryResultsHandler<TShard, TModel, TReaderResult, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, TOutParameters>(shardId, sprocName, null, rdr, parameters, connectionDescription, logger);

		public static TModel QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TOutParameters>
			(
			TShard shardId,
			string sprocName,
			object notUsed,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TShard : IComparable
			where TReaderResult0 : class, new()
			where TReaderResult1 : class, new()
			where TOutParameters : class, new()
			where TModel : class, new()
			=> QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, TOutParameters>(shardId, sprocName, null, rdr, parameters, connectionDescription, logger);

		public static TModel QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TOutParameters>
			(
			TShard shardId,
			string sprocName,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TShard : IComparable
			where TReaderResult0 : class, new()
			where TReaderResult1 : class, new()
			where TReaderResult2 : class, new()
			where TOutParameters : class, new()
			where TModel : class, new()
			=> QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, DummyType, DummyType, DummyType, DummyType, DummyType, TOutParameters>(shardId, sprocName, null, rdr, parameters, connectionDescription, logger);

		public static TModel QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TOutParameters>
			(
			TShard shardId,
			string sprocName,
			object notUsed,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TShard : IComparable
			where TReaderResult0 : class, new()
			where TReaderResult1 : class, new()
			where TReaderResult2 : class, new()
			where TReaderResult3 : class, new()
			where TOutParameters : class, new()
			where TModel : class, new()
			=> QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, DummyType, DummyType, DummyType, DummyType, TOutParameters>(shardId, sprocName, null, rdr, parameters, connectionDescription, logger);

		public static TModel QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TOutParameters>
			(
			TShard shardId,
			string sprocName,
			object notUsed,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TShard : IComparable
			where TReaderResult0 : class, new()
			where TReaderResult1 : class, new()
			where TReaderResult2 : class, new()
			where TReaderResult3 : class, new()
			where TReaderResult4 : class, new()
			where TOutParameters : class, new()
			where TModel : class, new()
			=> QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, DummyType, DummyType, DummyType, TOutParameters>(shardId, sprocName, null, rdr, parameters, connectionDescription, logger);

		public static TModel QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TOutParameters>
			(
			TShard shardId,
			string sprocName,
			object notUsed,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TShard : IComparable
			where TReaderResult0 : class, new()
			where TReaderResult1 : class, new()
			where TReaderResult2 : class, new()
			where TReaderResult3 : class, new()
			where TReaderResult4 : class, new()
			where TReaderResult5 : class, new()
			where TOutParameters : class, new()
			where TModel : class, new()
			=> QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, DummyType, DummyType, TOutParameters>(shardId, sprocName, null, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel>
		//	(
		//	TShard shardId,
		//	string sprocName,
		//	TArg optionalArgument,
		//	DbDataReader rdr,
		//	DbParameterCollection parameters,
		//	string connectionDescription,
		//	ILogger logger)
		//	where TModel : class, new()
		//	=> QueryResultsHandler<TShard, TArg, TModel, TModel, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType>(shardId, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1>
		//	(
		//	TShard shardId,
		//	string sprocName,
		//	TArg optionalArgument,
		//	DbDataReader rdr,
		//	DbParameterCollection parameters,
		//	string connectionDescription,
		//	ILogger logger)
		//	where TReaderResult0 : class, new()
		//	where TReaderResult1 : class, new()
		//	where TModel : class, new()
		//	=> QueryResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType>(shardId, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2>
		//	(
		//	TShard shardId,
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
		//	=> QueryResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType>(shardId, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
		//	(
		//	TShard shardId,
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
		//	=> QueryResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, DummyType, DummyType, DummyType, DummyType, DummyType>(shardId, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
		//	(
		//	TShard shardId,
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
		//	=> QueryResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, DummyType, DummyType, DummyType, DummyType>(shardId, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
		//	(
		//	TShard shardId,
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
		//	=> QueryResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, DummyType, DummyType, DummyType>(shardId, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
		//	(
		//	TShard shardId,
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
		//	=> QueryResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, DummyType, DummyType>(shardId, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//public static TModel SqlRdrResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
		//	(
		//	TShard shardId,
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
		//	=> QueryResultsHandler<TShard, TArg, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, DummyType>(shardId, sprocName, optionalArgument, rdr, parameters, connectionDescription, logger);

		//Full implementation for up to 8 results (plus output) 

		/// <summary>
		/// A function whose signature cooresponds to delegate QueryResultModelHandler and is used to map the provided model type(s) to query results.
		/// </summary>
		/// <typeparam name="TShard">The type of the shardId value. Can be any value type if not used.</typeparam>
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
		/// <param name="shardId">This value will be provided to ShardKey or ShardChild objects. If not using sharded data, any provided value will be ignored.</param>
		/// <param name="sprocName">The name of the stored procedure is used to cache the mapping metadata and also for provide richer logging information.</param>
		/// <param name="notUsed">This parameter is required to conform to the QueryResultModelHandler delegate signature. This argument should be null.</param>
		/// <param name="rdr">The data reader returned by the query.</param>
		/// <param name="parameters">The output parameters returned by the query.</param>
		/// <param name="connectionDescription">The connection description is used to enrich logging information.</param>
		/// <param name="logger">The logging instance to use for any logging requirements.</param>
		/// <returns>An instance of TResult, with properties matching the provided data.</returns>
		public static TModel QueryResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutResult>
			(
			TShard shardId,
			string sprocName,
			object notUsed,
			DbDataReader rdr,
			DbParameterCollection parameters,
			string connectionDescription,
			ILogger logger)
			where TShard : IComparable
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
				resultList0 = Mapper.FromDataReader<TShard, TReaderResult0>(shardId, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult1) != dummy)
			{
				resultList1 = Mapper.FromDataReader<TShard, TReaderResult1>(shardId, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult2) != dummy)
			{
				resultList2 = Mapper.FromDataReader<TShard, TReaderResult2>(shardId, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult3) != dummy)
			{
				resultList3 = Mapper.FromDataReader<TShard, TReaderResult3>(shardId, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult4) != dummy)
			{
				resultList4 = Mapper.FromDataReader<TShard, TReaderResult4>(shardId, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult5) != dummy)
			{
				resultList5 = Mapper.FromDataReader<TShard, TReaderResult5>(shardId, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult6) != dummy)
			{
				resultList6 = Mapper.FromDataReader<TShard, TReaderResult6>(shardId, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (hasNextResult && typeof(TReaderResult7) != dummy)
			{
				resultList7 = Mapper.FromDataReader<TShard, TReaderResult7>(shardId, rdr, logger);
				hasNextResult = rdr.NextResult();
			}
			if (typeof(TOutResult) != dummy)
			{
				resultOutPrms = Mapper.ReadOutParameters<TOutResult>(parameters, logger);
			}


			var queryKey = typeof(TModel).ToString() + sprocName;
			if (!_getObjectCache.TryGetValue(queryKey, out var sqlObjectDelegate))
			{
				sqlObjectDelegate = BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutResult>(shardId, sprocName, logger);
				if (!_getObjectCache.TryAdd(queryKey, sqlObjectDelegate))
				{
					sqlObjectDelegate = _getObjectCache[queryKey];
				}
			}
			var sqlObjectDelegate2 = (Func<TShard, string, IList<TReaderResult0>, IList<TReaderResult1>, IList<TReaderResult2>, IList<TReaderResult3>, IList<TReaderResult4>, IList<TReaderResult5>, IList<TReaderResult6>, IList<TReaderResult7>, TOutResult, ILogger, TModel>)sqlObjectDelegate;
			//return (TModel)sqlObjectDelegate(sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultList6, resultList7, resultOutPrms, logger);
			return (TModel)sqlObjectDelegate2(shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultList6, resultList7, resultOutPrms, logger);

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

		private static Func<TShard, string, IList<TReaderResult0>, IList<TReaderResult1>, IList<TReaderResult2>, IList<TReaderResult3>, IList<TReaderResult4>, IList<TReaderResult5>, IList<TReaderResult6>, IList<TReaderResult7>, TOutResult, ILogger, TModel> BuildModelFromResultsExpressions<
			TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutResult>
			(TShard shardId, string procedureName, ILogger logger)
			where TShard : IComparable
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
					expressions.Add(Expression.Assign(expModel, expResultOut));
					isPrmOutUsed = true;
				}
				else if (resultType == typeof(TReaderResult0))
				{
					expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult0), resultType }), expProcName, expResultSet0, expLogger)));
					expressions.Add(Expression.IfThen(
						Expression.Equal(expModel, expNull), //if (result == null)
						Expression.Return(expExitTarget, expNull, typeof(TModel)) //return null;
						));
					isRdrResult0Used = true;
				}
				else if (resultType == typeof(TReaderResult1))
				{
					expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult1), resultType }), expProcName, expResultSet1, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel))));
					isRdrResult1Used = true;
				}
				else if (resultType == typeof(TReaderResult2))
				{
					expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult2), resultType }), expProcName, expResultSet2, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel))));
					isRdrResult2Used = true;
				}
				else if (resultType == typeof(TReaderResult3))
				{
					expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult3), resultType }), expProcName, expResultSet3, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel))));
					isRdrResult3Used = true;
				}
				else if (resultType == typeof(TReaderResult4))
				{
					expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult4), resultType }), expProcName, expResultSet4, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel))));
					isRdrResult4Used = true;
				}
				else if (resultType == typeof(TReaderResult5))
				{
					expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult5), resultType }), expProcName, expResultSet5, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel))));
					isRdrResult5Used = true;
				}
				else if (resultType == typeof(TReaderResult6))
				{
					expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult6), resultType }), expProcName, expResultSet6, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel))));
					isRdrResult6Used = true;
				}
				else if (resultType == typeof(TReaderResult7))
				{
					expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult7), resultType }), expProcName, expResultSet7, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel))));
					isRdrResult7Used = true;
				}
				else
				{
					//match not found, so just create a new instance and we'll try again on properties
					expressions.Add(Expression.Assign(expModel, Expression.New(resultType)));
				}

				var props = resultType.GetProperties();

				//Iterate over any List<> properties and set any List<resultSet> that match.
				foreach (var prop in props)
				{
					if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult0>)) && !isRdrResult0Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet0));
						isRdrResult0Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult1>)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet1));
						isRdrResult1Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult2>)) && !isRdrResult2Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet2));
						isRdrResult2Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult3>)) && !isRdrResult3Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet3));
						isRdrResult3Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult4>)) && !isRdrResult4Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet4));
						isRdrResult4Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult5>)) && !isRdrResult5Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet5));
						isRdrResult5Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult6>)) && !isRdrResult6Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet6));
						isRdrResult6Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult7>)) && !isRdrResult7Used)
					{
						expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet7));
						isRdrResult7Used = true;
					}
				}
				//Iterate over any object (non-list) properties and set any resultSet that match.
				foreach (var prop in props)
				{
					if (prop.PropertyType.IsAssignableFrom(typeof(TOutResult)) && !isPrmOutUsed)
					{
						expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultOut));
						isPrmOutUsed = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult0)) && !isRdrResult0Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));   //var count = 0;
						expressions.Add(Expression.IfThen(                                      //if (resultSet0 != null)
							Expression.NotEqual(expResultSet0, expNull),                        //{ 
							Expression.Assign(expCount, Expression.Property(expResultSet0, miCount))    //count = resultSet0.Count;
							));                                                                 //}
						expressions.Add(Expression.IfThenElse(                                  //if (count == 1)
							Expression.Equal(expCount, expOne),                                 //{ result.prop = resultSet0[0]; }
							Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet0, "Item", Expression.Constant(0))),
							Expression.IfThen(                                                  //else if (count > 1)
								Expression.GreaterThan(expCount, expOne),                       //{       logger.LogError("");
								Expression.Call(miLogError, Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))
								));                                                               //}
						isRdrResult0Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult0)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet0, expNull), Expression.Assign(expCount, Expression.Property(expResultSet0, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet0, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult0Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult1)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet1, expNull), Expression.Assign(expCount, Expression.Property(expResultSet1, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet1, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult1Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult2)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet2, expNull), Expression.Assign(expCount, Expression.Property(expResultSet2, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet2, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult2Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult3)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet3, expNull), Expression.Assign(expCount, Expression.Property(expResultSet3, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet3, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult3Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult4)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet4, expNull), Expression.Assign(expCount, Expression.Property(expResultSet4, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet4, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult4Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult5)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet5, expNull), Expression.Assign(expCount, Expression.Property(expResultSet5, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet5, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult5Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult6)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet6, expNull), Expression.Assign(expCount, Expression.Property(expResultSet6, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet6, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult6Used = true;
					}
					else if (prop.PropertyType.IsAssignableFrom(typeof(TReaderResult7)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet7, expNull), Expression.Assign(expCount, Expression.Property(expResultSet7, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet7, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult7Used = true;
					}

				}
				var expExit = Expression.Label(expExitTarget);
				expressions.Add(expExit); //Exit procedure
			}

			expressions.Add(expModel); //return model
			var expBlock = Expression.Block(resultType, new ParameterExpression[] { expModel, expCount }, expressions); //+variables

			var lambda = Expression.Lambda<Func<TShard, string, IList<TReaderResult0>, IList<TReaderResult1>, IList<TReaderResult2>, IList<TReaderResult3>, IList<TReaderResult4>, IList<TReaderResult5>, IList<TReaderResult6>, IList<TReaderResult7>, TOutResult, ILogger, TModel>>
				(expBlock, new ParameterExpression[] { expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expResultSet6, expResultSet7, expResultOut, expLogger }); //+parameters
			return lambda.Compile();
		}

		//private static ShardChild<TShard, TRecord, TChild> HandleShardChildInParameters<TShard, TRecord, TChild>(string shardParameterName, string recordParameterName, string childParameterName, DbParameterCollection parameters, TShard shardIdArgument) where TShard : IComparable where TRecord : IComparable where TChild : IComparable
		//{
		//	if (!string.IsNullOrEmpty(shardParameterName) && parameters.Contains(shardParameterName))
		//	{

		//	}

		//	var result = new ShardChild<TShard, TRecord, TChild>();

		//	return result;
		//}

		//private static TShard FindShardParameterOrUseArgument<TShard>(TShard shardArgument, DbParameterCollection parameters, string shardParameterName)
		//{
		//	TShard shardId;
		//	if (parameters.Contains(shardParameterName))
		//	{
		//		shardId = parameters[shardParameterName].Value;
		//	}
		//	else
		//	{
		//		shardId = shardArgument;
		//	}
		//}

		#endregion
		private class BadShardType : IComparable
		{
			public int CompareTo(object obj)
			{
				throw new NotImplementedException();
			}
		}

		public class DummyType
		{
		}
	}
}
