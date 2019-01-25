﻿// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

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
using System.Threading;

namespace ArgentSea
{
    /// <summary>
    /// This static class contains the logic for mapping database parameters to/from properties.
    /// </summary>
	public static class Mapper
	{
        private static ConcurrentDictionary<Type, Lazy<Delegate>> _cacheInParamSet = new ConcurrentDictionary<Type, Lazy<Delegate>>();
        private static ConcurrentDictionary<Type, Lazy<Action<DbParameterCollection, HashSet<string>, ILogger>>> _cacheOutParamSet = new ConcurrentDictionary<Type, Lazy<Action<DbParameterCollection, HashSet<string>, ILogger>>>();
        private static ConcurrentDictionary<Type, Lazy<(Delegate RowData, Delegate Ordinals)>> _getRdrMapCache = new ConcurrentDictionary<Type, Lazy<(Delegate, Delegate)>>();
        private static ConcurrentDictionary<Type, Lazy<Delegate>> _getOutParamReadCache = new ConcurrentDictionary<Type, Lazy<Delegate>>();
        private static ConcurrentDictionary<string, Lazy<Delegate>> _getOutObjectCache = new ConcurrentDictionary<string, Lazy<Delegate>>();
        private static ConcurrentDictionary<string, Lazy<Delegate>> _getRstObjectCache = new ConcurrentDictionary<string, Lazy<Delegate>>();

        #region Public methods


        #region Map Input Parameters

        /// <summary>
        /// Accepts a Sql Parameter collection and appends Sql input parameters whose values correspond to the provided object properties and MapTo attributes.
        /// </summary>
        /// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql metadata and columns.</typeparam>
        /// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object.</param>
        /// <param name="model">An object model instance. The property values are use as parameter values.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static DbParameterCollection CreateInputParameters<TModel>(this DbParameterCollection parameters, TModel model, ILogger logger)
            where TModel : class, new()
            => CreateInputParameters<TModel>(parameters, model, null, logger);

        /// <summary>
        /// Accepts a Sql Parameter collection and appends Sql input parameters whose values correspond to the provided object properties and MapTo attributes.
        /// </summary>
        /// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql metadata and columns.</typeparam>
        /// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object.</param>
        /// <param name="model">An object model instance. The property values are use as parameter values.</param>
        /// <param name="ignoreParameters">A lists of parameter names that should not be created. Each entry must exactly match the parameter name, including prefix and casing.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static DbParameterCollection CreateInputParameters<TModel>(this DbParameterCollection parameters, TModel model, HashSet<string> ignoreParameters, ILogger logger)
            where TModel : class, new()
        {
            if (ignoreParameters is null)
			{
				ignoreParameters = new HashSet<string>();
			}
			var tModel = typeof(TModel);
            var lazySqlParameterDelegate = _cacheInParamSet.GetOrAdd(tModel, new Lazy<Delegate>(() => BuildInMapDelegate<TModel>(tModel, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            if (lazySqlParameterDelegate.IsValueCreated)
            {
                LoggingExtensions.SqlInParametersCacheHit(logger, tModel);
            }
            else
            {
                LoggingExtensions.SqlInParametersCacheMiss(logger, tModel);
            }

            foreach (DbParameter prm in parameters)
			{
				if (!ignoreParameters.Contains(prm.ParameterName))
				{
					ignoreParameters.Add(prm.ParameterName);
				}
			}
            ((Action<DbParameterCollection, HashSet<string>, ILogger, TModel>)lazySqlParameterDelegate.Value)(parameters, ignoreParameters, logger, model);
			return parameters;
		}
        #endregion

        #region Set Mapped Output Parameters
        /// <summary>
        /// Accepts a Sql Parameter collection and appends Sql output parameters corresponding to the MapTo attributes.
        /// </summary>
        /// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql parameter types.</typeparam>
        /// <param name="parameters">A parameter collection, possibly belonging to a ADO.Net Command object or a QueryParmaters object.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>The DbParameterCollection, enabling a fluent API.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static DbParameterCollection CreateOutputParameters<TModel>(this DbParameterCollection parameters, ILogger logger) 
            where TModel : class, new()
            => CreateOutputParameters(parameters, typeof(TModel), null, logger);

        /// <summary>
        /// Accepts a Sql Parameter collection and appends Sql output parameters corresponding to the MapTo attributes.
        /// </summary>
        /// <param name="parameters">A parameter collection, possibly belonging to a ADO.Net Command object or a QueryParmaters object.</param>
        /// <param name="tModel">The type of the object. The "MapTo" attributes are used to create the Sql parameter types.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>The DbParameterCollection, enabling a fluent API.</returns>
        public static DbParameterCollection CreateOutputParameters(this DbParameterCollection parameters, Type tModel, ILogger logger)
            => CreateOutputParameters(parameters, tModel, null, logger);


        /// <summary>
        /// Accepts a Sql Parameter collection and appends Sql output parameters corresponding to the MapTo attributes.
        /// </summary>
        /// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql parameter types.</typeparam>
        /// <param name="parameters">A parameter collection, possibly belonging to a ADO.Net Command object or a QueryParmaters object.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>The DbParameterCollection, enabling a fluent API.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static DbParameterCollection CreateOutputParameters<TModel>(this DbParameterCollection parameters, HashSet<string> ignoreParameters, ILogger logger)
            => CreateOutputParameters(parameters, typeof(TModel), ignoreParameters, logger);

        /// <summary>
        /// Accepts a Sql Parameter collection and appends Sql output parameters corresponding to the MapTo attributes.
        /// </summary>
        /// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to create the Sql parameter types.</typeparam>
        /// <param name="parameters">A parameter collection, possibly belonging to a ADO.Net Command object or a QueryParmaters object.</param>
        /// <param name="ignoreParameters">A lists of parameter names that should not be created.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>The DbParameterCollection, enabling a fluent API.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static DbParameterCollection CreateOutputParameters(this DbParameterCollection parameters, Type tModel, HashSet<string> ignoreParameters, ILogger logger)
		{
			//For each parameter, Expression Tree does the following:
			//ArgentSea.LoggingExtensions.TraceSetOutMapperProperty(logger, "ParameterName");
			//if (ArgentSea.ExpressionHelpers.DontIgnoreThisParameter("@ParameterName", ignoreParameters))
			//{
			//	ArgentSea.Sql.SqlParameterCollectionExtensions.AddSql---OutParameter(parameters, "@ParameterName");
			//}

			if (ignoreParameters is null)
			{
				ignoreParameters = new HashSet<string>();
			}

            var lazySqlParameterDelegate = _cacheOutParamSet.GetOrAdd(tModel, new Lazy<Action<DbParameterCollection, HashSet<string>, ILogger>>(() => BuildOutSetDelegate(tModel, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            if (lazySqlParameterDelegate.IsValueCreated)
            {
                LoggingExtensions.SqlSetOutParametersCacheHit(logger, tModel);
            }
            else
            {
                LoggingExtensions.SqlSetOutParametersCacheMiss(logger, tModel);
            }

			foreach (DbParameter prm in parameters)
			{

				if (!ignoreParameters.Contains(prm.ParameterName))
				{
					ignoreParameters.Add(prm.ParameterName);
				}
			}
            lazySqlParameterDelegate.Value(parameters, ignoreParameters, logger);
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
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ToModel<TModel>(this DbParameterCollection parameters, ILogger logger) 
            where TModel : class, new()
			=> ToModel<BadShardType, TModel>(parameters, null, logger);

        /// <summary>
        /// Creates a new object with property values based upon the provided output parameters which correspond to the MapTo attributes.
        /// </summary>
        /// <typeparam name="TShard">The type of the Shard Id.</typeparam>
        /// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to read the Sql parameter collection values.</typeparam>
        /// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object after a database query.</param>
        /// <param name="shardId">The identifier for the current shard. Required for ShardKey and ShardChild types.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>An object of the specified type, with properties set to parameter values.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ToModel<TShard, TModel>(this DbParameterCollection parameters, TShard shardId, ILogger logger) 
            where TModel : class, new() 
            where TShard : IComparable
		{
			var tModel = typeof(TModel);

            var lazySqlOutDelegate = _getOutParamReadCache.GetOrAdd(tModel, new Lazy<Delegate>(() => BuildOutGetDelegate<TShard>(parameters, tModel, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            if (lazySqlOutDelegate.IsValueCreated)
            {
                LoggingExtensions.SqlReadOutParametersCacheHit(logger, tModel);
            }
            else
            {
                LoggingExtensions.SqlReadOutParametersCacheMiss(logger, tModel);
            }
			return (TModel)((Func<TShard, DbParameterCollection, ILogger, object>)lazySqlOutDelegate.Value)(shardId, parameters, logger);
		}

        /// <summary>
        /// Accepts a single-row data reader object and returns a an object instance of the specified type using Mapping attributes.
        /// </summary>
        /// <typeparam name="TModel">The type of the result.</typeparam>
        /// <param name="rdr">The data reader, set to the current result set.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>An object of the specified type.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ToModel<TModel>(this DbDataReader rdr, ILogger logger)
            where TModel : class, new()
            => ToModel<BadShardType, TModel>(rdr, null, logger);

        /// <summary>
        /// Accepts a single-row data reader object and returns a an object instance of the specified type using Mapping attributes.
        /// </summary>
        /// <typeparam name="TShard">The type of the Shard Id.</typeparam>
        /// <typeparam name="TModel">The type of the result.</typeparam>
        /// <param name="shardId">The identifier for the current shard. Required for ShardKey and ShardChild types.</param>
        /// <param name="rdr">The data reader, set to the current result set.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>An object of the specified type.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ToModel<TShard, TModel>(this DbDataReader rdr, TShard shardId, ILogger logger)
            where TModel : class, new()
            where TShard : IComparable
        {
            TModel result = null;
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
                return result;
            }
            var tModel = typeof(TModel);

            var lazySqlRdrDelegate = _getRdrMapCache.GetOrAdd(tModel, new Lazy<(Delegate RowData, Delegate Ordinals)>(() => BuildReaderMapDelegate<TShard, TModel>(logger), LazyThreadSafetyMode.ExecutionAndPublication));
            if (lazySqlRdrDelegate.IsValueCreated)
            {
                LoggingExtensions.SqlReaderCacheHit(logger, tModel);
            }
            else
            {
                LoggingExtensions.SqlReadOutParametersCacheMiss(logger, tModel);
            }

            int[] ordinals = ((Func<DbDataReader, int[]>)lazySqlRdrDelegate.Value.Ordinals)(rdr);
            if (rdr.Read())
            {
                result = ((Func<TShard, DbDataReader, int[], ILogger, TModel>)lazySqlRdrDelegate.Value.RowData)(shardId, rdr, ordinals, logger);
            }
            if (rdr.Read())
            {
                throw new UnexpectedMultiRowResultException();
            }
            return result;
        }


        /// <summary>
        /// Accepts a data reader object and returns a list of objects of the specified type, one for each record.
        /// </summary>
        /// <typeparam name="TModel">The type of the list result</typeparam>
        /// <param name="rdr">The data reader, set to the current result set.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>A list of objects of the specified type, one for each result.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static IList<TModel> ToList<TModel>(this DbDataReader rdr, ILogger logger) 
            where TModel : class, new()
			=> ToList<BadShardType, TModel>(rdr, null, logger);

        /// <summary>
        /// Accepts a data reader object and returns a list of objects of the specified type, one for each record.
        /// </summary>
        /// <typeparam name="TShard">The type of the Shard Id.</typeparam>
        /// <typeparam name="TModel">The type of the list result.</typeparam>
        /// <param name="shardId">The identifier for the current shard. Required for ShardKey and ShardChild types.</param>
        /// <param name="rdr">The data reader, set to the current result set.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>A list of objects of the specified type, one for each result.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static IList<TModel> ToList<TShard, TModel>(this DbDataReader rdr, TShard shardId, ILogger logger)
            where TModel : class, new() 
            where TShard : IComparable
		{
            var result = new List<TModel>();
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
				return result;
			}
			var tModel = typeof(TModel);
            var lazySqlRdrDelegate = _getRdrMapCache.GetOrAdd(tModel, new Lazy<(Delegate RowData, Delegate Ordinals)>(() => BuildReaderMapDelegate<TShard, TModel>(logger), LazyThreadSafetyMode.ExecutionAndPublication));
            if (lazySqlRdrDelegate.IsValueCreated)
            {
                LoggingExtensions.SqlReaderCacheHit(logger, tModel);
            }
            else
            {
                LoggingExtensions.SqlReadOutParametersCacheMiss(logger, tModel);
            }

            int[] ordinals = ((Func<DbDataReader, int[]>)lazySqlRdrDelegate.Value.Ordinals)(rdr);
            while (rdr.Read())
            {
                var item = ((Func<TShard, DbDataReader, int[], ILogger, TModel>)lazySqlRdrDelegate.Value.RowData)(shardId, rdr, ordinals, logger);
                if (!(item is null))
                {
                    result.Add(item);
                }
            }
			return result;
		}
		#endregion

		#endregion
		#region delegate builders
		private static Action<DbParameterCollection, HashSet<string>, ILogger, TModel> BuildInMapDelegate<TModel>(Type tModel, ILogger logger)
            where TModel : class, new()
        {
			var expressions = new List<Expression>();
			//Create the two delegate parameters: in: sqlParametersCollection and model instance; out: sqlParametersCollection
			ParameterExpression prmSqlPrms = Expression.Variable(typeof(DbParameterCollection), "parameters");
			ParameterExpression prmObjInstance = Expression.Parameter(tModel, "model");
			ParameterExpression expIgnoreParameters = Expression.Parameter(typeof(HashSet<string>), "ignoreParameters");
			ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
            var variables = new List<ParameterExpression>();
            var exprInPrms = new ParameterExpression[] { prmSqlPrms, expIgnoreParameters, expLogger, prmObjInstance };
			//var prmTypedInstance = Expression.TypeAs(prmObjInstance, tModel);  //Our cached delegates accept neither generic nor dynamic arguments. We have to pass object then cast.
			var noDupPrmNameList = new HashSet<string>();
			//expressions.Add(prmTypedInstance);
			IterateInMapProperties(tModel, expressions, variables, prmSqlPrms, prmObjInstance, expIgnoreParameters, expLogger, noDupPrmNameList, logger);
			var inBlock = Expression.Block(variables, expressions);
			var lmbIn = Expression.Lambda<Action<DbParameterCollection, HashSet<string>, ILogger, TModel>>(inBlock, exprInPrms);
			logger?.CreatedExpressionTreeForSetInParameters(tModel, inBlock);
			return lmbIn.Compile();
		}
		private static void IterateInMapProperties(Type tModel, List<Expression> expressions, List<ParameterExpression> variables, ParameterExpression prmSqlPrms, Expression expModel, ParameterExpression expIgnoreParameters, ParameterExpression expLogger, HashSet<string> noDupPrmNameList, ILogger logger)
		{
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceInMapperProperty));
			foreach (var prop in tModel.GetProperties())
			{
				MemberExpression expProperty = Expression.Property(expModel, prop);
                MemberExpression expOriginalProperty = expProperty;
                var isShardKey = prop.IsDefined(typeof(MapShardKeyAttribute), true);
                var isShardChild = prop.IsDefined(typeof(MapShardChildAttribute), true);
                if ((isShardKey || isShardChild) && prop.IsDefined(typeof(ParameterMapAttributeBase), true))
                {
                    Type propType = prop.PropertyType;
                    var foundShardId = false;
                    var foundRecordId = false;
                    var foundChildId = false;
                    string shardIdPrm;
                    string recordIdPrm;
                    string childIdPrm;
                    expressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name)));

                    Expression expDetectNullOrEmpty;
                    if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        expProperty = Expression.Property(expProperty, propType.GetProperty(nameof(Nullable<int>.Value)));
                        propType = Nullable.GetUnderlyingType(propType);
                        expDetectNullOrEmpty = Expression.Property(expOriginalProperty, prop.PropertyType.GetProperty(nameof(Nullable<int>.HasValue)));
                    }
                    else
                    {
                        expDetectNullOrEmpty = Expression.NotEqual(expOriginalProperty, Expression.Property(null, propType.GetProperty(nameof(ShardKey<int, int>.Empty))));
                    }

                    if (isShardKey)
                    {
                        var shdData = prop.GetCustomAttribute<MapShardKeyAttribute>(true);
                        shardIdPrm = shdData.ShardIdName;
                        recordIdPrm = shdData.RecordIdName;
                        childIdPrm = null;
                    }
                    else
                    {
                        var shdData = prop.GetCustomAttribute<MapShardChildAttribute>(true);
                        shardIdPrm = shdData.ShardIdName;
                        recordIdPrm = shdData.RecordIdName;
                        childIdPrm = shdData.ChildIdName;
                    }

                    var attrPMs = prop.GetCustomAttributes<ParameterMapAttributeBase>(true);
                    foreach (var attrPM in attrPMs)
                    {
                        if (!string.IsNullOrEmpty(shardIdPrm) && attrPM.Name == shardIdPrm)
                        {
                            foundShardId = true;
                            var tDataShardId = propType.GetGenericArguments()[0];
                            if (!attrPM.IsValidType(tDataShardId))
                            {
                                throw new InvalidMapTypeException(prop, attrPM.SqlType);
                            }
                            var expShardProperty = Expression.Property(expProperty, propType.GetProperty(nameof(ShardKey<int, int>.ShardId)));
                            ParameterExpression expNullableShardId;
                            if (tDataShardId.IsValueType)
                            {
                                expNullableShardId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tDataShardId), prop.Name + "_NullableShardId");
                            }
                            else
                            {
                                expNullableShardId = Expression.Variable(tDataShardId, prop.Name + "_NullableShardId");
                            }
                            variables.Add(expNullableShardId);
                            expressions.Add(Expression.IfThenElse(
                                expDetectNullOrEmpty,
                                Expression.Assign(expNullableShardId, Expression.Convert(expShardProperty, expNullableShardId.Type)),
                                Expression.Assign(expNullableShardId, Expression.Constant(null, expNullableShardId.Type))
                                ));
                            attrPM.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expNullableShardId, expNullableShardId.Type, expLogger, logger);
                        }
                        if (attrPM.Name == recordIdPrm)
                        {
                            foundRecordId = true;
                            var tDataRecordId = propType.GetGenericArguments()[1];
                            if (!attrPM.IsValidType(tDataRecordId))
                            {
                                throw new InvalidMapTypeException(prop, attrPM.SqlType);
                            }
                            var expRecordProperty = Expression.Property(expProperty, propType.GetProperty(nameof(ShardKey<int, int>.RecordId)));
                            ParameterExpression expNullableRecordId;
                            if (tDataRecordId.IsValueType)
                            {
                                expNullableRecordId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tDataRecordId), prop.Name + "_NullableRecordId");
                            }
                            else
                            {
                                expNullableRecordId = Expression.Variable(tDataRecordId, prop.Name + "_NullableRecordId");
                            }
                            variables.Add(expNullableRecordId);
                            expressions.Add(Expression.IfThenElse(
                                expDetectNullOrEmpty,
                                Expression.Assign(expNullableRecordId, Expression.Convert(expRecordProperty, expNullableRecordId.Type)),
                                Expression.Assign(expNullableRecordId, Expression.Constant(null, expNullableRecordId.Type))
                                ));
                            attrPM.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expNullableRecordId, expNullableRecordId.Type, expLogger, logger);
                        }
                        if (isShardChild && attrPM.Name == childIdPrm)
                        {
                            foundChildId = true;
                            var tDataChildId = propType.GetGenericArguments()[2];
                            if (!attrPM.IsValidType(tDataChildId))
                            {
                                throw new InvalidMapTypeException(prop, attrPM.SqlType);
                            }
                            var expChildProperty = Expression.Property(expProperty, propType.GetProperty(nameof(ShardChild<int, int, int>.ChildId)));
                            ParameterExpression expNullableChildId;
                            if (tDataChildId.IsValueType)
                            {
                                expNullableChildId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tDataChildId), prop.Name + "_NullableChildId");
                            }
                            else
                            {
                                expNullableChildId = Expression.Variable(tDataChildId, prop.Name + "_NullableChildId");
                            }
                            variables.Add(expNullableChildId);
                            expressions.Add(Expression.IfThenElse(
                                expDetectNullOrEmpty,
                                Expression.Assign(expNullableChildId, Expression.Convert(expChildProperty, expNullableChildId.Type)),
                                Expression.Assign(expNullableChildId, Expression.Constant(null, expNullableChildId.Type))
                                ));
                            attrPM.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expNullableChildId, expNullableChildId.Type, expLogger, logger);
                        }
                    }
                    if (!string.IsNullOrEmpty(shardIdPrm) && !foundShardId)
                    {
                        throw new Exception($"The shard attribute specified a shardId attribute named {shardIdPrm}, but the attribute was not found. Remove this argument if you do not have a Shard Id.");
                    }
                    if (!foundRecordId)
                    {
                        throw new Exception($"The shard attribute specified a recordId attribute named {recordIdPrm}, but the attribute was not found.");
                    }
                    if (isShardChild && !foundChildId)
                    {
                        throw new Exception($"The ShardChild attribute specified a childId attribute named {childIdPrm}, but the attribute was not found.");
                    }
                }
                else if (prop.IsDefined(typeof(ParameterMapAttributeBase), true))
				{
					bool alreadyFound = false;
					var attrPMs = prop.GetCustomAttributes<ParameterMapAttributeBase>(true);
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
                    ExpressionHelpers.TryInstantiateMapToModel(prop, expProperty, expressions);
					IterateInMapProperties(prop.PropertyType, expressions, variables, prmSqlPrms, expProperty, expIgnoreParameters, expLogger, noDupPrmNameList, logger);
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
			logger?.CreatedExpressionTreeForSetOutParameters(TModel, outBlock);
			return lmbOut.Compile();
		}
		private static void IterateSetOutParameters(Type TModel, List<Expression> expressions, ParameterExpression prmSqlPrms, ParameterExpression expIgnoreParameters, ParameterExpression expLogger, HashSet<string> noDupPrmNameList, ILogger logger)
		{
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceSetOutMapperProperty));
			//Loop through all object properties:
			foreach (var prop in TModel.GetProperties())
			{
                var isShardKey = prop.IsDefined(typeof(MapShardKeyAttribute), true);
                var isShardChild = prop.IsDefined(typeof(MapShardChildAttribute), true);
                Type propType = null;

                if ((isShardKey || isShardChild) && prop.IsDefined(typeof(ParameterMapAttributeBase), true))
                {
                    var foundShardId = false;
                    var foundRecordId = false;
                    var foundChildId = false;
                    string shardIdPrm;
                    string recordIdPrm;
                    string childIdPrm;
                    expressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name)));

                    if (isShardKey)
                    {
                        var shdData = prop.GetCustomAttribute<MapShardKeyAttribute>(true);
                        shardIdPrm = shdData.ShardIdName;
                        recordIdPrm = shdData.RecordIdName;
                        childIdPrm = null;
                    }
                    else
                    {
                        var shdData = prop.GetCustomAttribute<MapShardChildAttribute>(true);
                        shardIdPrm = shdData.ShardIdName;
                        recordIdPrm = shdData.RecordIdName;
                        childIdPrm = shdData.ChildIdName;
                    }
                    var attrPMs = prop.GetCustomAttributes<ParameterMapAttributeBase>(true);
                    foreach (var attrPM in attrPMs)
                    {
                        if (!string.IsNullOrEmpty(shardIdPrm) && attrPM.Name == shardIdPrm)
                        {
                            foundShardId = true;
                            if (propType is null)
                            {
                                propType = prop.PropertyType;
                                if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    propType = Nullable.GetUnderlyingType(propType);
                                }
                            }
                            if (!attrPM.IsValidType(propType.GetGenericArguments()[0]))
                            {
                                throw new InvalidMapTypeException(prop, attrPM.SqlType);
                            }
                            attrPM.AppendSetOutParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expLogger, logger);
                        }
                        if (attrPM.Name == recordIdPrm)
                        {
                            foundRecordId = true;
                            if (propType is null)
                            {
                                propType = prop.PropertyType;
                                if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                    {
                                    propType = Nullable.GetUnderlyingType(propType);
                                }
                            }
                            if (!attrPM.IsValidType(propType.GetGenericArguments()[1]))
                            {
                                throw new InvalidMapTypeException(prop, attrPM.SqlType);
                            }
                            attrPM.AppendSetOutParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expLogger, logger);
                        }
                        if (isShardChild && attrPM.Name == childIdPrm)
                        {
                            foundChildId = true;
                            if (propType is null)
                            {
                                propType = prop.PropertyType;
                                if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                    {
                                    propType = Nullable.GetUnderlyingType(propType);
                                }
                            }
                            if (!attrPM.IsValidType(propType.GetGenericArguments()[2]))
                            {
                                throw new InvalidMapTypeException(prop, attrPM.SqlType);
                            }
                            attrPM.AppendSetOutParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expLogger, logger);
                        }
                    }
                    if (!string.IsNullOrEmpty(shardIdPrm) && !foundShardId)
                    {
                        throw new Exception($"The shard attribute specified a shardId attribute named {shardIdPrm}, but the attribute was not found. Remove this argument if you do not have a Shard Id.");
                    }
                    if (!foundRecordId)
                    {
                        throw new Exception($"The shard attribute specified a recordId attribute named {recordIdPrm}, but the attribute was not found.");
                    }
                    if (isShardChild && !foundChildId)
                    {
                        throw new Exception($"The ShardChild attribute specified a childId attribute named {childIdPrm}, but the attribute was not found.");
                    }
                }
                else if (prop.IsDefined(typeof(ParameterMapAttributeBase), true))
				{
					bool alreadyFound = false;
					var attrPMs = prop.GetCustomAttributes<ParameterMapAttributeBase>(true);
					foreach (var attrPM in attrPMs)
					{
						if (alreadyFound)
						{
							throw new MultipleMapAttributesException(prop);
						}
						alreadyFound = true;
						expressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name)));
                        if (propType is null)
                        {
                            propType = prop.PropertyType;
                            if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                    {
                                propType = Nullable.GetUnderlyingType(propType);
                            }
                        }
                        if (!attrPM.IsValidType(propType))
						{
							throw new InvalidMapTypeException(prop, attrPM.SqlType);
						}
						attrPM.AppendSetOutParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expLogger, logger);
					}
				}
				else if (prop.IsDefined(typeof(MapToModel)) && !prop.PropertyType.IsValueType)
				{
                    IterateSetOutParameters(prop.PropertyType, expressions, prmSqlPrms, expIgnoreParameters, expLogger, noDupPrmNameList, logger);
				}
			}
		}

        private static void HandleSetOutPrm(PropertyInfo prop, string parameterName, ParameterExpression expSprocParameters, List<Expression> expressions,
            ParameterExpression expIgnoreParameters, HashSet<string> noDupPrmNameList,
            ParameterExpression expLogger, 
            ILogger logger)
        {
            var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceSetOutMapperProperty));
            bool alreadyFound = false;
            var attrPMs = prop.GetCustomAttributes<ParameterMapAttributeBase>(true);
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
                attrPM.AppendSetOutParameterExpressions(expressions, expSprocParameters, expIgnoreParameters, noDupPrmNameList, expLogger, logger);
            }
        }

        private static Func<TShard, DbParameterCollection, ILogger, object> BuildOutGetDelegate<TShard>(DbParameterCollection parameters, Type tModel, ILogger logger) 
            where TShard : IComparable
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
			logger?.CreatedExpressionTreeForReadOutParameters(tModel, expBlock);
			return lambda.Compile();
		}
		private static void IterateGetOutParameters(Type tShard, Type tModel, ParameterExpression expShardArgument, ParameterExpression expSprocParameters, ParameterExpression expPrm, List<ParameterExpression> variableExpressions, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions, Expression expModel, ParameterExpression expLogger, LabelTarget exitLabel, ILogger logger)
		{

			foreach (var prop in tModel.GetProperties())
			{
				if ((prop.IsDefined(typeof(MapShardKeyAttribute), true) || prop.IsDefined(typeof(MapShardChildAttribute), true)) && prop.IsDefined(typeof(ParameterMapAttributeBase), true))
				{
                    int notUsed = 0;
                    HandleShardKeyChild(true, prop, tShard, tModel, expShardArgument, expSprocParameters, expPrm, null, null, null, null, ref notUsed, variableExpressions, requiredExpressions, nonrequiredExpressions, expModel, expLogger, exitLabel, logger);
				}
				else if (prop.IsDefined(typeof(ParameterMapAttributeBase), true))
				{
					bool alreadyFound = false;
					var attrPMs = prop.GetCustomAttributes<ParameterMapAttributeBase>(true);

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
                    ExpressionHelpers.TryInstantiateMapToModel(prop, expProperty, requiredExpressions);
                    IterateGetOutParameters(tShard, prop.PropertyType, expShardArgument, expSprocParameters, expPrm, variableExpressions, requiredExpressions, nonrequiredExpressions, expProperty, expLogger, exitLabel, logger);
				}
			}
		}

		private static void HandleOutProperty(ParameterMapAttributeBase attrPM, PropertyInfo prop, Type tModel, ParameterExpression expSprocParameters, ParameterExpression expPrm, List<ParameterExpression> variableExpressions, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions, Expression expModel, ParameterExpression expLogger, LabelTarget exitLabel, ILogger logger)
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
				requiredExpressions.Add(ExpressionHelpers.ReturnNullIfPrmNull(expPrm, attrPM.ParameterName, tModel, exitLabel, expLogger));

				attrPM.AppendReadOutParameterExpressions(expProperty, requiredExpressions, expSprocParameters, expPrm, prop.PropertyType, expLogger, logger);
			}
			else
			{
				nonrequiredExpressions.Add(expCallLog);
				nonrequiredExpressions.Add(expAssign);
				attrPM.AppendReadOutParameterExpressions(expProperty, nonrequiredExpressions, expSprocParameters, expPrm, prop.PropertyType, expLogger, logger);
			}

		}
		private static void HandleOutVariable(ParameterMapAttributeBase attrPM, ParameterExpression var, Type tModel, ParameterExpression expSprocParameters, ParameterExpression expPrm, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions, ParameterExpression expLogger, LabelTarget exitLabel, ILogger logger)
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
				requiredExpressions.Add(ExpressionHelpers.ReturnNullIfPrmNull(expPrm, attrPM.ParameterName, tModel, exitLabel, expLogger));
			}
			else
			{
				nonrequiredExpressions.Add(expAssign);
				attrPM.AppendReadOutParameterExpressions(var, nonrequiredExpressions, expSprocParameters, expPrm, var.Type, expLogger, logger);
			}

		}

		private static void HandleShardKeyChild(bool isOutPrms, PropertyInfo prop, Type tArgShard, Type tModel, ParameterExpression expShardArgument, 
            ParameterExpression expSprocParameters, ParameterExpression expPrm, 
            ParameterExpression expRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, List<MethodCallExpression> columnLookupExpressions, ref int propIndex,
            List<ParameterExpression> variableExpressions, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions, 
            Expression expModel, ParameterExpression expLogger, LabelTarget exitLabel, ILogger logger)
		{

			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceGetOutMapperProperty));

			MemberExpression expShardProperty = Expression.Property(expModel, prop);
			var propType = prop.PropertyType;
			var isNullableShardType = (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>));

			if (isNullableShardType)
			{
				propType = Nullable.GetUnderlyingType(propType);
			}
			var isShardKey = propType.IsGenericType && propType.Name == "ShardKey`2" && prop.IsDefined(typeof(MapShardKeyAttribute), true);
			var isShardChild = propType.IsGenericType && propType.Name == "ShardChild`3" && prop.IsDefined(typeof(MapShardChildAttribute), true);
            var isNoShardId = (tArgShard == typeof(BadShardType));

            if (isShardKey || isShardChild)
			{
				var tShardId = propType.GetProperty(nameof(ShardKey<int, int>.ShardId)).PropertyType;
				var tRecordId = propType.GetProperty(nameof(ShardKey<int, int>.RecordId)).PropertyType;
				Type tChildId = null;
                if (!isNoShardId && tArgShard != tShardId)
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
						expDataChildId = Expression.Variable(tChildId, prop.Name + "_ChildId");
					}
					variableExpressions.Add(expDataChildId);
				}

				string shardParameterName = null;
				string recordParameterName = null;
				string childParameterName = null;
				NewExpression expDataOrigin;
				if (isShardChild)
				{
					var shardPM = prop.GetCustomAttribute<MapShardChildAttribute>(true);
					shardParameterName = shardPM.ShardIdName;
					recordParameterName = shardPM.RecordIdName;
					childParameterName = shardPM.ChildIdName;
					expDataOrigin = Expression.New(typeof(DataOrigin).GetConstructor(new[] { typeof(char) }), new[] { Expression.Constant(shardPM.Origin.SourceIndicator, typeof(char)) });
				}
				else
				{
					var shardPM = prop.GetCustomAttribute<MapShardKeyAttribute>(true);
					shardParameterName = shardPM.ShardIdName;
					recordParameterName = shardPM.RecordIdName;
					expDataOrigin = Expression.New(typeof(DataOrigin).GetConstructor(new[] { typeof(char) }), new[] { Expression.Constant(shardPM.Origin.SourceIndicator, typeof(char)) });
				}

				//ShardId
				var attrPMs = prop.GetCustomAttributes<ParameterMapAttributeBase>(true);
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
                        if (attrPM.Name == shardParameterName)
                        {
                            expressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(expDataShardId.Name)));
                            shardIdFound = true;
                            if (!attrPM.IsValidType(tShardId))
                            {
                                throw new InvalidMapTypeException(expDataShardId.Name, tShardId, attrPM.SqlType);
                            }
                            var miGetParameter = typeof(ExpressionHelpers).GetMethod(nameof(ExpressionHelpers.GetParameter), BindingFlags.Static | BindingFlags.NonPublic);
                            if (isOutPrms)
                            {
                                var expAssign = Expression.Assign(expPrm, Expression.Call(miGetParameter, expSprocParameters, Expression.Constant(attrPM.ParameterName, typeof(string))));
                                expressions.Add(expAssign);
                                if (attrPM.IsRequired)
                                {
                                    requiredExpressions.Add(ExpressionHelpers.ReturnNullIfPrmNull(expPrm, attrPM.ParameterName, tModel, exitLabel, expLogger));
                                }
                                attrPM.AppendReadOutParameterExpressions(expDataShardId, expressions, expSprocParameters, expPrm, expDataShardId.Type, expLogger, logger);
                            }
                            else
                            {
                                if (attrPM.IsRequired)
                                {
                                    requiredExpressions.Add(ExpressionHelpers.ReturnNullIfColNull(expRdr, expOrdinal, tModel, exitLabel, expLogger));
                                }
                                attrPM.AppendReaderExpressions(expDataShardId, columnLookupExpressions, expressions, expRdr, expOrdinals, expOrdinal, ref propIndex, expDataShardId.Type, expLogger, logger);
                            }
                            if (expShardArgument.Type != typeof(BadShardType))
                            {
                                if (tShardId.IsValueType)
                                {
                                    expressions.Add(Expression.IfThen(
                                        //if
                                        Expression.Not(Expression.Property(expDataShardId, expDataShardId.Type.GetProperty(nameof(Nullable<int>.HasValue)))),
                                        //then
                                        Expression.Assign(expDataShardId, Expression.Convert(expShardArgument, expDataShardId.Type))
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
                            }
							break;
						}
					}
					if (!shardIdFound)
					{
						throw new Exception($"The shard map attribute specifies a ShardId name of \"{ shardParameterName }\" but no corresponding MapTo attribute was found with this parameter name.");
					}
				}
				else
				{
                    //if (isNoShardId)
                    //{
                    //    throw new Exception($"The shard map attribute does not specify a shardId parameter/column and the procedure was invoked without providing a shardId; therefore no shardId could be determined.");
                    //}
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
					if (attrPM.Name == recordParameterName)
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
                        if (isOutPrms)
                        {
                            HandleOutVariable(attrPM, expDataRecordId, tModel, expSprocParameters, expPrm, requiredExpressions, nonrequiredExpressions, expLogger, exitLabel, logger);
                        }
                        else
                        {
                            HandleRdrVariable(attrPM, expDataRecordId, tModel, expRdr, expOrdinals, expOrdinal, columnLookupExpressions, variableExpressions, requiredExpressions, nonrequiredExpressions, expModel, expLogger, ref propIndex, exitLabel, logger);
                        }
                        break;
					}
				}
				if (!recordIdFound)
				{
					throw new Exception($"The shard map attribute specifies a RecordId name of \"{ recordParameterName }\" but no corresponding MapTo attribute was found with this parameter name.");
				}
				if (isShardChild)
				{
					var childIdFound = false;
					foreach (var attrPM in attrPMs)
					{
						if (attrPM.Name == childParameterName)
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
                            if (isOutPrms)
                            {
                                HandleOutVariable(attrPM, expDataChildId, tModel, expSprocParameters, expPrm, requiredExpressions, nonrequiredExpressions, expLogger, exitLabel, logger);
                            }
                            else
                            {
                                HandleRdrVariable(attrPM, expDataChildId, tModel, expRdr, expOrdinals, expOrdinal, columnLookupExpressions, variableExpressions, requiredExpressions, nonrequiredExpressions, expModel, expLogger, ref propIndex, exitLabel, logger);
                            }
                            break;
						}
					}
					if (!childIdFound)
					{
						throw new Exception($"The shard map attribute specifies a ChildId name of \"{ childParameterName }\" but no corresponding MapTo attribute was found with this parameter name.");
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


		private static (Func<TShard, DbDataReader, int[], ILogger, TModel> DataRow, Func<DbDataReader, int[]> Ordinals) BuildReaderMapDelegate<TShard, TModel>(ILogger logger) 
            where TShard : IComparable
            where TModel : class, new()
        {
            var tModel = typeof(TModel);
			var tShard = typeof(TShard);
			ParameterExpression expShardArgument = Expression.Variable(tShard, "shardId");
			ParameterExpression prmSqlRdr = Expression.Parameter(typeof(DbDataReader), "rdr"); //input param
            //ParameterExpression expOrdinalsVar = Expression.Variable(typeof(int[]), "ordinals");
            ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var variableExpressions = new List<ParameterExpression>();
			var expModel = Expression.Variable(tModel, "model"); //model variable
			variableExpressions.Add(expModel);
			var expOrdinal = Expression.Variable(typeof(int), "ordinal");
			variableExpressions.Add(expOrdinal);
			var expOrdinalsArg = Expression.Parameter(typeof(int[]), "ordinals");
			//variableExpressions.Add(expOrdinalArg);

			var initialExpressions = new List<Expression>();
			var columnLookupExpressions = new List<MethodCallExpression>();
			var expressionPrms = new ParameterExpression[] { expShardArgument, prmSqlRdr, expOrdinalsArg, expLogger };

            var expExitLabel = Expression.Label(tModel);

            var propIndex = 0;

			var expAssign = Expression.Assign(expModel, Expression.New(tModel));
			initialExpressions.Add(expAssign);
            var subsequentExpressions = new List<Expression>();

            //Loop through all object properties:
            IterateRdrColumns(tShard, tModel, expModel, expShardArgument, columnLookupExpressions, variableExpressions, initialExpressions, subsequentExpressions, prmSqlRdr, expOrdinalsArg, expOrdinal, ref propIndex, expLogger, expExitLabel, logger);

            subsequentExpressions.Add(Expression.Goto(expExitLabel, expModel)); //return value;

            var resultExpressions = new List<Expression>();
            //resultExpressions.Add(Expression.Assign(expOrdinalsArg, Expression.NewArrayInit(typeof(int), columnLookupExpressions.ToArray())));
            resultExpressions.AddRange(initialExpressions);
            resultExpressions.AddRange(subsequentExpressions);
            resultExpressions.Add(Expression.Label(expExitLabel, Expression.Constant(null, tModel)));

            var expDataBlock = Expression.Block(tModel, variableExpressions, resultExpressions);
            var lambdaDataRow = Expression.Lambda<Func<TShard, DbDataReader, int[], ILogger, TModel>>(expDataBlock, expressionPrms);

            var expOrdinalArray = Expression.NewArrayInit(typeof(int), columnLookupExpressions.ToArray());
            //var lambdaOrdinals = Expression.Lambda<Func<DbDataReader, int[]>>(expOrdinalArray, new ParameterExpression[] { prmSqlRdr, expOrdinalsArg });
            var lambdaOrdinals = Expression.Lambda<Func<DbDataReader, int[]>>(expOrdinalArray, new ParameterExpression[] { prmSqlRdr });
            logger?.CreatedExpressionTreeForReaderOrdinals(tModel, expOrdinalArray);
            logger?.CreatedExpressionTreeForReaderRowData(tModel, expDataBlock);

            return (lambdaDataRow.Compile(), lambdaOrdinals.Compile());
		}
		private static void IterateRdrColumns(Type tShard, Type tModel, Expression expModel, ParameterExpression expShardArgument, List<MethodCallExpression> columnLookupExpressions, List<ParameterExpression> variableExpressions, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions, ParameterExpression expRdr, ParameterExpression expOrdinalsArg, ParameterExpression expOrdinal, ref int propIndex, ParameterExpression expLogger, LabelTarget exitLabel, ILogger logger)
		{
			foreach (var prop in tModel.GetProperties())
			{
                if ((prop.IsDefined(typeof(MapShardKeyAttribute), true) || prop.IsDefined(typeof(MapShardChildAttribute), true)) && prop.IsDefined(typeof(ParameterMapAttributeBase), true))
                {
                    HandleShardKeyChild(false, prop, tShard, tModel, expShardArgument, null, null, expRdr, expOrdinalsArg, expOrdinal, columnLookupExpressions, ref propIndex, variableExpressions, requiredExpressions, nonrequiredExpressions, expModel, expLogger, exitLabel, logger);
                }
                else if (prop.IsDefined(typeof(ParameterMapAttributeBase), true))
				{
                    var alreadyFound = false;
					var attrPMs = prop.GetCustomAttributes<ParameterMapAttributeBase>(true);

                    foreach (var attrPM in attrPMs)
					{
						if (alreadyFound)
						{
							throw new MultipleMapAttributesException(prop);
						}
						alreadyFound = true;
                        HandleRdrColumn(attrPM, prop, tModel, expRdr, expOrdinalsArg, expOrdinal, columnLookupExpressions, variableExpressions, requiredExpressions, nonrequiredExpressions, expModel, expLogger, ref propIndex, exitLabel, logger);
					}
					//propIndex++;
				}
				else if (prop.IsDefined(typeof(MapToModel)) && !prop.PropertyType.IsValueType)
				{
					MemberExpression expProperty = Expression.Property(expModel, prop);
                    ExpressionHelpers.TryInstantiateMapToModel(prop, expProperty, requiredExpressions);
                    IterateRdrColumns(tShard, prop.PropertyType, expProperty, expShardArgument, columnLookupExpressions, variableExpressions, requiredExpressions, nonrequiredExpressions, expRdr, expOrdinalsArg, expOrdinal, ref propIndex, expLogger, exitLabel, logger);
				}
			}

		}
        private static void HandleRdrColumn(ParameterMapAttributeBase attrPM, PropertyInfo prop, Type tModel, ParameterExpression expRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, List<MethodCallExpression> columnLookupExpressions, List<ParameterExpression> variableExpressions, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions, Expression expModel, ParameterExpression expLogger, ref int propIndex, LabelTarget exitLabel, ILogger logger)
        {
            var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceRdrMapperProperty));
            var expCallLog = Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name));

            if (!attrPM.IsValidType(prop.PropertyType))
            {
                throw new InvalidMapTypeException(prop, attrPM.SqlType);
            }

            MemberExpression expProperty = Expression.Property(expModel, prop);

            if (attrPM.IsRequired)
            {
                requiredExpressions.Add(expCallLog);
                requiredExpressions.Add(ExpressionHelpers.ReturnNullIfColNull(expRdr, expOrdinal, tModel, exitLabel, expLogger));
                attrPM.AppendReaderExpressions(expProperty, columnLookupExpressions, requiredExpressions, expRdr, expOrdinals, expOrdinal, ref propIndex, prop.PropertyType, expLogger, logger);

            }
            else
            {
                nonrequiredExpressions.Add(expCallLog);
                attrPM.AppendReaderExpressions(expProperty, columnLookupExpressions, nonrequiredExpressions, expRdr, expOrdinals, expOrdinal, ref propIndex, prop.PropertyType, expLogger, logger);
            }
        }
        private static void HandleRdrVariable(ParameterMapAttributeBase attrPM, ParameterExpression var, Type tModel, ParameterExpression expRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, List<MethodCallExpression> columnLookupExpressions, List<ParameterExpression> variableExpressions, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions, Expression expModel, ParameterExpression expLogger, ref int propIndex, LabelTarget exitLabel, ILogger logger)
        {
            var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceRdrMapperProperty));
            if (!attrPM.IsValidType(var.Type))
            {
                throw new InvalidMapTypeException(var.Name, var.Type, attrPM.SqlType);
            }
            if (attrPM.IsRequired)
            {
                requiredExpressions.Add(ExpressionHelpers.ReturnNullIfColNull(expRdr, expOrdinal, tModel, exitLabel, expLogger));
                attrPM.AppendReaderExpressions(var, columnLookupExpressions, requiredExpressions, expRdr, expOrdinals, expOrdinal, ref propIndex, var.Type, expLogger, logger);

            }
            else
            {
                attrPM.AppendReaderExpressions(var, columnLookupExpressions, nonrequiredExpressions, expRdr, expOrdinals, expOrdinal, ref propIndex, var.Type, expLogger, logger);
            }
        }
        #endregion
        #region Convert Sql result to object(s)

        /// <summary>
        /// Uses Mapping attributes to return a list of TModel records, populated from DataReader rows.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The DbDataReader containing tables and rows.</param>
        /// <param name="parameters">Not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>A list of TModel objects, one for each record returned by the DataReader.</returns>
        public static IList<TModel> ListFromReaderResultsHandler<TShard, TModel>
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
            => Mapper.ToList<TShard, TModel>(rdr, shardId, logger);

        private static void ValidateDataReader(string sprocName, DbDataReader rdr, string connectionDescription, ILogger logger)
        {
            if (rdr is null)
            {
                logger?.DataReaderIsNull(sprocName, connectionDescription);
                throw new Exception($"The procedure {sprocName} on connection {connectionDescription} did not return a data reader object.");
            }
            if (rdr.IsClosed)
            {
                logger?.DataReaderIsClosed(sprocName, connectionDescription);
                throw new Exception($"The procedure {sprocName} on connection {connectionDescription} returned a data reader that is not open.");
            }
        }

        #region Handle Complex Models with both DataReader and Output Paramters
        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">Not used, but required for method signature.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TShard, TModel>
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
        {
            MoveRdrToEnd(rdr);
            return Mapper.ToModel<TShard, TModel>(parameters, shardId, logger);
        }


        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult">A type with attributes that correspond to the data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with one result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TShard, TModel, TReaderResult>
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
            where TModel : class, new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            var resultList = (List<TReaderResult>)Mapper.ToList<TShard, TReaderResult>(rdr, shardId, logger);
            MoveRdrToEnd(rdr);
            var resultOutPrms = Mapper.ToModel<TShard, TModel>(parameters, shardId, logger);
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TReaderResult, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(shardId, sprocName, 3, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TReaderResult>, TModel, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList, resultOutPrms, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with two result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>
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
            where TModel : class, new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TShard, TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TShard, TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            MoveRdrToEnd(rdr);
            var resultOutPrms = Mapper.ToModel<TShard, TModel>(parameters, shardId, logger);
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(shardId, sprocName, 7, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, TModel, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList0, resultList1, resultOutPrms, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with three result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>
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
            where TModel : class, new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TShard, TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TShard, TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TShard, TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            MoveRdrToEnd(rdr);
            var resultOutPrms = Mapper.ToModel<TShard, TModel>(parameters, shardId, logger);
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(shardId, sprocName, 15, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, TModel, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList0, resultList1, resultList2, resultOutPrms, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with four result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
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
            where TModel : class, new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TShard, TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TShard, TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TShard, TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TShard, TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            MoveRdrToEnd(rdr);
            var resultOutPrms = Mapper.ToModel<TShard, TModel>(parameters, shardId, logger);
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(shardId, sprocName, 31, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, TModel, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultOutPrms, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to the fifth data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with five result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
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
            where TModel : class, new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TShard, TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TShard, TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TShard, TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TShard, TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TShard, TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            MoveRdrToEnd(rdr);
            var resultOutPrms = Mapper.ToModel<TShard, TModel>(parameters, shardId, logger);
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(shardId, sprocName, 63, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, TModel, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultOutPrms, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to the fifth data reader result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to the sixth data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with six result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
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
            where TModel : class, new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;
            List<TReaderResult5> resultList5;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TShard, TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TShard, TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TShard, TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TShard, TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TShard, TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            if (rdr.NextResult())
            {
                resultList5 = (List<TReaderResult5>)Mapper.ToList<TShard, TReaderResult5>(rdr, shardId, logger);
            }
            else
            {
                resultList5 = new List<TReaderResult5>();
            }
            MoveRdrToEnd(rdr);
            var resultOutPrms = Mapper.ToModel<TShard, TModel>(parameters, shardId, logger);
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, Mapper.DummyType, Mapper.DummyType, TModel>(shardId, sprocName, 127, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, TModel, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultOutPrms, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to the fifth data reader result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to the sixth data reader result.</typeparam>
        /// <typeparam name="TReaderResult6">A type with attributes that correspond to the seventh data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with seven result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
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
            where TModel : class, new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;
            List<TReaderResult5> resultList5;
            List<TReaderResult6> resultList6;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TShard, TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TShard, TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TShard, TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TShard, TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TShard, TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            if (rdr.NextResult())
            {
                resultList5 = (List<TReaderResult5>)Mapper.ToList<TShard, TReaderResult5>(rdr, shardId, logger);
            }
            else
            {
                resultList5 = new List<TReaderResult5>();
            }
            if (rdr.NextResult())
            {
                resultList6 = (List<TReaderResult6>)Mapper.ToList<TShard, TReaderResult6>(rdr, shardId, logger);
            }
            else
            {
                resultList6 = new List<TReaderResult6>();
            }
            MoveRdrToEnd(rdr);
            var resultOutPrms = Mapper.ToModel<TShard, TModel>(parameters, shardId, logger);
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, Mapper.DummyType, TModel>(shardId, sprocName, 255, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, TModel, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultList6, resultOutPrms, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to the fifth data reader result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to the sixth data reader result.</typeparam>
        /// <typeparam name="TReaderResult6">A type with attributes that correspond to the seventh data reader result.</typeparam>
        /// <typeparam name="TReaderResult7">A type with attributes that correspond to the eighth data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with eight result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
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
            where TModel : class, new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;
            List<TReaderResult5> resultList5;
            List<TReaderResult6> resultList6;
            List<TReaderResult7> resultList7;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TShard, TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TShard, TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TShard, TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TShard, TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TShard, TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            if (rdr.NextResult())
            {
                resultList5 = (List<TReaderResult5>)Mapper.ToList<TShard, TReaderResult5>(rdr, shardId, logger);
            }
            else
            {
                resultList5 = new List<TReaderResult5>();
            }
            if (rdr.NextResult())
            {
                resultList6 = (List<TReaderResult6>)Mapper.ToList<TShard, TReaderResult6>(rdr, shardId, logger);
            }
            else
            {
                resultList6 = new List<TReaderResult6>();
            }
            if (rdr.NextResult())
            {
                resultList7 = (List<TReaderResult7>)Mapper.ToList<TShard, TReaderResult7>(rdr, shardId, logger);
            }
            else
            {
                resultList7 = new List<TReaderResult7>();
            }
            MoveRdrToEnd(rdr);
            var resultOutPrms = Mapper.ToModel<TShard, TModel>(parameters, shardId, logger);
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TModel>(shardId, sprocName, 511, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, List<TReaderResult7>, TModel, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultList6, resultList7, resultOutPrms, logger);
        }
        #endregion

        #region Handle Complex Models with DataReader results
        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with one result set.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TShard, TModel>
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
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            var resultList = (List<TModel>)Mapper.ToList<TShard, TModel>(rdr, shardId, logger);
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TModel, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType>(shardId, sprocName, 2, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TModel>, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with two result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1>
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
            where TModel : class, new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TShard, TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TShard, TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(shardId, sprocName, 6, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList0, resultList1, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with three result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2>
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
            where TModel : class, new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TShard, TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TShard, TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TShard, TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(shardId, sprocName, 14, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList0, resultList1, resultList2, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with four result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
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
            where TModel : class, new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TShard, TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TShard, TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TShard, TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TShard, TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(shardId, sprocName, 30, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList0, resultList1, resultList2, resultList3, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to the fifth data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with five result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
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
            where TModel : class, new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TShard, TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TShard, TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TShard, TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TShard, TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TShard, TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(shardId, sprocName, 62, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to the fifth data reader result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to the sixth data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with six result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
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
            where TModel : class, new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;
            List<TReaderResult5> resultList5;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TShard, TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TShard, TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TShard, TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TShard, TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TShard, TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            if (rdr.NextResult())
            {
                resultList5 = (List<TReaderResult5>)Mapper.ToList<TShard, TReaderResult5>(rdr, shardId, logger);
            }
            else
            {
                resultList5 = new List<TReaderResult5>();
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, Mapper.DummyType, Mapper.DummyType, TModel>(shardId, sprocName, 126, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5> , ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to the fifth data reader result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to the sixth data reader result.</typeparam>
        /// <typeparam name="TReaderResult6">A type with attributes that correspond to the seventh data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with seven result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
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
            where TModel : class, new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;
            List<TReaderResult5> resultList5;
            List<TReaderResult6> resultList6;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TShard, TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TShard, TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TShard, TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TShard, TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TShard, TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            if (rdr.NextResult())
            {
                resultList5 = (List<TReaderResult5>)Mapper.ToList<TShard, TReaderResult5>(rdr, shardId, logger);
            }
            else
            {
                resultList5 = new List<TReaderResult5>();
            }
            if (rdr.NextResult())
            {
                resultList6 = (List<TReaderResult6>)Mapper.ToList<TShard, TReaderResult6>(rdr, shardId, logger);
            }
            else
            {
                resultList6 = new List<TReaderResult6>();
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, Mapper.DummyType, TModel>(shardId, sprocName, 254, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultList6, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TShard">The type of the shard identifier.</typeparam>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to the fifth data reader result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to the sixth data reader result.</typeparam>
        /// <typeparam name="TReaderResult6">A type with attributes that correspond to the seventh data reader result.</typeparam>
        /// <typeparam name="TReaderResult7">A type with attributes that correspond to the eighth data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The open data reader with eight result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
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
            where TModel : class, new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;
            List<TReaderResult5> resultList5;
            List<TReaderResult6> resultList6;
            List<TReaderResult7> resultList7;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TShard, TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TShard, TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TShard, TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TShard, TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TShard, TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            if (rdr.NextResult())
            {
                resultList5 = (List<TReaderResult5>)Mapper.ToList<TShard, TReaderResult5>(rdr, shardId, logger);
            }
            else
            {
                resultList5 = new List<TReaderResult5>();
            }
            if (rdr.NextResult())
            {
                resultList6 = (List<TReaderResult6>)Mapper.ToList<TShard, TReaderResult6>(rdr, shardId, logger);
            }
            else
            {
                resultList6 = new List<TReaderResult6>();
            }
            if (rdr.NextResult())
            {
                resultList7 = (List<TReaderResult7>)Mapper.ToList<TShard, TReaderResult7>(rdr, shardId, logger);
            }
            else
            {
                resultList7 = new List<TReaderResult7>();
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TModel>(shardId, sprocName, 510, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, List<TReaderResult7>, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultList6, resultList7, logger);
        }
        #endregion

		private static TModel AssignRootToResult<TEval, TModel>(string procedureName, IList<TEval> resultList, ILogger logger) where TModel : class, new() where TEval : class, new()
		{
			if (resultList is null)
			{
				throw new Exception($"Procedure {procedureName} failed to return an expected base recordset result.");
			}
			else if (resultList.Count == 0)
			{
				logger?.LogDebug($"Procedure {procedureName} returned an empty base result.");
				return null;
			}
			else if (resultList.Count > 1)
			{
                throw new UnexpectedMultiRowResultException(procedureName);
            }
            else
			{
				var result = resultList[0] as TModel;
				return result;
			}
		}

        //private static Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, List<TReaderResult7>, TOutResult, ILogger, TModel>
        private static Delegate BuildModelFromResultsExpressions<TShard, TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TOutResult>
			(TShard shardId, string procedureName, int recordSetFlags, ILogger logger)
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
            var expShardId = Expression.Parameter(typeof(TShard), "shardId");
            var expProcName = Expression.Parameter(typeof(string), "sprocName");
            var expResultSet0 = Expression.Parameter(typeof(List<TReaderResult0>), "rstResult0");
            var expResultSet1 = Expression.Parameter(typeof(List<TReaderResult1>), "rstResult1");
            var expResultSet2 = Expression.Parameter(typeof(List<TReaderResult2>), "rstResult2");
            var expResultSet3 = Expression.Parameter(typeof(List<TReaderResult3>), "rstResult3");
            var expResultSet4 = Expression.Parameter(typeof(List<TReaderResult4>), "rstResult4");
            var expResultSet5 = Expression.Parameter(typeof(List<TReaderResult5>), "rstResult5");
            var expResultSet6 = Expression.Parameter(typeof(List<TReaderResult6>), "rstResult6");
            var expResultSet7 = Expression.Parameter(typeof(List<TReaderResult7>), "rstResult7");
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
			var miSetResultSetGeneric = typeof(Mapper).GetMethod(nameof(Mapper.AssignRootToResult), BindingFlags.Static | BindingFlags.NonPublic);
			var miFormat = typeof(string).GetMethod(nameof(string.Concat), BindingFlags.Static);
			var resultType = typeof(TModel);

            var tModel = typeof(TModel);
			// 1. Try to create an instance of our result class.
			using (logger?.BuildSqlResultsHandlerScope(procedureName, tModel))
			{
				//Set base type to some result value, if we can.
				if ((recordSetFlags & 1) == 1 && resultType == typeof(TOutResult))
				{
					expressions.Add(Expression.Assign(expModel, expResultOut));
					isPrmOutUsed = true;
				}
				else if ((recordSetFlags & 2) == 2 && resultType == typeof(TReaderResult0))
				{
					expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod(new Type[] { typeof(TReaderResult0), resultType }), expProcName, expResultSet0, expLogger)));
					expressions.Add(Expression.IfThen(
						Expression.Equal(expModel, expNull), //if (result == null)
						Expression.Return(expExitTarget, expNull, tModel) //return null;
						));
					isRdrResult0Used = true;
				}
				else if ((recordSetFlags & 4) == 4 && resultType == typeof(TReaderResult1))
				{
					expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult1), resultType }), expProcName, expResultSet1, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel))));
					isRdrResult1Used = true;
				}
				else if ((recordSetFlags & 8) == 8 && resultType == typeof(TReaderResult2))
				{
					expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult2), resultType }), expProcName, expResultSet2, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel))));
					isRdrResult2Used = true;
				}
				else if ((recordSetFlags & 16) == 16 && resultType == typeof(TReaderResult3))
				{
					expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult3), resultType }), expProcName, expResultSet3, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel))));
					isRdrResult3Used = true;
				}
				else if ((recordSetFlags & 32) == 32 && resultType == typeof(TReaderResult4))
				{
					expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult4), resultType }), expProcName, expResultSet4, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel))));
					isRdrResult4Used = true;
				}
				else if ((recordSetFlags & 64) == 64 && resultType == typeof(TReaderResult5))
				{
					expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult5), resultType }), expProcName, expResultSet5, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel))));
					isRdrResult5Used = true;
				}
				else if ((recordSetFlags & 128) == 128 && resultType == typeof(TReaderResult6))
				{
					expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult6), resultType }), expProcName, expResultSet6, expLogger)));
					expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, expNull, typeof(TModel))));
					isRdrResult6Used = true;
				}
				else if ((recordSetFlags & 256) == 256 && resultType == typeof(TReaderResult7))
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
                    if ((recordSetFlags & 2) == 2 && prop.PropertyType == expResultSet0.Type && !isRdrResult0Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet0));
                        isRdrResult0Used = true;
                    }
                    else if ((recordSetFlags & 2) == 2 && prop.PropertyType.IsAssignableFrom(expResultSet0.Type) && !isRdrResult0Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), Expression.Convert(expResultSet0, prop.PropertyType)));
                        isRdrResult0Used = true;
                    }
                    else if ((recordSetFlags & 4) == 4 && prop.PropertyType == expResultSet1.Type && !isRdrResult1Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet1));
                        isRdrResult1Used = true;
                    }
                    else if ((recordSetFlags & 4) == 4 && prop.PropertyType.IsAssignableFrom(expResultSet1.Type) && !isRdrResult1Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), Expression.Convert(expResultSet1, prop.PropertyType)));
                        isRdrResult1Used = true;
                    }
                    else if ((recordSetFlags & 8) == 8 && prop.PropertyType == expResultSet2.Type && !isRdrResult2Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet2));
                        isRdrResult2Used = true;
                    }
                    else if ((recordSetFlags & 8) == 8 && prop.PropertyType.IsAssignableFrom(expResultSet2.Type) && !isRdrResult2Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), Expression.Convert(expResultSet2, prop.PropertyType)));
                        isRdrResult2Used = true;
                    }
                    else if ((recordSetFlags & 16) == 16 && prop.PropertyType == expResultSet3.Type && !isRdrResult3Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet3));
                        isRdrResult3Used = true;
                    }
                    else if ((recordSetFlags & 16) == 16 && prop.PropertyType.IsAssignableFrom(expResultSet3.Type) && !isRdrResult3Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), Expression.Convert(expResultSet3, prop.PropertyType)));
                        isRdrResult3Used = true;
                    }
                    else if ((recordSetFlags & 32) == 32 && prop.PropertyType == expResultSet4.Type && !isRdrResult4Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet4));
                        isRdrResult4Used = true;
                    }
                    else if ((recordSetFlags & 32) == 32 && prop.PropertyType.IsAssignableFrom(expResultSet4.Type) && !isRdrResult4Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), Expression.Convert(expResultSet4, prop.PropertyType)));
                        isRdrResult4Used = true;
                    }
                    else if ((recordSetFlags & 64) == 64 && prop.PropertyType == expResultSet5.Type && !isRdrResult5Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet5));
                        isRdrResult5Used = true;
                    }
                    else if ((recordSetFlags & 64) == 64 && prop.PropertyType.IsAssignableFrom(expResultSet5.Type) && !isRdrResult5Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), Expression.Convert(expResultSet5, prop.PropertyType)));
                        isRdrResult5Used = true;
                    }
                    else if ((recordSetFlags & 128) == 128 && prop.PropertyType == expResultSet6.Type && !isRdrResult6Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet6));
                        isRdrResult6Used = true;
                    }
                    else if ((recordSetFlags & 128) == 128 && prop.PropertyType.IsAssignableFrom(expResultSet6.Type) && !isRdrResult6Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), Expression.Convert(expResultSet6, prop.PropertyType)));
                        isRdrResult6Used = true;
                    }
                    else if ((recordSetFlags & 256) == 256 && prop.PropertyType == expResultSet7.Type && !isRdrResult7Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultSet7));
                        isRdrResult7Used = true;
                    }
                    else if ((recordSetFlags & 256) == 256 && prop.PropertyType.IsAssignableFrom(expResultSet7.Type) && !isRdrResult7Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expModel, prop), Expression.Convert(expResultSet7, prop.PropertyType)));
                        isRdrResult7Used = true;
                    }
                }
                //Iterate over any object (non-list) properties and set any resultSet that match.
                foreach (var prop in props)
				{
					if ((recordSetFlags & 1) == 1 && prop.PropertyType.IsAssignableFrom(typeof(TOutResult)) && !isPrmOutUsed)
					{
						expressions.Add(Expression.Assign(Expression.Property(expModel, prop), expResultOut));
						isPrmOutUsed = true;
					}
					else if ((recordSetFlags & 2) == 2 && prop.PropertyType.IsAssignableFrom(typeof(TReaderResult0)) && !isRdrResult0Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));   //var count = 0;
						expressions.Add(Expression.IfThen(                                      //if (resultSet0 != null)
							Expression.NotEqual(expResultSet0, expNull),                        //{ 
							Expression.Assign(expCount, Expression.Property(expResultSet0, miCount)) //count = resultSet0.Count;
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
					else if ((recordSetFlags & 4) == 4 && prop.PropertyType.IsAssignableFrom(typeof(TReaderResult1)) && !isRdrResult1Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet1, expNull), Expression.Assign(expCount, Expression.Property(expResultSet1, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet1, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult1Used = true;
					}
					else if ((recordSetFlags & 8) == 8 && prop.PropertyType.IsAssignableFrom(typeof(TReaderResult2)) && !isRdrResult2Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet2, expNull), Expression.Assign(expCount, Expression.Property(expResultSet2, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet2, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult2Used = true;
					}
					else if ((recordSetFlags & 16) == 16 && prop.PropertyType.IsAssignableFrom(typeof(TReaderResult3)) && !isRdrResult3Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet3, expNull), Expression.Assign(expCount, Expression.Property(expResultSet3, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet3, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult3Used = true;
					}
					else if ((recordSetFlags & 32) == 32 && prop.PropertyType.IsAssignableFrom(typeof(TReaderResult4)) && !isRdrResult4Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet4, expNull), Expression.Assign(expCount, Expression.Property(expResultSet4, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet4, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult4Used = true;
					}
					else if ((recordSetFlags & 64) == 64 && prop.PropertyType.IsAssignableFrom(typeof(TReaderResult5)) && !isRdrResult5Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet5, expNull), Expression.Assign(expCount, Expression.Property(expResultSet5, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet5, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult5Used = true;
					}
					else if ((recordSetFlags & 128) == 128 && prop.PropertyType.IsAssignableFrom(typeof(TReaderResult6)) && !isRdrResult6Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet6, expNull), Expression.Assign(expCount, Expression.Property(expResultSet6, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expModel, prop), Expression.Property(expResultSet6, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult6Used = true;
					}
					else if ((recordSetFlags & 256) == 256 && prop.PropertyType.IsAssignableFrom(typeof(TReaderResult7)) && !isRdrResult7Used)
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

            if (recordSetFlags == 2)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TModel>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 3)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expResultOut, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TReaderResult0>, TModel, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 6)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expResultSet1, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 7)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expResultSet1, expResultOut, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, TModel, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 14)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 15)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultOut, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, TModel, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 30)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 31)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultOut, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, TModel, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 62)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 63)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultOut, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, TModel, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 126)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 127)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expResultOut, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, TModel, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 254)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expResultSet6, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 255)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expResultSet6, expResultOut, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, TModel, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 510)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expResultSet6, expResultSet7, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, List<TReaderResult7>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 511)
            {
                var prms = new ParameterExpression[] { expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expResultSet6, expResultSet7, expResultOut, expLogger };
                var lambda = Expression.Lambda<Func<TShard, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, List<TReaderResult7>, TModel, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            throw new Exception("Invalid recordSetFlags value");
        }
        private static void MoveRdrToEnd(DbDataReader rdr)
        {
            if (!(rdr is null) && !rdr.IsClosed)
            {
                while (rdr.NextResult())
                {
                    //skip any unprocessed recordset results (if any),
                    //Out parameters are at the end of the TDS stream on SQL Server.
                }
            }
        }

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
