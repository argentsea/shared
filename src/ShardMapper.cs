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
	public static class ShardMapperExtensions
	{
		public static DbParameterCollection MapToInParameters<TShard, TModel>(this DbParameterCollection prms, TModel model, ILogger logger) where TShard : IComparable where TModel : class
		{
			return ShardMapper<TShard>.MapToInParameters<TModel>(prms, model, logger);
		}
		public static DbParameterCollection MapToInParameters<TShard, TModel>(this DbParameterCollection prms, TModel model, HashSet<string> ignoreParameters, ILogger logger) where TShard : IComparable where TModel : class
		{
			return ShardMapper<TShard>.MapToInParameters<TModel>(prms, model, ignoreParameters, logger);
		}
		public static DbParameterCollection MapToOutParameters<TShard>(this DbParameterCollection prms, Type modelT, ILogger logger) where TShard : IComparable
		{
			return ShardMapper<TShard>.MapToOutParameters(prms, modelT, logger);
		}
		public static DbParameterCollection MapToOutParameters<TShard>(this DbParameterCollection prms, Type modelT, HashSet<string> ignoreParameters, ILogger logger) where TShard : IComparable
		{
			return ShardMapper<TShard>.MapToOutParameters(prms, modelT, ignoreParameters, logger);
		}
	}

	public static class ShardMapper<TShard>
    {
        private static ConcurrentDictionary<Type, Action<DbParameterCollection, HashSet<string>, ILogger, object>> _cacheInParamSet = new ConcurrentDictionary<Type, Action<DbParameterCollection, HashSet<string>, ILogger, object>>();
        private static ConcurrentDictionary<Type, Action<DbParameterCollection, HashSet<string>, ILogger>> _cacheOutParamSet = new ConcurrentDictionary<Type, Action<DbParameterCollection, HashSet<string>, ILogger>>();
        private static ConcurrentDictionary<Type, Func<TShard, DbDataReader, ILogger, object>> _getRdrParamCache = new ConcurrentDictionary<Type, Func<TShard, DbDataReader, ILogger, object>>();
        private static ConcurrentDictionary<Type, Func<TShard, DbParameterCollection, ILogger, object>> _getOutParamReadCache = new ConcurrentDictionary<Type, Func<TShard, DbParameterCollection, ILogger, object>>();
        private static ConcurrentDictionary<string, Func<TShard, string, object, object, object, object, object, object, object, object, object, ILogger, object>> _getObjectCache = new ConcurrentDictionary<string, Func<TShard, string, object, object, object, object, object, object, object, object, object, ILogger, object>>();

        #region Public methods

        /// <summary>
        /// Accepts a Sql Parameter collection and appends Sql input parameters whose values correspond to the provided object properties and MapTo attributes.
        /// </summary>
        /// <typeparam name="modelT">The type of the object. The "MapTo" attributes are used to create the Sql metadata and columns.</typeparam>
        /// <param name="prms">A parameter collection, generally belonging to a ADO.Net Command object.</param>
        /// <param name="model">An object model instance. The property values are use as parameter values.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        public static DbParameterCollection MapToInParameters<T>(DbParameterCollection prms, T model, ILogger logger) where T : class
            => MapToInParameters<T>(prms, model, null, logger);

        /// <summary>
        /// Accepts a Sql Parameter collection and appends Sql input parameters whose values correspond to the provided object properties and MapTo attributes.
        /// </summary>
        /// <typeparam name="modelT">The type of the object. The "MapTo" attributes are used to create the Sql metadata and columns.</typeparam>
        /// <param name="prms">A parameter collection, generally belonging to a ADO.Net Command object.</param>
        /// <param name="model">An object model instance. The property values are use as parameter values.</param>
        /// <param name="ignoreParameters">A lists of parameter names that should not be created. Each entry must start with '@' to be a valid parameter.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        public static DbParameterCollection MapToInParameters<T>(DbParameterCollection prms, T model, HashSet<string> ignoreParameters, ILogger logger) where T : class
        {
            var modelT = typeof(T);
            if (!_cacheInParamSet.TryGetValue(modelT, out var SqlParameterDelegates))
            {
                SqlLoggerExtensions.SqlInParametersCacheMiss(logger, modelT);
                SqlParameterDelegates = BuildInMapDelegate(modelT, logger);
                if (!_cacheInParamSet.TryAdd(modelT, SqlParameterDelegates))
                {
                    SqlParameterDelegates = _cacheInParamSet[modelT];
                }
            }
            else
            {
                SqlLoggerExtensions.SqlInParametersCacheHit(logger, modelT);
            }
            foreach (DbParameter prm in prms)
            {
                if (!ignoreParameters.Contains(prm.ParameterName))
                {
                    ignoreParameters.Add(prm.ParameterName);
                }
            }
            SqlParameterDelegates(prms, ignoreParameters, logger, model);
			return prms;
        }

        /// <summary>
        /// Accepts a Sql Parameter collection and appends Sql output parameters corresponding to the MapTo attributes.
        /// </summary>
        /// <typeparam name="modelT">The type of the object. The "MapTo" attributes are used to create the Sql parameter types.</typeparam>
        /// <param name="prms">A parameter collection, generally belonging to a ADO.Net Command object.</param>
        /// <param name="model">The type of the model.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        public static DbParameterCollection MapToOutParameters(DbParameterCollection prms, Type modelT, ILogger logger)
            => MapToOutParameters(prms, modelT, null, logger);

        /// <summary>
        /// Accepts a Sql Parameter collection and appends Sql output parameters corresponding to the MapTo attributes.
        /// </summary>
        /// <typeparam name="modelT">The type of the object. The "MapTo" attributes are used to create the Sql parameter types.</typeparam>
        /// <param name="prms">A parameter collection, generally belonging to a ADO.Net Command object.</param>
        /// <param name="model">The type of the model.</param>
        /// <param name="ignoreParameters">A lists of parameter names that should not be created. Each entry must start with '@' to be a valid parameter.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        public static DbParameterCollection MapToOutParameters(DbParameterCollection prms, Type modelT, HashSet<string> ignoreParameters, ILogger logger)
        {
            if (!_cacheOutParamSet.TryGetValue(modelT, out var SqlParameterDelegates))
            {
                SqlLoggerExtensions.SqlSetOutParametersCacheMiss(logger, modelT);
                SqlParameterDelegates = BuildOutSetDelegate(modelT, logger);
                if (!_cacheOutParamSet.TryAdd(modelT, SqlParameterDelegates))
                {
                    SqlParameterDelegates = _cacheOutParamSet[modelT];
                    SqlLoggerExtensions.SqlSetOutParametersCacheMiss(logger, modelT);
                }
            }
            else
            {
                SqlLoggerExtensions.SqlSetOutParametersCacheHit(logger, modelT);
            }
            foreach (DbParameter prm in prms)
            {

                if (!ignoreParameters.Contains(prm.ParameterName))
                {
                    ignoreParameters.Add(prm.ParameterName);
                }
            }
            SqlParameterDelegates(prms, ignoreParameters, logger);
			return prms;
        }

        /// <summary>
        /// Creates a new object with property values based upon the provided output parameters which correspond to the MapTo attributes.
        /// </summary>
        /// <typeparam name="modelT">The type of the object. The "MapTo" attributes are used to read the Sql parameter collection values.</typeparam>
        /// <param name="prms">A parameter collection, generally belonging to a ADO.Net Command object after a database query.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>An object of the specified type, with properties set to parameter values.</returns>
        public static modelT ReadOutParameters<modelT>(TShard shardNumber, DbParameterCollection prms, ILogger logger) where modelT : class, new()
        {
            var T = typeof(modelT);
            if (!_getOutParamReadCache.TryGetValue(T, out var SqlRdrDelegate))
            {
                SqlLoggerExtensions.SqlReadOutParametersCacheMiss(logger, T);
                SqlRdrDelegate = BuildOutGetDelegate(prms, typeof(modelT), logger);
                if (!_getOutParamReadCache.TryAdd(typeof(modelT), SqlRdrDelegate))
                {
                    SqlRdrDelegate = _getOutParamReadCache[typeof(modelT)];
                }
            }
            else
            {
                SqlLoggerExtensions.SqlReadOutParametersCacheHit(logger, T);
            }
            return (modelT)SqlRdrDelegate(shardNumber, prms, logger);
        }

        /// <summary>
        /// Accepts a data reader object and returns a list of objects of the specified type, one for each record.
        /// </summary>
        /// <typeparam name="modelT">The type of the list result</typeparam>
        /// <param name="rdr">The data reader, set to the current result set.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>A list of objects of the specified type, one for each result.</returns>
        public static IList<modelT> FromDataReader<modelT>(TShard shardNumber, DbDataReader rdr, ILogger logger) where modelT : class, new()
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
                return new List<modelT>();
            }
            var T = typeof(modelT);
            if (!_getRdrParamCache.TryGetValue(typeof(modelT), out var SqlRdrDelegate))
            {
                SqlLoggerExtensions.SqlReaderCacheMiss(logger, T);
                SqlRdrDelegate = BuildReaderMapDelegate<modelT>(logger);
                if (!_getRdrParamCache.TryAdd(typeof(modelT), SqlRdrDelegate))
                {
                    SqlRdrDelegate = _getRdrParamCache[typeof(modelT)];
                }
            }
            else
            {
                SqlLoggerExtensions.SqlReaderCacheHit(logger, T);
            }
            return (List<modelT>)SqlRdrDelegate(shardNumber, rdr, logger);
        }

        #endregion
        #region delegate builders
        private static Action<DbParameterCollection, HashSet<string>, ILogger, object> BuildInMapDelegate(Type modelT, ILogger logger)
        {
            var expressions = new List<Expression>();
            //Create the two delegate parameters: in: sqlParametersCollection and model instance; out: sqlParametersCollection
            ParameterExpression prmSqlPrms = Expression.Variable(typeof(DbParameterCollection), "prms");
            ParameterExpression prmObjInstance = Expression.Parameter(typeof(object), "model");
            ParameterExpression expIgnoreParameters = Expression.Parameter(typeof(HashSet<string>), "ignoreParameters");
            ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
            var exprInPrms = new ParameterExpression[] { prmSqlPrms, expIgnoreParameters, expLogger, prmObjInstance };

            using (logger.BuildInParameterScope(modelT))
            {
                var prmTypedInstance = Expression.TypeAs(prmObjInstance, modelT);  //Our cached delegates accept neither generic nor dynamic arguments. We have to pass object then cast.
                var noDupPrmNameList = new HashSet<string>();
                //var miLogTrace = typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.TraceInMapperProperty));
                logger.SqlExpressionBlockStart(nameof(ShardMapper<TShard>.MapToInParameters), exprInPrms);
                expressions.Add(prmTypedInstance);
                logger.SqlExpressionLog(prmTypedInstance);
                IterateInMapProperties(modelT, expressions, prmSqlPrms, prmTypedInstance, expIgnoreParameters, expLogger, noDupPrmNameList, logger);
                logger.SqlExpressionBlockEnd(nameof(ShardMapper<TShard>.MapToInParameters));
            }
            var inBlock = Expression.Block(expressions);
            var lmbIn = Expression.Lambda<Action<DbParameterCollection, HashSet<string>, ILogger, object>>(inBlock, exprInPrms);
            return lmbIn.Compile();
        }
		private static bool PropertyTypeIsShardType(Type propertyType)
		{
			if (propertyType == typeof(Nullable<>))
			{
				propertyType = Nullable.GetUnderlyingType(propertyType);
			}
			if (propertyType == typeof(ShardKey<,>) || propertyType == typeof(ShardChild<,,>))
			{
				return true;
			}
			else
			{
				return false;
			}
		}
        private static void IterateInMapProperties(Type modelT, List<Expression> expressions, ParameterExpression prmSqlPrms, Expression prmTypedInstance, ParameterExpression expIgnoreParameters, ParameterExpression expLogger, HashSet<string> noDupPrmNameList, ILogger logger)
        {
            var miLogTrace = typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.TraceInMapperProperty));
            foreach (var prop in modelT.GetProperties())
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

        private static Action<DbParameterCollection, HashSet<string>, ILogger> BuildOutSetDelegate(Type modelT, ILogger logger)
        {
            var expressions = new List<Expression>();

            //Create the two delegate parameters: in: sqlParametersCollection and model instance; out: sqlParametersCollection
            ParameterExpression prmSqlPrms = Expression.Variable(typeof(DbParameterCollection), "prms");
            ParameterExpression expIgnoreParameters = Expression.Parameter(typeof(HashSet<string>), "ignoreParameters");
            ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
            var noDupPrmNameList = new HashSet<string>();

            var exprOutPrms = new ParameterExpression[] { prmSqlPrms, expIgnoreParameters, expLogger };

            IterateSetOutParameters(modelT, expressions, prmSqlPrms, expIgnoreParameters, expLogger, noDupPrmNameList, logger);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                using (logger.BuildSetOutParameterScope(modelT))
                {
                    logger.SqlExpressionBlockStart(nameof(ShardMapper<TShard>.MapToOutParameters), exprOutPrms);
                    foreach (var exp in expressions)
                    {
                        logger.SqlExpressionLog(exp);
                    }
                    logger.SqlExpressionBlockEnd(nameof(ShardMapper<TShard>.MapToOutParameters));
                }
            }
            var outBlock = Expression.Block(expressions);
            var lmbOut = Expression.Lambda<Action<DbParameterCollection, HashSet<string>, ILogger>>(outBlock, exprOutPrms);
            return lmbOut.Compile();
        }
        private static void IterateSetOutParameters(Type modelT, List<Expression> expressions, ParameterExpression prmSqlPrms, ParameterExpression expIgnoreParameters, ParameterExpression expLogger, HashSet<string> noDupPrmNameList, ILogger logger)
        {
            var miLogTrace = typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.TraceSetOutMapperProperty));
            //Loop through all object properties:
            foreach (var prop in modelT.GetProperties())
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


        private static Func<TShard, DbParameterCollection, ILogger, object> BuildOutGetDelegate(DbParameterCollection prms, Type modelT, ILogger logger)
        {
            ParameterExpression prmShardNumber = Expression.Variable(typeof(TShard), "shardNumber");
            ParameterExpression expPrms = Expression.Parameter(typeof(DbParameterCollection), "prms");
            ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var variableExpressions = new List<ParameterExpression>();
            var expModel = Expression.Variable(modelT, "model"); //list result variable
			variableExpressions.Add(expModel);
			var expPrm = Expression.Variable(typeof(DbParameter), "prm");
			variableExpressions.Add(expPrm);
			var expressionPrms = new ParameterExpression[] { prmShardNumber, expPrms, expLogger };


            var expressions = new List<Expression>()
            {
                Expression.Assign(expModel, Expression.New(modelT)) // var result = new <T>;
            };
            using (logger.BuildGetOutParameterScope(modelT))
            {
                logger.SqlExpressionBlockStart(nameof(ShardMapper<TShard>.ReadOutParameters), expressionPrms);
                //Loop through all object properties:

                IterateGetOutParameters(modelT, prmShardNumber, expPrms, expPrm, variableExpressions, expressions, expModel, expLogger, logger);

                expressions.Add(expModel); //return value;
                logger.SqlExpressionLog(expModel);
                logger.SqlExpressionBlockEnd(nameof(ShardMapper<TShard>.ReadOutParameters));
            }
            var expBlock = Expression.Block(variableExpressions, expressions);
            var lambda = Expression.Lambda<Func<TShard, DbParameterCollection, ILogger, object>>(expBlock, expressionPrms);
            return lambda.Compile();
        }
        private static void IterateGetOutParameters(Type modelT, ParameterExpression prmShardNumber, ParameterExpression expPrms, ParameterExpression expPrm, List<ParameterExpression> variableExpressions, List<Expression> expressions, Expression expModel, ParameterExpression expLogger, ILogger logger)
        {
            var miLogTrace = typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.TraceGetOutMapperProperty));

            foreach (var prop in modelT.GetProperties())
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
						var expCallLog = Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name));
						expressions.Add(expCallLog);
						logger.SqlExpressionLog(expCallLog);

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

        private static Func<TShard, DbDataReader, ILogger, List<T>> BuildReaderMapDelegate<T>(ILogger logger)
        {
            var modelT = typeof(T);

            var expressions = new List<Expression>();
            var columnLookupExpressions = new List<MethodCallExpression>();

            ParameterExpression prmShardNumber = Expression.Variable(typeof(TShard), "shardNumber");
            ParameterExpression prmSqlRdr = Expression.Parameter(typeof(DbDataReader), "rdr"); //input param
            ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
            var expressionPrms = new ParameterExpression[] { prmShardNumber, prmSqlRdr, expLogger };

            //MethodInfos for subsequent Expression calls
            var miGetFieldOrdinal = typeof(ShardMapper<TShard>).GetMethod(nameof(ShardMapper<TShard>.GetFieldOrdinal), BindingFlags.NonPublic | BindingFlags.Static);
            var miRead = typeof(DbDataReader).GetMethod(nameof(DbDataReader.Read));
            var miGetFieldValue = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue));
            var miListAdd = typeof(List<T>).GetMethod(nameof(List<T>.Add));

            //create List<modelT> result
            var expModel = Expression.Parameter(typeof(T), "model"); //model variable
            var expListResult = Expression.Parameter(typeof(List<T>), "result"); //list result variable
            var expOrdinal = Expression.Variable(typeof(int), "ordinal");
            var expOrdinals = Expression.Variable(typeof(int[]), "ordinals");

            var propIndex = 0;
            var resultExpressions = new List<Expression>();
            logger.SqlExpressionNote("while (rdr.Read())");
            using (logger.BuildRdrScope(modelT))
            {
                logger.SqlExpressionBlockStart(nameof(ShardMapper<TShard>.FromDataReader), expressionPrms);
                var expAssign = Expression.Assign(expModel, Expression.New(typeof(T)));
                expressions.Add(expAssign); //var model = new T
                logger.SqlExpressionLog(expAssign);

                //Loop through all object properties:
                IterateRdrColumns(modelT, prmShardNumber, expModel, columnLookupExpressions, expressions, prmSqlRdr, expOrdinals, expOrdinal, ref propIndex, expLogger, logger);

                var expAddList = Expression.Call(expListResult, miListAdd, expModel);
                expressions.Add(expAddList); //ResultList.Add(model);
                logger.SqlExpressionLog(expAddList);

                logger.SqlExpressionNote($"Out-of-order expressions which should appear at the beginning of {nameof(ShardMapper<TShard>.FromDataReader)}:");
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
            var lambda = Expression.Lambda<Func<TShard, DbDataReader, ILogger, List<T>>>(expBlock, expressionPrms);
            return lambda.Compile();
        }
        private static void IterateRdrColumns(Type modelT, ParameterExpression prmShardNumber, Expression expModel, List<MethodCallExpression> columnLookupExpressions, List<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, ParameterExpression expLogger, ILogger logger)
        {
            var miLogTrace = typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.TraceRdrMapperProperty));
            foreach (var prop in modelT.GetProperties())
            {
                if (prop.IsDefined(typeof(ParameterMapAttribute), true))
                {
                    var attrPMs = prop.GetCustomAttributes<ParameterMapAttribute>(true);
					//TODO: if ShardKey/ShardChild create memory variables and add to expression
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
        #region Helpers
        //Return -1 if not found (rather than error, as rdr.GetOrdinal does), otherwise identical code.
        internal static int GetFieldOrdinal(DbDataReader rdr, string fieldName)
        {
            if (fieldName.StartsWith("@"))
            {
                fieldName = fieldName.Substring(1);
            }
            for (int i = 0; i < rdr.FieldCount; i++)
            {
                if (fieldName == rdr.GetName(i))
                {
                    return i;
                }
            }
            var comparer = System.Globalization.CultureInfo.InvariantCulture.CompareInfo;
            CompareOptions co = CompareOptions.IgnoreCase;
            for (int i = 0; i < rdr.FieldCount; i++)
            {
                if (comparer.Compare(fieldName, rdr.GetName(i), co) == 0)
                {
                    return i;
                }
            }
            co = CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth;
            for (int i = 0; i < rdr.FieldCount; i++)
            {
                if (comparer.Compare(fieldName, rdr.GetName(i), co) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        #endregion
        #region Convert Sql result to object(s)
        public static TResult SqlResultsToObject<TReaderResult, TOutParameters, TResult>
            (
            DataOrigin dataOrigin,
            TShard shardNumber,
            string proceedureName,
            DbDataReader rdr,
            DbParameterCollection prms,
            ILogger logger)
            where TReaderResult : class, new()
            where TOutParameters : class, new()
            where TResult : class, new()
            => SqlResultsToObject<TReaderResult, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, DummyType, TOutParameters, TResult>(dataOrigin, shardNumber, proceedureName, rdr, prms, logger);
        

        public static TResult SqlResultsToObject<TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutParameters, TResult>
            (
            DataOrigin dataOrigin,
            TShard shardNumber,
            string procedureName,
            DbDataReader rdr,
            DbParameterCollection prms,
            ILogger logger)
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

            var dummy = typeof(DummyType);
            var hasNextResult = true;
            if (typeof(TReaderResult0) != dummy)
            {
                resultList0 = ShardMapper<TShard>.FromDataReader<TReaderResult0>(shardNumber, rdr, logger);
                hasNextResult = rdr.NextResult();
            }
            if (hasNextResult && typeof(TReaderResult1) != dummy)
            {
                resultList1 = ShardMapper<TShard>.FromDataReader<TReaderResult1>(shardNumber, rdr, logger);
                hasNextResult = rdr.NextResult();
            }
            if (hasNextResult && typeof(TReaderResult2) != dummy)
            {
                resultList2 = ShardMapper<TShard>.FromDataReader<TReaderResult2>(shardNumber, rdr, logger);
                hasNextResult = rdr.NextResult();
            }
            if (hasNextResult && typeof(TReaderResult3) != dummy)
            {
                resultList3 = ShardMapper<TShard>.FromDataReader<TReaderResult3>(shardNumber, rdr, logger);
                hasNextResult = rdr.NextResult();
            }
            if (hasNextResult && typeof(TReaderResult4) != dummy)
            {
                resultList4 = ShardMapper<TShard>.FromDataReader<TReaderResult4>(shardNumber, rdr, logger);
                hasNextResult = rdr.NextResult();
            }
            if (hasNextResult && typeof(TReaderResult5) != dummy)
            {
                resultList5 = ShardMapper<TShard>.FromDataReader<TReaderResult5>(shardNumber, rdr, logger);
                hasNextResult = rdr.NextResult();
            }
            if (hasNextResult && typeof(TReaderResult6) != dummy)
            {
                resultList6 = ShardMapper<TShard>.FromDataReader<TReaderResult6>(shardNumber, rdr, logger);
                hasNextResult = rdr.NextResult();
            }
            if (hasNextResult && typeof(TReaderResult7) != dummy)
            {
                resultList7 = ShardMapper<TShard>.FromDataReader<TReaderResult7>(shardNumber, rdr, logger);
                hasNextResult = rdr.NextResult();
            }
            if (typeof(TOutParameters) != dummy)
            {
                resultOutPrms = ShardMapper<TShard>.ReadOutParameters<TOutParameters>(shardNumber, prms, logger);
            }


            var queryKey = typeof(TResult).ToString() + procedureName;
            if (!_getObjectCache.TryGetValue(queryKey, out var sqlObjectDelegate))
            {
                sqlObjectDelegate = BuildExpressionSqlResultsToObject<TResult, 
                    TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, 
                    TReaderResult7, TOutParameters> (procedureName, logger);
                if (!_getObjectCache.TryAdd(queryKey, sqlObjectDelegate))
                {
                    sqlObjectDelegate = _getObjectCache[queryKey];
                }
            }

            return (TResult)sqlObjectDelegate(shardNumber, procedureName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultList6, resultList7, resultOutPrms, logger);
        }
        private static TResult AssignRootToResult<TEval, TResult>(TShard shardNumber, string procedureName, IList<TEval> resultList, ILogger logger) where TResult : class, new() where TEval : class, new()
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
        private static Func<TShard, string, object, object, object, object, object, object, object, object, object, ILogger, object> BuildExpressionSqlResultsToObject<
            TResult, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutParameters>
            (string procedureName, ILogger logger)
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
            var expPrmOut = Expression.Parameter(typeof(TOutParameters), "prms");
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
            var miSetResultSetGeneric = typeof(ShardMapper<TShard>).GetMethod(nameof(ShardMapper<TShard>.AssignRootToResult), BindingFlags.Static);
            var miFormat = typeof(string).GetMethod(nameof(string.Concat), BindingFlags.Static);
            var resultType = typeof(TResult);

            using (logger.BuildSqlResultsToObjectScope(procedureName, resultType))
            {
                //Set base type to some result value, if we can.
                if (resultType == typeof(TOutParameters))
                {
                    var expAssign = Expression.Assign(expResult, expPrmOut);
                    expressions.Add(expAssign);
                    logger.SqlExpressionLog(expAssign);
                    isPrmOutUsed = true;
                }
                else if (resultType == typeof(TReaderResult0))
                {
                    //result = AssignRootToResult<TReaderResult0, TResult>(shardNumber, procedureName, resultList0, logger)
                    var expAssign = Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult0), resultType }), expShardNumber, expProcName, expResultSet0, expLogger));
                    expressions.Add(expAssign);
                    logger.SqlExpressionLog(expAssign);

                    var expIf = Expression.IfThen(
                        Expression.Equal(expResult, expNull), //if (result == null)
                        Expression.Return(expExitTarget, expNull, typeof(TResult)) //return null;
                        );

                    expressions.Add(expIf);
                    logger.SqlExpressionLog(expIf);
                    isRdrResult0Used = true;
                }
                else if (result is TReaderResult1)
                {
                    var expAssign = Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult1), resultType }), expShardNumber, expProcName, expResultSet1, expLogger));
                    expressions.Add(expAssign);
                    logger.SqlExpressionLog(expAssign);
                    var expIf = Expression.IfThen(Expression.Equal(expResult, expNull), Expression.Return(expExitTarget, expNull, typeof(TResult)));
                    expressions.Add(expIf);
                    logger.SqlExpressionLog(expIf);
                    isRdrResult1Used = true;
                }
                else if (result is TReaderResult2)
                {
                    var expAssign = Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult2), resultType }), expShardNumber, expProcName, expResultSet2, expLogger));
                    expressions.Add(expAssign);
                    logger.SqlExpressionLog(expAssign);
                    var expIf = Expression.IfThen(Expression.Equal(expResult, expNull), Expression.Return(expExitTarget, expNull, typeof(TResult)));
                    expressions.Add(expIf);
                    logger.SqlExpressionLog(expIf);
                    isRdrResult2Used = true;
                }
                else if (result is TReaderResult3)
                {
                    var expAssign = Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult3), resultType }), expShardNumber, expProcName, expResultSet3, expLogger));
                    expressions.Add(expAssign);
                    logger.SqlExpressionLog(expAssign);
                    var expIf = Expression.IfThen(Expression.Equal(expResult, expNull), Expression.Return(expExitTarget, expNull, typeof(TResult)));
                    expressions.Add(expIf);
                    logger.SqlExpressionLog(expIf);
                    isRdrResult3Used = true;
                }
                else if (result is TReaderResult4)
                {
                    var expAssign = Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult4), resultType }), expShardNumber, expProcName, expResultSet4, expLogger));
                    expressions.Add(expAssign);
                    logger.SqlExpressionLog(expAssign);
                    var expIf = Expression.IfThen(Expression.Equal(expResult, expNull), Expression.Return(expExitTarget, expNull, typeof(TResult)));
                    expressions.Add(expIf);
                    logger.SqlExpressionLog(expIf);
                    isRdrResult4Used = true;
                }
                else if (result is TReaderResult5)
                {
                    var expAssign = Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult5), resultType }), expShardNumber, expProcName, expResultSet5, expLogger));
                    expressions.Add(expAssign);
                    logger.SqlExpressionLog(expAssign);
                    var expIf = Expression.IfThen(Expression.Equal(expResult, expNull), Expression.Return(expExitTarget, expNull, typeof(TResult)));
                    expressions.Add(expIf);
                    logger.SqlExpressionLog(expIf);
                    isRdrResult5Used = true;
                }
                else if (result is TReaderResult6)
                {
                    var expAssign = Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult6), resultType }), expShardNumber, expProcName, expResultSet6, expLogger));
                    expressions.Add(expAssign);
                    logger.SqlExpressionLog(expAssign);
                    var expIf = Expression.IfThen(Expression.Equal(expResult, expNull), Expression.Return(expExitTarget, expNull, typeof(TResult)));
                    expressions.Add(expIf);
                    logger.SqlExpressionLog(expIf);
                    isRdrResult6Used = true;
                }
                else if (result is TReaderResult7)
                {
                    var expAssign = Expression.Assign(expResult, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult7), resultType }), expShardNumber, expProcName, expResultSet7, expLogger));
                    expressions.Add(expAssign);
                    logger.SqlExpressionLog(expAssign);
                    var expIf = Expression.IfThen(Expression.Equal(expResult, expNull), Expression.Return(expExitTarget, expNull, typeof(TResult)));
                    expressions.Add(expIf);
                    logger.SqlExpressionLog(expIf);
                    isRdrResult7Used = true;
                }
                else
                {
                    //match not found, so just create a new instance and we'll try again on properties
                    var expAssign = Expression.Assign(expResult, Expression.New(resultType));
                    expressions.Add(expAssign);
                    logger.SqlExpressionLog(expAssign);
                }

                var props = resultType.GetProperties();

                //Iterate over any List<> properties and set any List<resultSet> that match.
                foreach (var prop in props)
                {
                    if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult0>)) && !isRdrResult0Used)
                    {
                        var expAssign = Expression.Assign(Expression.Property(expResult, prop), expResultSet0);
                        expressions.Add(expAssign);
                        logger.SqlExpressionLog(expAssign);
                        isRdrResult0Used = true;
                    }
                    else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult1>)) && !isRdrResult1Used)
                    {
                        var expAssign = Expression.Assign(Expression.Property(expResult, prop), expResultSet1);
                        expressions.Add(expAssign);
                        logger.SqlExpressionLog(expAssign);
                        isRdrResult1Used = true;
                    }
                    else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult2>)) && !isRdrResult2Used)
                    {
                        var expAssign = Expression.Assign(Expression.Property(expResult, prop), expResultSet2);
                        expressions.Add(expAssign);
                        logger.SqlExpressionLog(expAssign);
                        isRdrResult2Used = true;
                    }
                    else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult3>)) && !isRdrResult3Used)
                    {
                        var expAssign = Expression.Assign(Expression.Property(expResult, prop), expResultSet3);
                        expressions.Add(expAssign);
                        logger.SqlExpressionLog(expAssign);
                        isRdrResult3Used = true;
                    }
                    else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult4>)) && !isRdrResult4Used)
                    {
                        var expAssign = Expression.Assign(Expression.Property(expResult, prop), expResultSet4);
                        expressions.Add(expAssign);
                        logger.SqlExpressionLog(expAssign);
                        isRdrResult4Used = true;
                    }
                    else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult5>)) && !isRdrResult5Used)
                    {
                        var expAssign = Expression.Assign(Expression.Property(expResult, prop), expResultSet5);
                        expressions.Add(expAssign);
                        logger.SqlExpressionLog(expAssign);
                        isRdrResult5Used = true;
                    }
                    else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult6>)) && !isRdrResult6Used)
                    {
                        var expAssign = Expression.Assign(Expression.Property(expResult, prop), expResultSet6);
                        expressions.Add(expAssign);
                        logger.SqlExpressionLog(expAssign);
                        isRdrResult6Used = true;
                    }
                    else if (prop.PropertyType.IsAssignableFrom(typeof(IList<TReaderResult7>)) && !isRdrResult7Used)
                    {
                        var expAssign = Expression.Assign(Expression.Property(expResult, prop), expResultSet7);
                        expressions.Add(expAssign);
                        logger.SqlExpressionLog(expAssign);
                        isRdrResult7Used = true;
                    }
                }
                //Iterate over any object (non-list) properties and set any resultSet that match.
                foreach (var prop in props)
                {
                    if (prop.PropertyType.IsAssignableFrom(typeof(TOutParameters)) && !isPrmOutUsed)
                    {
                        var expAssign = Expression.Assign(Expression.Property(expResult, prop), expPrmOut);
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
                            Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet0, "Item", Expression.Constant(0))),
                            Expression.IfThen(                                                  //else if (count > 1)
                                Expression.GreaterThan(expCount, expOne),                       //{       logger.LogError("");
                                Expression.Call(miLogError, Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount)))
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
                        var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet0, "Item", Expression.Constant(0))),
                            Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
                                Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount))));
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
                        var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet1, "Item", Expression.Constant(0))),
                            Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
                                Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount))));
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
                        var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet2, "Item", Expression.Constant(0))),
                            Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
                                Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount))));
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
                        var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet3, "Item", Expression.Constant(0))),
                            Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
                                Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount))));
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
                        var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet4, "Item", Expression.Constant(0))),
                            Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
                                Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount))));
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
                        var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet5, "Item", Expression.Constant(0))),
                            Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
                                Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount))));
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
                        var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet6, "Item", Expression.Constant(0))),
                            Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
                                Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount))));
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
                        var expErrIfMultiple = Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expResult, prop), Expression.Property(expResultSet7, "Item", Expression.Constant(0))),
                            Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
                                Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on shard {{0}} unexpectedly returned {{1}} results instead of one."), expShardNumber, expCount))));
                        expressions.Add(expErrIfMultiple);
                        logger.SqlExpressionLog(expErrIfMultiple);
                        isRdrResult7Used = true;
                    }

                }
                var expExit = Expression.Label(expExitTarget);
                expressions.Add(expExit); //Exit procedure
                logger.SqlExpressionLog(expExit);
            }
            var expBlock = Expression.Block(resultType, new ParameterExpression[] { expCount, expResult }, expressions); //+variables
            var lambda = Expression.Lambda<Func<TShard, string, object, object, object, object, object, object, object, object, object, ILogger, object>>
                (expBlock, new ParameterExpression[] { expShardNumber, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expResultSet6, expResultSet7, expPrmOut, expLogger }); //+parameters
            return lambda.Compile();
        }
        #endregion
    }
}
