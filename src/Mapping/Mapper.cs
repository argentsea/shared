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
using System.ComponentModel;
using System.Data.SqlTypes;

namespace ArgentSea
{
    /// <summary>
    /// This static class contains the logic for mapping database parameters to/from properties.
    /// </summary>
	public static class Mapper
	{
        private static readonly ConcurrentDictionary<Type, Lazy<Delegate>> _cacheInParamSet = new ConcurrentDictionary<Type, Lazy<Delegate>>();
        private static readonly ConcurrentDictionary<Type, Lazy<Action<DbParameterCollection, HashSet<string>, ILogger>>> _cacheOutParamSet = new ConcurrentDictionary<Type, Lazy<Action<DbParameterCollection, HashSet<string>, ILogger>>>();
        private static readonly ConcurrentDictionary<Type, Lazy<(Delegate RowData, Delegate Ordinals)>> _getRdrMapCache = new ConcurrentDictionary<Type, Lazy<(Delegate, Delegate)>>();
        private static readonly ConcurrentDictionary<Type, Lazy<Delegate>> _getOutParamReadCache = new ConcurrentDictionary<Type, Lazy<Delegate>>();
        private static readonly ConcurrentDictionary<string, Lazy<Delegate>> _getOutObjectCache = new ConcurrentDictionary<string, Lazy<Delegate>>();
        private static readonly ConcurrentDictionary<string, Lazy<Delegate>> _getRstObjectCache = new ConcurrentDictionary<string, Lazy<Delegate>>();

        #region Public methods


        public static void AppendRecordIdParameter(this DbParameterCollection parameters)
        {

        }


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

        #region Set Model from Output Parameters
        /// <summary>
        /// Creates a new object with property values based upon the provided output parameters which correspond to the MapTo attributes.
        /// </summary>
        /// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to read the Sql parameter collection values.</typeparam>
        /// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object after a database query.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>An object of the specified type, with properties set to parameter values.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ToModel<TModel>(this DbParameterCollection parameters, ILogger logger) 
            where TModel : new()
			=> SetModel<TModel>(parameters, new TModel(), 0, logger);

        /// <summary>
        /// Creates a new object with property values based upon the provided output parameters which correspond to the MapTo attributes.
        /// </summary>
        /// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to read the Sql parameter collection values.</typeparam>
        /// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object after a database query.</param>
        /// <param name="shardId">The identifier for the current shard. Required for ShardKey and ShardChild types.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>An object of the specified type, with properties set to parameter values.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ToModel<TModel>(this DbParameterCollection parameters, short shardId, ILogger logger) 
            where TModel : new() 
            => SetModel<TModel>(parameters, new TModel(), shardId, logger);

        /// <summary>
        /// Creates a new object with property values based upon the provided output parameters which correspond to the MapTo attributes.
        /// </summary>
        /// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to read the Sql parameter collection values.</typeparam>
        /// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object after a database query.</param>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>An object of the specified type, with properties set to parameter values.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel SetModel<TModel>(this DbParameterCollection parameters, TModel instance, ILogger logger)
            => SetModel<TModel>(parameters, instance, 0, logger);

        /// <summary>
        /// Creates a new object with property values based upon the provided output parameters which correspond to the MapTo attributes.
        /// </summary>
        /// <typeparam name="TModel">The type of the object. The "MapTo" attributes are used to read the Sql parameter collection values.</typeparam>
        /// <param name="parameters">A parameter collection, generally belonging to a ADO.Net Command object after a database query.</param>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The identifier for the current shard. Required for ShardKey and ShardChild types.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>An object of the specified type, with properties set to parameter values.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel SetModel<TModel>(this DbParameterCollection parameters, TModel instance, short shardId, ILogger logger)
        {
            var tModel = typeof(TModel);
            if (instance is null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            var lazySqlOutDelegate = _getOutParamReadCache.GetOrAdd(tModel, new Lazy<Delegate>(() => BuildOutGetDelegate<TModel>(parameters, tModel, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            if (lazySqlOutDelegate.IsValueCreated)
            {
                LoggingExtensions.SqlReadOutParametersCacheHit(logger, tModel);
            }
            else
            {
                LoggingExtensions.SqlReadOutParametersCacheMiss(logger, tModel);
            }
            var del = lazySqlOutDelegate.Value;
            return ((Func<TModel, short, DbParameterCollection, ILogger, TModel>)del)(instance, shardId, parameters, logger);
        }

        #endregion

        #region Set Model from Data Reader

        /// <summary>
        /// Accepts a single-row data reader object and returns a an object instance of the specified type using Mapping attributes.
        /// </summary>
        /// <typeparam name="TModel">The type of the result.</typeparam>
        /// <param name="rdr">The data reader, set to the current result set.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>An object of the specified type.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ToModel<TModel>(this DbDataReader rdr, ILogger logger)
            where TModel : new()
            => SetModel<TModel>(rdr, new TModel(), 0, logger);

        /// <summary>
        /// Accepts a single-row data reader object and returns a an object instance of the specified type using Mapping attributes.
        /// </summary>
        /// <typeparam name="TModel">The type of the result.</typeparam>
        /// <param name="rdr">The data reader, set to the current result set.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>An object of the specified type.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ToModel<TModel>(this DbDataReader rdr, short shardId, ILogger logger)
            where TModel : new()
            => SetModel<TModel>(rdr, new TModel(), shardId, logger);

        /// <summary>
        /// Accepts a single-row data reader object and returns a an object instance of the specified type using Mapping attributes.
        /// </summary>
        /// <typeparam name="TModel">The type of the result.</typeparam>
        /// <param name="rdr">The data reader, set to the current result set.</param>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>An object of the specified type.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel SetModel<TModel>(this DbDataReader rdr, TModel instance, ILogger logger)
            => SetModel<TModel>(rdr, instance, 0, logger);

        /// <summary>
        /// Accepts a single-row data reader object and returns a an object instance of the specified type using Mapping attributes.
        /// </summary>
        /// <typeparam name="TModel">The type of the result.</typeparam>
        /// <param name="shardId">The identifier for the current shard. Required for ShardKey and ShardChild types.</param>
        /// <param name="rdr">The data reader, set to the current result set.</param>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>An object of the specified type.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel SetModel<TModel>(this DbDataReader rdr, TModel instance, short shardId, ILogger logger)
        {
            TModel result = default;
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

            var lazySqlRdrDelegate = _getRdrMapCache.GetOrAdd(tModel, new Lazy<(Delegate RowData, Delegate Ordinals)>(() => BuildReaderMapDelegate<TModel>(logger), LazyThreadSafetyMode.ExecutionAndPublication));
            if (lazySqlRdrDelegate.IsValueCreated)
            {
                LoggingExtensions.SqlReaderCacheHit(logger, tModel);
            }
            else
            {
                LoggingExtensions.SqlReadOutParametersCacheMiss(logger, tModel);
            }

            int[] ordinals = ((Func<DbDataReader, ILogger, int[]>)lazySqlRdrDelegate.Value.Ordinals)(rdr, logger);
            if (rdr.Read())
            {
                result = ((Func<TModel, short, DbDataReader, int[], ILogger, TModel>)lazySqlRdrDelegate.Value.RowData)(instance, shardId, rdr, ordinals, logger);
            }
            if (rdr.Read())
            {
                throw new UnexpectedMultiRowResultException();
            }
            return result;
        }

        #endregion

        #region Set List From Data Reader Rows
        /// <summary>
        /// Accepts a data reader object and returns a list of objects of the specified type, one for each record.
        /// </summary>
        /// <typeparam name="TModel">The type of the list result</typeparam>
        /// <param name="rdr">The data reader, set to the current result set.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>A list of objects of the specified type, one for each result.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static List<TModel> ToList<TModel>(this DbDataReader rdr, ILogger logger) 
            where TModel : new()
			=> ToList<TModel>(rdr, 0, logger);

        /// <summary>
        /// Accepts a data reader object and returns a list of objects of the specified type, one for each record.
        /// </summary>
        /// <typeparam name="TModel">The type of the list result.</typeparam>
        /// <param name="shardId">The identifier for the current shard. Required for ShardKey and ShardChild types.</param>
        /// <param name="rdr">The data reader, set to the current result set.</param>
        /// <param name="logger">The logger instance to write any processing or debug information to.</param>
        /// <returns>A list of objects of the specified type, one for each result.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static List<TModel> ToList<TModel>(this DbDataReader rdr, short shardId, ILogger logger)
            where TModel : new() 
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
            var lazySqlRdrDelegate = _getRdrMapCache.GetOrAdd(tModel, new Lazy<(Delegate RowData, Delegate Ordinals)>(() => BuildReaderMapDelegate<TModel>(logger), LazyThreadSafetyMode.ExecutionAndPublication));
            if (lazySqlRdrDelegate.IsValueCreated)
            {
                LoggingExtensions.SqlReaderCacheHit(logger, tModel);
            }
            else
            {
                LoggingExtensions.SqlReadOutParametersCacheMiss(logger, tModel);
            }

            int[] ordinals = ((Func<DbDataReader, ILogger, int[]>)lazySqlRdrDelegate.Value.Ordinals)(rdr, logger);
            while (rdr.Read())
            {
                var item = ((Func<TModel, short, DbDataReader, int[], ILogger, TModel>)lazySqlRdrDelegate.Value.RowData)(new TModel(), shardId, rdr, ordinals, logger);
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
            var foundPrms = false;
            IterateInMapProperties(tModel, expressions, variables, prmSqlPrms, prmObjInstance, expIgnoreParameters, expLogger, noDupPrmNameList, ref foundPrms, logger);
            if (!foundPrms)
            {
                throw new NoMappingAttributesFoundException();
            }
            var inBlock = Expression.Block(variables, expressions);
			var lmbIn = Expression.Lambda<Action<DbParameterCollection, HashSet<string>, ILogger, TModel>>(inBlock, exprInPrms);
			logger?.CreatedExpressionTreeForSetInParameters(tModel, inBlock);
			return lmbIn.Compile();
		}

        private static void IterateInMapProperties(Type tModel, List<Expression> expressions, List<ParameterExpression> variables, ParameterExpression prmSqlPrms, Expression expModel, ParameterExpression expIgnoreParameters, ParameterExpression expLogger, HashSet<string> noDupPrmNameList, ref bool foundPrms, ILogger logger)
		{
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceInMapperProperty));
			foreach (var prop in tModel.GetProperties())
			{
				MemberExpression expProperty = Expression.Property(expModel, prop);
                Type propType = prop.PropertyType;



                var shdAttr = ExpressionHelpers.GetMapShardKeyAttribute(prop, propType, out var isNullable, out var isShardKey, out var isShardChild, out var isShardGrandChild, out var isShardGreatGrandChild);
                if (shdAttr is not null && (isShardKey || isShardChild || isShardGrandChild || isShardGrandChild))
                {
                    ExpressionHelpers.ShardKeyInMapProperties(prop, propType, shdAttr, isNullable, isShardKey, isShardChild, isShardGrandChild, isShardGreatGrandChild, expProperty, expressions, variables, prmSqlPrms, expIgnoreParameters, expLogger, noDupPrmNameList, miLogTrace, ref foundPrms, logger);
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
							throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
						}
						//MemberExpression expProperty = Expression.Property(expModel, prop);
						attrPM.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expProperty, prop.PropertyType, expLogger, logger);
                        foundPrms = true;
                    }
                }
				else if (prop.IsDefined(typeof(MapToModel)) && !prop.PropertyType.IsValueType)
				{
                    ExpressionHelpers.TryInstantiateMapToModel(prop, expProperty, expressions);
					IterateInMapProperties(prop.PropertyType, expressions, variables, prmSqlPrms, expProperty, expIgnoreParameters, expLogger, noDupPrmNameList, ref foundPrms, logger);
				}
			}
        }


        ////public static Func<ReadOnlyMemory<byte>, ParameterCollection, ILogger, IShardKey> BuildShardKeyQueryDelegate(Type tModel, ILogger logger)
        ////{

        ////    var expressions = new List<Expression>();
        ////    ParameterExpression expReadOnlyMemory = Expression.Parameter(typeof(ReadOnlyMemory<byte>), "key");
        ////    ParameterExpression expSqlPrms = Expression.Variable(typeof(DbParameterCollection), "parameters");
        ////    ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
        ////    ParameterExpression expIgnoreParameters = Expression.Parameter(typeof(HashSet<string>), "ignoreParameters");
        ////    var noDupPrmNameList = new HashSet<string>();
        ////    var variables = new List<ParameterExpression>();
        ////    var exprInPrms = new ParameterExpression[] { expReadOnlyMemory, expSqlPrms, expLogger };
        ////    var foundPrms = false;

        ////    PropertyInfo piAsSpan = typeof(ReadOnlyMemory<byte>).GetProperty("Span")!;
        ////    var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceInMapperProperty));
        ////    var expSpan = Expression.Property(expReadOnlyMemory, piAsSpan)!;
        ////    foreach (var prop in tModel.GetProperties())
        ////    {
        ////        //MemberExpression expProperty = Expression.Property(prmObjInstance, prop);
        ////        Type propType = prop.PropertyType;

        ////        var shdAttr = ExpressionHelpers.GetMapShardKeyAttribute(prop, propType, out var isNullable, out var isShardKey, out var isShardChild, out var isShardGrandChild, out var isShardGreatGrandChild);
        ////        if (shdAttr is not null && shdAttr.IsRecordIdentifier && !isNullable && (isShardKey || isShardChild || isShardGrandChild || isShardGrandChild))
        ////        {
        ////            var ctorInfo = prop.GetType().GetConstructor([typeof(ReadOnlySpan<byte>)])!;
        ////            var ctorPrm = ctorInfo.GetParameters()!.Single();
        ////            if (ctorPrm.Name == "data" && ctorPrm.ParameterType == typeof(ReadOnlySpan<byte>))
        ////            {
        ////                var expNewShardKey = Expression.New(ctorInfo, [expSpan]);
        ////                ExpressionHelpers.ShardKeyInMapProperties(prop, propType, shdAttr, isNullable, isShardKey, isShardChild, isShardGrandChild, isShardGreatGrandChild, expNewShardKey, expressions, variables, expSqlPrms, expIgnoreParameters, expLogger, noDupPrmNameList, miLogTrace, ref foundPrms, logger);
        ////                expressions.Add(expNewShardKey);
        ////                var newBlock = Expression.Block(typeof(IShardKey), exprInPrms, expressions);
        ////                var lmbNew = Expression.Lambda<Func<ReadOnlyMemory<byte>, ParameterCollection, ILogger, IShardKey>>(newBlock, [expReadOnlyMemory, expSqlPrms]);
        ////                //logger?.CreatedExpressionTreeForSetInParameters(tModel, newBlock);
        ////                return lmbNew.Compile();
        ////            }
        ////        }
        ////    }
        ////    throw new Exception("ShardKey not found.");
        ////}




        //private static bool ShardKeyInMapProperties(PropertyInfo prop, Type propType, MapShardKeyAttribute shdAttr, bool isNullable, bool isShardKey, bool isShardChild, bool isShardGrandChild, bool isShardGreatGrandChild, Expression expProperty, List<Expression> expressions, List<ParameterExpression> variables, ParameterExpression prmSqlPrms, ParameterExpression expIgnoreParameters, ParameterExpression expLogger, HashSet<string> noDupPrmNameList, MethodInfo miLogTrace, ref bool foundPrms, ILogger logger)
        //{
        //    Expression expOriginalProperty = expProperty;
        //    //var shdAttr = ExpressionHelpers.GetMapShardKeyAttribute(prop, propType, out var isNullable, out var isShardKey, out var isShardChild, out var isShardGrandChild, out var isShardGreatGrandChild);

        //    //if (shdAttr is null || !(isShardKey || isShardChild || isShardGrandChild || isShardGrandChild))
        //    //{
        //    //    return false;
        //    //}
        //    var foundRecordId = false;
        //    var foundChildId = false;
        //    var foundGrandChildId = false;
        //    var foundGreatGrandChildId = false;
        //    expressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name)));

        //    Expression expDetectNullOrEmpty;
        //    if (isNullable)
        //    {
        //        expProperty = Expression.Property(expProperty, propType.GetProperty(nameof(Nullable<int>.Value)));
        //        propType = Nullable.GetUnderlyingType(propType);
        //        expDetectNullOrEmpty = Expression.Property(expOriginalProperty, prop.PropertyType.GetProperty(nameof(Nullable<int>.HasValue)));
        //    }
        //    else
        //    {
        //        expDetectNullOrEmpty = Expression.NotEqual(expOriginalProperty, Expression.Property(null, propType.GetProperty(nameof(ShardKey<int>.Empty))));
        //    }


        //    if (shdAttr.ShardParameter is not null)
        //    {
        //        var expShardProperty = Expression.Property(expProperty, propType.GetProperty(nameof(ShardKey<int>.ShardId)));
        //        ParameterExpression expNullableShardId = Expression.Variable(typeof(Nullable<>).MakeGenericType(typeof(short)), prop.Name + "_NullableShardId");
        //        variables.Add(expNullableShardId);
        //        expressions.Add(Expression.IfThenElse(
        //            expDetectNullOrEmpty,
        //            Expression.Assign(expNullableShardId, Expression.Convert(expShardProperty, expNullableShardId.Type)),
        //            Expression.Assign(expNullableShardId, Expression.Constant(null, expNullableShardId.Type))
        //            ));
        //        shdAttr.ShardParameter.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expNullableShardId, expNullableShardId.Type, expLogger, logger);
        //    }

        //    var attrPMs = prop.GetCustomAttributes<ParameterMapAttributeBase>(true);

        //    foreach (var attrPM in attrPMs)
        //    {
        //        if (attrPM.Name == shdAttr.RecordIdName)
        //        {
        //            foundRecordId = true;
        //            var tDataRecordId = propType.GetGenericArguments()[0];
        //            if (!attrPM.IsValidType(tDataRecordId))
        //            {
        //                throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
        //            }
        //            var expRecordProperty = Expression.Property(expProperty, propType.GetProperty(nameof(ShardKey<int>.RecordId)));
        //            ParameterExpression expNullableRecordId;
        //            if (tDataRecordId.IsValueType)
        //            {
        //                expNullableRecordId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tDataRecordId), prop.Name + "_NullableRecordId");
        //            }
        //            else
        //            {
        //                expNullableRecordId = Expression.Variable(tDataRecordId, prop.Name + "_NullableRecordId");
        //            }
        //            variables.Add(expNullableRecordId);
        //            expressions.Add(Expression.IfThenElse(
        //                expDetectNullOrEmpty,
        //                Expression.Assign(expNullableRecordId, Expression.Convert(expRecordProperty, expNullableRecordId.Type)),
        //                Expression.Assign(expNullableRecordId, Expression.Constant(null, expNullableRecordId.Type))
        //                ));
        //            attrPM.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expNullableRecordId, expNullableRecordId.Type, expLogger, logger);
        //            foundPrms = true;
        //        }
        //        if ((isShardChild || isShardGrandChild || isShardGreatGrandChild) && attrPM.Name == shdAttr.ChildIdName)
        //        {
        //            foundChildId = true;

        //            var tDataId = propType.GetGenericArguments()[1];
        //            if (!attrPM.IsValidType(tDataId))
        //            {
        //                throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
        //            }
        //            var expChildProperty = Expression.Property(expProperty, propType.GetProperty(nameof(ShardKey<int, int>.ChildId)));
        //            ParameterExpression expNullableChildId;
        //            if (tDataId.IsValueType)
        //            {
        //                expNullableChildId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tDataId), prop.Name + "_NullableChildId");
        //            }
        //            else
        //            {
        //                expNullableChildId = Expression.Variable(tDataId, prop.Name + "_NullableChildId");
        //            }
        //            variables.Add(expNullableChildId);
        //            expressions.Add(Expression.IfThenElse(
        //                expDetectNullOrEmpty,
        //                Expression.Assign(expNullableChildId, Expression.Convert(expChildProperty, expNullableChildId.Type)),
        //                Expression.Assign(expNullableChildId, Expression.Constant(null, expNullableChildId.Type))
        //                ));
        //            attrPM.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expNullableChildId, expNullableChildId.Type, expLogger, logger);
        //            foundPrms = true;
        //        }
        //        if ((isShardGrandChild || isShardGreatGrandChild) && attrPM.Name == shdAttr.GrandChildIdName)
        //        {
        //            foundGrandChildId = true;
        //            var tDataId = propType.GetGenericArguments()[2];
        //            if (!attrPM.IsValidType(tDataId))
        //            {
        //                throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
        //            }
        //            var expChildProperty = Expression.Property(expProperty, propType.GetProperty(nameof(ShardKey<int, int, int>.GrandChildId)));
        //            ParameterExpression expNullableChildId;
        //            if (tDataId.IsValueType)
        //            {
        //                expNullableChildId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tDataId), prop.Name + "_NullableGrandChildId");
        //            }
        //            else
        //            {
        //                expNullableChildId = Expression.Variable(tDataId, prop.Name + "_NullableGrandChildId");
        //            }
        //            variables.Add(expNullableChildId);
        //            expressions.Add(Expression.IfThenElse(
        //                expDetectNullOrEmpty,
        //                Expression.Assign(expNullableChildId, Expression.Convert(expChildProperty, expNullableChildId.Type)),
        //                Expression.Assign(expNullableChildId, Expression.Constant(null, expNullableChildId.Type))
        //                ));
        //            attrPM.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expNullableChildId, expNullableChildId.Type, expLogger, logger);
        //            foundPrms = true;
        //        }
        //        if (isShardGreatGrandChild && attrPM.Name == shdAttr.GreatGrandChildIdName)
        //        {
        //            foundGreatGrandChildId = true;
        //            var tDataId = propType.GetGenericArguments()[3];
        //            if (!attrPM.IsValidType(tDataId))
        //            {
        //                throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
        //            }
        //            var expChildProperty = Expression.Property(expProperty, propType.GetProperty(nameof(ShardKey<int, int, int, int>.GreatGrandChildId)));
        //            ParameterExpression expNullableChildId;
        //            if (tDataId.IsValueType)
        //            {
        //                expNullableChildId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tDataId), prop.Name + "_NullableGreatGrandChildId");
        //            }
        //            else
        //            {
        //                expNullableChildId = Expression.Variable(tDataId, prop.Name + "_NullableGreatGrandChildId");
        //            }
        //            variables.Add(expNullableChildId);
        //            expressions.Add(Expression.IfThenElse(
        //                expDetectNullOrEmpty,
        //                Expression.Assign(expNullableChildId, Expression.Convert(expChildProperty, expNullableChildId.Type)),
        //                Expression.Assign(expNullableChildId, Expression.Constant(null, expNullableChildId.Type))
        //                ));
        //            attrPM.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expNullableChildId, expNullableChildId.Type, expLogger, logger);
        //            foundPrms = true;
        //        }
        //    }
        //    if (!foundRecordId)
        //    {
        //        throw new MapAttributeMissingException(MapAttributeMissingException.ShardElement.RecordId, shdAttr.RecordIdName);
        //    }
        //    if ((isShardChild || isShardGrandChild || isShardGreatGrandChild) && !foundChildId)
        //    {
        //        throw new MapAttributeMissingException(MapAttributeMissingException.ShardElement.ChildId, shdAttr.ChildIdName);
        //    }
        //    if ((isShardGrandChild || isShardGreatGrandChild) && !foundGrandChildId)
        //    {
        //        throw new MapAttributeMissingException(MapAttributeMissingException.ShardElement.GrandChildId, shdAttr.GrandChildIdName);
        //    }
        //    if (isShardGreatGrandChild && !foundGreatGrandChildId)
        //    {
        //        throw new MapAttributeMissingException(MapAttributeMissingException.ShardElement.GreatGrandChildId, shdAttr.GreatGrandChildIdName);
        //    }
        //    return true;
        //}


        private static Action<DbParameterCollection, HashSet<string>, ILogger> BuildOutSetDelegate(Type TModel, ILogger logger)
		{
			var expressions = new List<Expression>();

			//Create the two delegate parameters: in: sqlParametersCollection and model instance; out: sqlParametersCollection
			ParameterExpression prmSqlPrms = Expression.Variable(typeof(DbParameterCollection), "parameters");
			ParameterExpression expIgnoreParameters = Expression.Parameter(typeof(HashSet<string>), "ignoreParameters");
			ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var noDupPrmNameList = new HashSet<string>();

			var exprOutPrms = new ParameterExpression[] { prmSqlPrms, expIgnoreParameters, expLogger };

            bool foundPrms = false;
            IterateSetOutParameters(TModel, expressions, prmSqlPrms, expIgnoreParameters, expLogger, noDupPrmNameList, ref foundPrms, logger);
            if (!foundPrms)
            {
                throw new NoMappingAttributesFoundException();
            }

            var outBlock = Expression.Block(expressions);
			var lmbOut = Expression.Lambda<Action<DbParameterCollection, HashSet<string>, ILogger>>(outBlock, exprOutPrms);
			logger?.CreatedExpressionTreeForSetOutParameters(TModel, outBlock);
			return lmbOut.Compile();
		}
		private static void IterateSetOutParameters(Type TModel, List<Expression> expressions, ParameterExpression prmSqlPrms, ParameterExpression expIgnoreParameters, ParameterExpression expLogger, HashSet<string> noDupPrmNameList, ref bool foundPrms, ILogger logger)
		{
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceSetOutMapperProperty));

            //Loop through all object properties:
            foreach (var prop in TModel.GetProperties())
			{
                Type propType = prop.PropertyType;
                var shdAttr = ExpressionHelpers.GetMapShardKeyAttribute(prop, propType, out var isNullable, out var isShardKey, out var isShardChild, out var isShardGrandChild, out var isShardGreatGrandChild);
                propType = null;

                if (!(shdAttr is null) && (isShardKey || isShardChild || isShardGrandChild || isShardGrandChild))
                {
                    var foundRecordId = false;
                    var foundChildId = false;
                    var foundGrandChildId = false;
                    var foundGreatGrandChildId = false;
                    expressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name)));

                    if (!(shdAttr.ShardParameter is null))
                    {
                        shdAttr.ShardParameter.AppendSetOutParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expLogger, logger);
                    }

                    var attrPMs = prop.GetCustomAttributes<ParameterMapAttributeBase>(true);
                    foreach (var attrPM in attrPMs)
                    {
                        if (attrPM.Name == shdAttr.RecordIdName)
                        {
                            foundRecordId = true;
                            if (propType is null)
                            {
                                propType = prop.PropertyType;
                                if (isNullable)
                                    {
                                    propType = Nullable.GetUnderlyingType(propType);
                                }
                            }
                            if (!attrPM.IsValidType(propType.GetGenericArguments()[0]))
                            {
                                throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
                            }
                            attrPM.AppendSetOutParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expLogger, logger);
                            foundPrms = true;
                        }
                        if ((isShardChild || isShardGrandChild || isShardGreatGrandChild) && attrPM.Name == shdAttr.ChildIdName)
                        {
                            foundChildId = true;
                            if (propType is null)
                            {
                                propType = prop.PropertyType;
                                if (isNullable)
                                    {
                                    propType = Nullable.GetUnderlyingType(propType);
                                }
                            }
                            if (!attrPM.IsValidType(propType.GetGenericArguments()[1]))
                            {
                                throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
                            }
                            attrPM.AppendSetOutParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expLogger, logger);
                            foundPrms = true;
                        }
                        if ((isShardGrandChild || isShardGreatGrandChild) && attrPM.Name == shdAttr.GrandChildIdName)
                        {
                            foundGrandChildId = true;
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
                                throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
                            }
                            attrPM.AppendSetOutParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expLogger, logger);
                            foundPrms = true;
                        }
                        if (isShardGreatGrandChild && attrPM.Name == shdAttr.GreatGrandChildIdName)
                        {
                            foundGreatGrandChildId = true;
                            if (propType is null)
                            {
                                propType = prop.PropertyType;
                                if (isNullable)
                                {
                                    propType = Nullable.GetUnderlyingType(propType);
                                }
                            }
                            if (!attrPM.IsValidType(propType.GetGenericArguments()[3]))
                            {
                                throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
                            }
                            attrPM.AppendSetOutParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expLogger, logger);
                            foundPrms = true;
                        }
                    }
                    if (!foundRecordId)
                    {
                        throw new MapAttributeMissingException(MapAttributeMissingException.ShardElement.RecordId, shdAttr.RecordIdName);
                    }
                    if ((isShardChild || isShardGrandChild || isShardGreatGrandChild) && !foundChildId)
                    {
                        throw new MapAttributeMissingException(MapAttributeMissingException.ShardElement.ChildId, shdAttr.ChildIdName);
                    }
                    if ((isShardGrandChild || isShardGreatGrandChild) && !foundGrandChildId)
                    {
                        throw new MapAttributeMissingException(MapAttributeMissingException.ShardElement.GrandChildId, shdAttr.GrandChildIdName);
                    }
                    if (isShardGreatGrandChild && !foundGreatGrandChildId)
                    {
                        throw new MapAttributeMissingException(MapAttributeMissingException.ShardElement.GreatGrandChildId, shdAttr.GreatGrandChildIdName);
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
                            if (isNullable)
                                    {
                                propType = Nullable.GetUnderlyingType(propType);
                            }
                        }
                        if (!attrPM.IsValidType(propType))
						{
							throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
						}
						attrPM.AppendSetOutParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expLogger, logger);
                        foundPrms = true;
                    }
                }
				else if (prop.IsDefined(typeof(MapToModel)) && !prop.PropertyType.IsValueType)
				{
                    IterateSetOutParameters(prop.PropertyType, expressions, prmSqlPrms, expIgnoreParameters, expLogger, noDupPrmNameList, ref foundPrms, logger);
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
                    throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
                }
                attrPM.AppendSetOutParameterExpressions(expressions, expSprocParameters, expIgnoreParameters, noDupPrmNameList, expLogger, logger);
            }
        }

        private static Func<TModel, short, DbParameterCollection, ILogger, TModel> BuildOutGetDelegate<TModel>(DbParameterCollection parameters, Type tModel, ILogger logger) 
		{
            ParameterExpression expInstanceArgument = Expression.Variable(tModel, "instance");
            ParameterExpression expShardArgument = Expression.Variable(typeof(short), "shardId");
			ParameterExpression expSprocParameters = Expression.Parameter(typeof(DbParameterCollection), "parameters");
			ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var variableExpressions = new List<ParameterExpression>();
            //	var expModel = Expression.Variable(tModel, "model"); //result variable
            //variableExpressions.Add(expInstanceArgument);
            var expPrm = Expression.Variable(typeof(DbParameter), "prm");
			variableExpressions.Add(expPrm);

			var expressionPrms = new ParameterExpression[] { expInstanceArgument, expShardArgument, expSprocParameters, expLogger };
			var expExitLabel = Expression.Label(tModel);

            var initialExpressions = new List<Expression>();
			var subsequentExpressions = new List<Expression>();
            var foundPrms = false;
            IterateGetOutParameters(tModel, expShardArgument, expSprocParameters, expPrm, variableExpressions, initialExpressions, subsequentExpressions, expInstanceArgument, expLogger, expExitLabel, ref foundPrms, logger);
            if (!foundPrms)
            {
                throw new NoMappingAttributesFoundException();
            }

			subsequentExpressions.Add(Expression.Goto(expExitLabel, expInstanceArgument)); //return value;

			initialExpressions.AddRange(subsequentExpressions);
            initialExpressions.Add(Expression.Label(expExitLabel, Expression.Default(tModel))); // Expression.Constant(null, tModel)));
			var expBlock = Expression.Block(variableExpressions, initialExpressions);
			var lambda = Expression.Lambda<Func<TModel, short, DbParameterCollection, ILogger, TModel>>(expBlock, expressionPrms);
			logger?.CreatedExpressionTreeForReadOutParameters(tModel, expBlock);
			return lambda.Compile();
		}
		private static void IterateGetOutParameters(Type tModel, ParameterExpression expShardArgument, ParameterExpression expSprocParameters, ParameterExpression expPrm, List<ParameterExpression> variableExpressions, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions, Expression expModel, ParameterExpression expLogger, LabelTarget exitLabel, ref bool foundPrms, ILogger logger)
		{

            foreach (var prop in tModel.GetProperties())
			{
                ExpressionHelpers.GetMapShardKeyAttribute(prop, prop.PropertyType, out var isNullable, out var isShardKey, out var isShardChild, out var isShardGrandChild, out var isShardGreatGrandChild);
                if (isShardKey || isShardChild || isShardGrandChild || isShardGreatGrandChild)
                {
                    int notUsed = 0;
                    HandleShardKeyChild(true, prop, tModel, expShardArgument, expSprocParameters, expPrm, null, null, null, null, ref notUsed, variableExpressions, requiredExpressions, nonrequiredExpressions, expModel, expLogger, exitLabel, logger);
                    foundPrms = true;
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
                        foundPrms = true;
                    }
                }
				else if (prop.IsDefined(typeof(MapToModel)) && !prop.PropertyType.IsValueType)
				{
					MemberExpression expProperty = Expression.Property(expModel, prop);
                    ExpressionHelpers.TryInstantiateMapToModel(prop, expProperty, requiredExpressions);
                    IterateGetOutParameters(prop.PropertyType, expShardArgument, expSprocParameters, expPrm, variableExpressions, requiredExpressions, nonrequiredExpressions, expProperty, expLogger, exitLabel, ref foundPrms, logger);
				}
			}
        }

        private static void HandleOutProperty(ParameterMapAttributeBase attrPM, PropertyInfo prop, Type tModel, ParameterExpression expSprocParameters, ParameterExpression expPrm, List<ParameterExpression> variableExpressions, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions, Expression expModel, ParameterExpression expLogger, LabelTarget exitLabel, ILogger logger)
		{
			var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceGetOutMapperProperty));
			var expCallLog = Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name));

			if (!attrPM.IsValidType(prop.PropertyType))
			{
				throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
			}

			MemberExpression expProperty = Expression.Property(expModel, prop);

			var miGetParameter = typeof(ExpressionHelpers).GetMethod(nameof(ExpressionHelpers.GetParameter), BindingFlags.Static | BindingFlags.NonPublic);
			var expAssign = Expression.Assign(expPrm, Expression.Call(miGetParameter, expSprocParameters, Expression.Constant(attrPM.ParameterName, typeof(string))));

			if (attrPM.IsRecordIdentifier)
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
				throw new InvalidMapTypeException(var.Name, var.Type, attrPM.SqlType, attrPM.SqlTypeName);
			}

			var miGetParameter = typeof(ExpressionHelpers).GetMethod(nameof(ExpressionHelpers.GetParameter), BindingFlags.Static | BindingFlags.NonPublic);
			var expAssign = Expression.Assign(expPrm, Expression.Call(miGetParameter, expSprocParameters, Expression.Constant(attrPM.ParameterName, typeof(string))));
			if (attrPM.IsRecordIdentifier)
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

        private static void HandleShardKeyChild(bool isOutPrms, PropertyInfo prop, Type tModel, ParameterExpression expShardArgument,
            ParameterExpression expSprocParameters, ParameterExpression expPrm,
            ParameterExpression expRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, List<MethodCallExpression> columnLookupExpressions, ref int propIndex,
            List<ParameterExpression> variableExpressions, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions,
            Expression expModel, ParameterExpression expLogger, LabelTarget exitLabel, ILogger logger)
        {

            var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceGetOutMapperProperty));

            MemberExpression expShardProperty = Expression.Property(expModel, prop);
            var propType = prop.PropertyType;
            var shdAttr = ExpressionHelpers.GetMapShardKeyAttribute(prop, propType, out var isNullable, out var isShardKey, out var isShardChild, out var isShardGrandChild, out var isShardGreatGrandChild);

            if (isNullable)
            {
                propType = Nullable.GetUnderlyingType(propType);
            }
            var foundRecordId = false;
            var foundChildId = false;
            var foundGrandChildId = false;
            var foundGreatGrandChildId = false;

            if (!(shdAttr is null) && (isShardKey || isShardChild || isShardGrandChild || isShardGrandChild))
            {
                var tRecordId = propType.GetProperty(nameof(ShardKey<int>.RecordId)).PropertyType;
                Type tChildId = null;
                Type tGrandChildId = null;
                Type tGreatGrandChildId = null;
                if (isShardChild || isShardGrandChild || isShardGreatGrandChild)
				{
					tChildId = propType.GetProperty(nameof(ShardKey<int, int>.ChildId)).PropertyType;
				}
                if (isShardGrandChild || isShardGreatGrandChild)
                {
                    tGrandChildId = propType.GetProperty(nameof(ShardKey<int, int, int>.GrandChildId)).PropertyType;
                }
                if (isShardGreatGrandChild)
                {
                    tGreatGrandChildId = propType.GetProperty(nameof(ShardKey<int, int, int, int>.GreatGrandChildId)).PropertyType;
                }

                ParameterExpression expDataShardId;
				ParameterExpression expDataRecordId;
				ParameterExpression expDataChildId = null;
                ParameterExpression expDataGrandChildId = null;
                ParameterExpression expDataGreatGrandChildId = null;

                expDataShardId = Expression.Variable(typeof(Nullable<>).MakeGenericType(typeof(short)), prop.Name + "_ShardId");
				variableExpressions.Add(expDataShardId);

				if (tRecordId.IsValueType)
				{
					expDataRecordId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tRecordId), prop.Name + "_RecordId");
				}
				else
				{
					expDataRecordId = Expression.Variable(tRecordId, prop.Name + "_RecordId");
				}
				variableExpressions.Add(expDataRecordId);

                if (isShardChild || isShardGrandChild || isShardGreatGrandChild)
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
                if (isShardGrandChild || isShardGreatGrandChild)
                {
                    if (tGrandChildId.IsValueType)
                    {
                        expDataGrandChildId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tGrandChildId), prop.Name + "_GrandChildId");
                    }
                    else
                    {
                        expDataGrandChildId = Expression.Variable(tGrandChildId, prop.Name + "_GrandChildId");
                    }
                    variableExpressions.Add(expDataGrandChildId);
                }
                if (isShardGreatGrandChild)
                {
                    if (tGreatGrandChildId.IsValueType)
                    {
                        expDataGreatGrandChildId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tGreatGrandChildId), prop.Name + "_GreatGrandChildId");
                    }
                    else
                    {
                        expDataGreatGrandChildId = Expression.Variable(tGreatGrandChildId, prop.Name + "_GreatGrandChildId");
                    }
                    variableExpressions.Add(expDataGreatGrandChildId);
                }

                ConstantExpression expDataOrigin;

                expDataOrigin = Expression.Constant(shdAttr.Origin, typeof(char));

                if (!(shdAttr.ShardParameter is null))
                {
                    nonrequiredExpressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(expDataShardId.Name)));
                    if (isOutPrms)
                    {
                        HandleOutVariable(shdAttr.ShardParameter, expDataShardId, tModel, expSprocParameters, expPrm, requiredExpressions, nonrequiredExpressions, expLogger, exitLabel, logger);
                    }
                    else
                    {
                        HandleRdrVariable(shdAttr.ShardParameter, expDataShardId, tModel, expRdr, expOrdinals, expOrdinal, columnLookupExpressions, variableExpressions, requiredExpressions, nonrequiredExpressions, expModel, expLogger, ref propIndex, exitLabel, logger);
                    }

                }
                else
                {
                    nonrequiredExpressions.Add(Expression.Assign(expDataShardId, Expression.Convert(expShardArgument, expDataShardId.Type)));
                }

                var attrPMs = prop.GetCustomAttributes<ParameterMapAttributeBase>(true);
				//RecordId
				foreach (var attrPM in attrPMs)
				{
					if (attrPM.Name == shdAttr.RecordIdName)
					{
                        foundRecordId = true;
						if (attrPM.IsRecordIdentifier)
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
					}
                    if ((isShardChild || isShardGrandChild || isShardGreatGrandChild) && attrPM.Name == shdAttr.ChildIdName)
                    {
                        foundChildId = true;
                        if (attrPM.IsRecordIdentifier)
                        {
                            requiredExpressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(expDataChildId.Name)));
                        }
                        else
                        {
                            nonrequiredExpressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(expDataChildId.Name)));
                        }
                        if (isOutPrms)
                        {
                            HandleOutVariable(attrPM, expDataChildId, tModel, expSprocParameters, expPrm, requiredExpressions, nonrequiredExpressions, expLogger, exitLabel, logger);
                        }
                        else
                        {
                            HandleRdrVariable(attrPM, expDataChildId, tModel, expRdr, expOrdinals, expOrdinal, columnLookupExpressions, variableExpressions, requiredExpressions, nonrequiredExpressions, expModel, expLogger, ref propIndex, exitLabel, logger);
                        }
                    }
                    if ((isShardGrandChild || isShardGreatGrandChild) && attrPM.Name == shdAttr.GrandChildIdName)
                    {
                        foundGrandChildId = true;
                        if (attrPM.IsRecordIdentifier)
                        {
                            requiredExpressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(expDataGrandChildId.Name)));
                        }
                        else
                        {
                            nonrequiredExpressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(expDataGrandChildId.Name)));
                        }
                        if (isOutPrms)
                        {
                            HandleOutVariable(attrPM, expDataGrandChildId, tModel, expSprocParameters, expPrm, requiredExpressions, nonrequiredExpressions, expLogger, exitLabel, logger);
                        }
                        else
                        {
                            HandleRdrVariable(attrPM, expDataGrandChildId, tModel, expRdr, expOrdinals, expOrdinal, columnLookupExpressions, variableExpressions, requiredExpressions, nonrequiredExpressions, expModel, expLogger, ref propIndex, exitLabel, logger);
                        }
                    }
                    if (isShardGreatGrandChild && attrPM.Name == shdAttr.GreatGrandChildIdName)
                    {
                        foundGreatGrandChildId = true;
                        if (attrPM.IsRecordIdentifier)
                        {
                            requiredExpressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(expDataGreatGrandChildId.Name)));
                        }
                        else
                        {
                            nonrequiredExpressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(expDataGreatGrandChildId.Name)));
                        }
                        if (isOutPrms)
                        {
                            HandleOutVariable(attrPM, expDataGreatGrandChildId, tModel, expSprocParameters, expPrm, requiredExpressions, nonrequiredExpressions, expLogger, exitLabel, logger);
                        }
                        else
                        {
                            HandleRdrVariable(attrPM, expDataGreatGrandChildId, tModel, expRdr, expOrdinals, expOrdinal, columnLookupExpressions, variableExpressions, requiredExpressions, nonrequiredExpressions, expModel, expLogger, ref propIndex, exitLabel, logger);
                        }
                    }
                }

                if (!foundRecordId)
                {
                    throw new MapAttributeMissingException(MapAttributeMissingException.ShardElement.RecordId, shdAttr.RecordIdName);
                }
                if ((isShardChild || isShardGrandChild || isShardGreatGrandChild) && !foundChildId)
                {
                    throw new MapAttributeMissingException(MapAttributeMissingException.ShardElement.ChildId, shdAttr.ChildIdName);
                }
                if ((isShardGrandChild || isShardGreatGrandChild) && !foundGrandChildId)
                {
                    throw new MapAttributeMissingException(MapAttributeMissingException.ShardElement.GrandChildId, shdAttr.GrandChildIdName);
                }
                if (isShardGreatGrandChild && !foundGreatGrandChildId)
                {
                    throw new MapAttributeMissingException(MapAttributeMissingException.ShardElement.GreatGrandChildId, shdAttr.GreatGrandChildIdName);
                }

                var expHasNoNulls = Expression.AndAlso(Expression.NotEqual(expDataShardId, Expression.Constant(null, expDataShardId.Type)),
						Expression.NotEqual(expDataRecordId, Expression.Constant(null, expDataRecordId.Type)));
                if (isShardChild || isShardGrandChild || isShardGreatGrandChild)
                {
                    expHasNoNulls = Expression.AndAlso(expHasNoNulls,
						Expression.NotEqual(expDataChildId, Expression.Constant(null, expDataChildId.Type)));
				}
                if (isShardGrandChild || isShardGreatGrandChild)
                {
                    expHasNoNulls = Expression.AndAlso(expHasNoNulls,
                        Expression.NotEqual(expDataGrandChildId, Expression.Constant(null, expDataGrandChildId.Type)));
                }
                if (isShardGreatGrandChild)
                {
                    expHasNoNulls = Expression.AndAlso(expHasNoNulls,
                        Expression.NotEqual(expDataGreatGrandChildId, Expression.Constant(null, expDataGreatGrandChildId.Type)));
                }

                Type[] constructorTypes;
				Expression[] constructorArgs;
                //Type tShard;
                if (isShardGreatGrandChild)
                {
                    constructorTypes = new[] { typeof(char), typeof(short), tRecordId, tChildId, tGrandChildId, tGreatGrandChildId };
                    constructorArgs = new Expression[6];
                }
                else if (isShardGrandChild)
                {
                    constructorTypes = new[] { typeof(char), typeof(short), tRecordId, tChildId, tGrandChildId };
                    constructorArgs = new Expression[5];
                }
                if (isShardChild)
                {
                    constructorTypes = new[] { typeof(char), typeof(short), tRecordId, tChildId };
                    constructorArgs = new Expression[4];
                }
                else
                {
                    constructorTypes = new[] { typeof(char), typeof(short), tRecordId };
                    constructorArgs = new Expression[3];
                }
				constructorArgs[0] = expDataOrigin;
				constructorArgs[1] = Expression.Property(expDataShardId, expDataShardId.Type.GetProperty(nameof(Nullable<int>.Value)));

                if (tRecordId.IsValueType)
				{
					constructorArgs[2] = Expression.Property(expDataRecordId, expDataRecordId.Type.GetProperty(nameof(Nullable<int>.Value)));
				}
				else
				{
					constructorArgs[2] = expDataRecordId;
				}
                if (isShardChild || isShardGrandChild || isShardGreatGrandChild)
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
                if (isShardGrandChild || isShardGreatGrandChild)
                {
                    if (tGrandChildId.IsValueType)
                    {
                        constructorArgs[4] = Expression.Property(expDataGrandChildId, expDataGrandChildId.Type.GetProperty(nameof(Nullable<int>.Value)));
                    }
                    else
                    {
                        constructorArgs[4] = expDataGrandChildId;
                    }
                }
                if (isShardGreatGrandChild)
                {
                    if (tGreatGrandChildId.IsValueType)
                    {
                        constructorArgs[5] = Expression.Property(expDataGreatGrandChildId, expDataGreatGrandChildId.Type.GetProperty(nameof(Nullable<int>.Value)));
                    }
                    else
                    {
                        constructorArgs[5] = expDataGreatGrandChildId;
                    }
                }
                var expNewShardInstance = Expression.New(propType.GetConstructor(constructorTypes), constructorArgs);
				Expression expActionIfNull;
				if (isNullable)
				{
					expActionIfNull = Expression.Assign(expShardProperty, Expression.Constant(null, expShardProperty.Type));
					expNewShardInstance = Expression.New(typeof(Nullable<>).MakeGenericType(propType).GetConstructor(new Type[] { propType }), new Expression[] { expNewShardInstance });
				}
				else
				{
					expActionIfNull = Expression.Assign(expShardProperty, Expression.Property(null, propType.GetProperty(nameof(ShardKey<int>.Empty))));
				}
				nonrequiredExpressions.Add(Expression.IfThenElse(
					expHasNoNulls,
					Expression.Assign(expShardProperty, expNewShardInstance),
					expActionIfNull
					));
			}
		}


		//private static (Func<object, short, DbDataReader, int[], ILogger, TModel> DataRow, Func<DbDataReader, ILogger, int[]> Ordinals) BuildReaderMapDelegate<TModel>(ILogger logger)
        private static (Delegate DataRow, Func<DbDataReader, ILogger, int[]> Ordinals) BuildReaderMapDelegate<TModel>(ILogger logger)
        {
            var tModel = typeof(TModel);
            ParameterExpression expInstanceArgument = Expression.Parameter(tModel, "instance");
            ParameterExpression expShardArgument = Expression.Variable(typeof(short), "shardId");
			ParameterExpression prmSqlRdr = Expression.Parameter(typeof(DbDataReader), "rdr"); //input param
            ParameterExpression expLogger = Expression.Parameter(typeof(ILogger), "logger");
			var variableExpressions = new List<ParameterExpression>();
			//var expModel = Expression.Variable(tModel, "model"); //model variable
//			variableExpressions.Add(expInstanceArgument);
			var expOrdinal = Expression.Variable(typeof(int), "ordinal");
			variableExpressions.Add(expOrdinal);
			var expOrdinalsArg = Expression.Parameter(typeof(int[]), "ordinals");
            //variableExpressions.Add(expOrdinalArg);
            //variableExpressions.Add(expLogger);

			var initialExpressions = new List<Expression>();
			var columnLookupExpressions = new List<MethodCallExpression>();
			var expressionPrms = new ParameterExpression[] { expInstanceArgument, expShardArgument, prmSqlRdr, expOrdinalsArg, expLogger };

            var expExitLabel = Expression.Label(tModel);

            var propIndex = 0;

			//var expAssign = Expression.Assign(expModel, Expression.New(tModel));
			//initialExpressions.Add(expAssign);
            var subsequentExpressions = new List<Expression>();

            //Loop through all object properties:
            IterateRdrColumns(tModel, expInstanceArgument, expShardArgument, columnLookupExpressions, variableExpressions, initialExpressions, subsequentExpressions, prmSqlRdr, expOrdinalsArg, expOrdinal, ref propIndex, expLogger, expExitLabel, logger);
            if (propIndex == 0)
            {
                throw new NoMappingAttributesFoundException();
            }
            subsequentExpressions.Add(Expression.Goto(expExitLabel, expInstanceArgument)); //return value;

            var resultExpressions = new List<Expression>();
            //resultExpressions.Add(Expression.Assign(expOrdinalsArg, Expression.NewArrayInit(typeof(int), columnLookupExpressions.ToArray())));
            resultExpressions.AddRange(initialExpressions);
            resultExpressions.AddRange(subsequentExpressions);
            resultExpressions.Add(Expression.Label(expExitLabel, Expression.Default(tModel)));

            var expDataBlock = Expression.Block(tModel, variableExpressions, resultExpressions);
            var lambdaDataRow = Expression.Lambda<Func<TModel, short, DbDataReader, int[], ILogger, TModel>>(expDataBlock, expressionPrms);

            var expOrdinalArray = Expression.NewArrayInit(typeof(int), columnLookupExpressions.ToArray());
            //var lambdaOrdinals = Expression.Lambda<Func<DbDataReader, int[]>>(expOrdinalArray, new ParameterExpression[] { prmSqlRdr, expOrdinalsArg });
            var lambdaOrdinals = Expression.Lambda<Func<DbDataReader, ILogger, int[]>>(expOrdinalArray, new ParameterExpression[] { prmSqlRdr, expLogger });
            logger?.CreatedExpressionTreeForReaderOrdinals(tModel, expOrdinalArray);
            logger?.CreatedExpressionTreeForReaderRowData(tModel, expDataBlock);

            return (lambdaDataRow.Compile(), lambdaOrdinals.Compile());
		}
		private static void IterateRdrColumns(Type tModel, Expression expModel, ParameterExpression expShardArgument, List<MethodCallExpression> columnLookupExpressions, List<ParameterExpression> variableExpressions, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions, ParameterExpression expRdr, ParameterExpression expOrdinalsArg, ParameterExpression expOrdinal, ref int propIndex, ParameterExpression expLogger, LabelTarget exitLabel, ILogger logger)
		{
			foreach (var prop in tModel.GetProperties())
			{
                ExpressionHelpers.GetMapShardKeyAttribute(prop, prop.PropertyType, out var isNullable, out var isShardKey, out var isShardChild, out var isShardGrandChild, out var isShardGreatGrandChild);
                if (isShardKey || isShardChild || isShardGrandChild || isShardGreatGrandChild)
                {
                    HandleShardKeyChild(false, prop, tModel, expShardArgument, null, null, expRdr, expOrdinalsArg, expOrdinal, columnLookupExpressions, ref propIndex, variableExpressions, requiredExpressions, nonrequiredExpressions, expModel, expLogger, exitLabel, logger);
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
                    IterateRdrColumns(prop.PropertyType, expProperty, expShardArgument, columnLookupExpressions, variableExpressions, requiredExpressions, nonrequiredExpressions, expRdr, expOrdinalsArg, expOrdinal, ref propIndex, expLogger, exitLabel, logger);
				}
			}

		}
        private static void HandleRdrColumn(ParameterMapAttributeBase attrPM, PropertyInfo prop, Type tModel, ParameterExpression expRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, List<MethodCallExpression> columnLookupExpressions, List<ParameterExpression> variableExpressions, List<Expression> requiredExpressions, List<Expression> nonrequiredExpressions, Expression expModel, ParameterExpression expLogger, ref int propIndex, LabelTarget exitLabel, ILogger logger)
        {
            var miLogTrace = typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.TraceRdrMapperProperty));
            var expCallLog = Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name));

            if (!attrPM.IsValidType(prop.PropertyType))
            {
                throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
            }

            MemberExpression expProperty = Expression.Property(expModel, prop);

            if (attrPM.IsRecordIdentifier)
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
                throw new InvalidMapTypeException(var.Name, var.Type, attrPM.SqlType, attrPM.SqlTypeName);
            }
            if (attrPM.IsRecordIdentifier)
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
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">The optional data parameter is not used but is required by the delegate’s method signature.</param>
        /// <param name="rdr">The DbDataReader containing tables and rows.</param>
        /// <param name="parameters">Not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>A list of TModel objects, one for each record returned by the DataReader.</returns>
        public static List<TModel> ListFromReaderResultsHandler<TModel>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            => Mapper.ToList<TModel>(rdr, shardId, logger);

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

        #region Handle Complex Models with both DataReader and Output Parameters
        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of the instance value.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">Not used, but required for method signature.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger) where TModel : new()
        {
            return ModelFromOutResultsHandler<TModel>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters.
        /// </summary>
        /// <typeparam name="TModel">The type of the instance value.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">Not used, but required for method signature.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
        {
            MoveRdrToEnd(rdr);
            return parameters.SetModel<TModel>(instance, shardId, logger);
        }


        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult">A type with attributes that correspond to the data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with one result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new() where TReaderResult : new()
        {
            return ModelFromOutResultsHandler<TModel, TReaderResult>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult">A type with attributes that correspond to the data reader result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with one result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            var resultList = (List<TReaderResult>)Mapper.ToList<TReaderResult>(rdr, shardId, logger);
            MoveRdrToEnd(rdr);
            var resultOutPrms = Mapper.SetModel<TModel>(parameters, instance, shardId, logger);
            if (resultOutPrms is null)
            {
                logger?.EmptyResult(sprocName);
                return default(TModel);
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(instance, shardId, sprocName, 3, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult>, TModel, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList, resultOutPrms, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with two result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            where TReaderResult0 : new()
            where TReaderResult1 : new()
        {
            return ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }


        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with two result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult0 : new()
            where TReaderResult1 : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            MoveRdrToEnd(rdr);
            var resultOutPrms = Mapper.SetModel<TModel>(parameters, instance, shardId, logger);
            if (resultOutPrms is null)
            {
                logger?.EmptyResult(sprocName);
                return default(TModel);
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult0, TReaderResult1, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(instance, shardId, sprocName, 7, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, TModel, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList0, resultList1, resultOutPrms, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with three result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
        {
            return ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with three result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            MoveRdrToEnd(rdr);
            var resultOutPrms = Mapper.SetModel<TModel>(parameters, instance, shardId, logger);
            if (resultOutPrms is null)
            {
                logger?.EmptyResult(sprocName);
                return default(TModel);
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult0, TReaderResult1, TReaderResult2, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(instance, shardId, sprocName, 15, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, TModel, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList0, resultList1, resultList2, resultOutPrms, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with four result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
        {
            return ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with four result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            MoveRdrToEnd(rdr);
            var resultOutPrms = Mapper.SetModel<TModel>(parameters, instance, shardId, logger);
            if (resultOutPrms is null)
            {
                logger?.EmptyResult(sprocName);
                return default(TModel);
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(instance, shardId, sprocName, 31, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, TModel, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultOutPrms, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to the fifth data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with five result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
        {
            return ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to the fifth data reader result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with five result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            MoveRdrToEnd(rdr);
            var resultOutPrms = Mapper.SetModel<TModel>(parameters, instance, shardId, logger);
            if (resultOutPrms is null)
            {
                logger?.EmptyResult(sprocName);
                return default(TModel);
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(instance, shardId, sprocName, 63, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, TModel, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultOutPrms, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to the fifth data reader result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to the sixth data reader result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with six result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
            where TReaderResult5 : new()
        {
            return ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to the fifth data reader result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to the sixth data reader result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with six result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
            where TReaderResult5 : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;
            List<TReaderResult5> resultList5;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            if (rdr.NextResult())
            {
                resultList5 = (List<TReaderResult5>)Mapper.ToList<TReaderResult5>(rdr, shardId, logger);
            }
            else
            {
                resultList5 = new List<TReaderResult5>();
            }
            MoveRdrToEnd(rdr);
            var resultOutPrms = Mapper.SetModel<TModel>(parameters, instance, shardId, logger);
            if (resultOutPrms is null)
            {
                logger?.EmptyResult(sprocName);
                return default(TModel);
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, Mapper.DummyType, Mapper.DummyType, TModel>(instance, shardId, sprocName, 127, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, TModel, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultOutPrms, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
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
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with seven result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
            where TReaderResult5 : new()
            where TReaderResult6 : new()
        {
            return ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to the fifth data reader result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to the sixth data reader result.</typeparam>
        /// <typeparam name="TReaderResult6">A type with attributes that correspond to the seventh data reader result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with seven result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
            where TReaderResult5 : new()
            where TReaderResult6 : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;
            List<TReaderResult5> resultList5;
            List<TReaderResult6> resultList6;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            if (rdr.NextResult())
            {
                resultList5 = (List<TReaderResult5>)Mapper.ToList<TReaderResult5>(rdr, shardId, logger);
            }
            else
            {
                resultList5 = new List<TReaderResult5>();
            }
            if (rdr.NextResult())
            {
                resultList6 = (List<TReaderResult6>)Mapper.ToList<TReaderResult6>(rdr, shardId, logger);
            }
            else
            {
                resultList6 = new List<TReaderResult6>();
            }
            MoveRdrToEnd(rdr);
            var resultOutPrms = Mapper.SetModel<TModel>(parameters, instance, shardId, logger);
            if (resultOutPrms is null)
            {
                logger?.EmptyResult(sprocName);
                return default(TModel);
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, Mapper.DummyType, TModel>(instance, shardId, sprocName, 255, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, TModel, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultList6, resultOutPrms, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
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
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with eight result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
            where TReaderResult5 : new()
            where TReaderResult6 : new()
            where TReaderResult7 : new()
        {
            return ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }


        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using output parameters and data reader results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to the first data reader result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to the second data reader result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to the third data reader result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to the forth data reader result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to the fifth data reader result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to the sixth data reader result.</typeparam>
        /// <typeparam name="TReaderResult6">A type with attributes that correspond to the seventh data reader result.</typeparam>
        /// <typeparam name="TReaderResult7">A type with attributes that correspond to the eighth data reader result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with eight result sets for list properties.</param>
        /// <param name="parameters">The output parameter set which correspond to the attributes of TModel.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        public static TModel ModelFromOutResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
            where TReaderResult5 : new()
            where TReaderResult6 : new()
            where TReaderResult7 : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;
            List<TReaderResult5> resultList5;
            List<TReaderResult6> resultList6;
            List<TReaderResult7> resultList7;

            var resultList0 = (List<TReaderResult0>)Mapper.ToList<TReaderResult0>(rdr, shardId, logger);
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            if (rdr.NextResult())
            {
                resultList5 = (List<TReaderResult5>)Mapper.ToList<TReaderResult5>(rdr, shardId, logger);
            }
            else
            {
                resultList5 = new List<TReaderResult5>();
            }
            if (rdr.NextResult())
            {
                resultList6 = (List<TReaderResult6>)Mapper.ToList<TReaderResult6>(rdr, shardId, logger);
            }
            else
            {
                resultList6 = new List<TReaderResult6>();
            }
            if (rdr.NextResult())
            {
                resultList7 = (List<TReaderResult7>)Mapper.ToList<TReaderResult7>(rdr, shardId, logger);
            }
            else
            {
                resultList7 = new List<TReaderResult7>();
            }
            MoveRdrToEnd(rdr);
            parameters.SetModel<TModel>(instance, shardId, logger);
            if (instance == null)
            {
                logger?.EmptyResult(sprocName);
                return default(TModel);
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getOutObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TModel>(instance, shardId, sprocName, 511, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, List<TReaderResult7>, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultList6, resultList7, logger);
        }
        #endregion

        #region Handle Complex Models with DataReader results
        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with one result set.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
        {
            return ModelFromReaderResultsHandler<TModel>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with one result set.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            return rdr.SetModel<TModel>(instance, shardId, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult">A type with attributes that correspond to a model’s list, populated by a second SELECT result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with two result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            where TReaderResult : new()
        {
            return ModelFromReaderResultsHandler<TModel, TReaderResult>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult">A type with attributes that correspond to a model’s list, populated by a second SELECT result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with two result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);

            rdr.SetModel(instance, shardId, logger);
            List<TReaderResult> resultList;
            if (rdr.NextResult())
            {
                resultList = (List<TReaderResult>)Mapper.ToList<TReaderResult>(rdr, shardId, logger);
            }
            else
            {
                resultList = new List<TReaderResult>();
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(instance, shardId, sprocName, 2, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult>, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to a model’s list, populated by a second SELECT result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to a model’s list, populated by a third SELECT result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with two result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            where TReaderResult0 : new()
            where TReaderResult1 : new()
        {
            return ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to a model’s list, populated by a second SELECT result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to a model’s list, populated by a third SELECT result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with two result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult0 : new()
            where TReaderResult1 : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            rdr.SetModel(instance, shardId, logger);
            List<TReaderResult0> resultList0;
            List<TReaderResult1> resultList1;
            if (rdr.NextResult())
            {
                resultList0 = Mapper.ToList<TReaderResult0>(rdr, shardId, logger);
            }
            else
            {
                resultList0 = new List<TReaderResult0>();
            }
            if (rdr.NextResult())
            {
                resultList1 = Mapper.ToList<TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult0, TReaderResult1, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(instance, shardId, sprocName, 6, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList0, resultList1, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to a model’s list, populated by an second SELECT result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to a model’s list, populated by an third SELECT result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to a model’s list, populated by an fourth SELECT result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with three result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
        {
            return ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to a model’s list, populated by an second SELECT result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to a model’s list, populated by an third SELECT result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to a model’s list, populated by an fourth SELECT result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with three result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            rdr.SetModel(instance, shardId, logger);
            List<TReaderResult0> resultList0;
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            if (rdr.NextResult())
            {
                resultList0 = Mapper.ToList<TReaderResult0>(rdr, shardId, logger);
            }
            else
            {
                resultList0 = new List<TReaderResult0>();
            }
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult0, TReaderResult1, TReaderResult2, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(instance, shardId, sprocName, 14, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList0, resultList1, resultList2, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to a model’s list, populated by an second SELECT result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to a model’s list, populated by an third SELECT result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to a model’s list, populated by an fourth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to a model’s list, populated by an fifth SELECT result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with four result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
        {
            return ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to a model’s list, populated by an second SELECT result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to a model’s list, populated by an third SELECT result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to a model’s list, populated by an fourth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to a model’s list, populated by an fifth SELECT result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with four result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            rdr.SetModel(instance, shardId, logger);
            List<TReaderResult0> resultList0;
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            if (rdr.NextResult())
            {
                resultList0 = Mapper.ToList<TReaderResult0>(rdr, shardId, logger);
            }
            else
            {
                resultList0 = new List<TReaderResult0>();
            }
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(instance, shardId, sprocName, 30, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList0, resultList1, resultList2, resultList3, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to a model’s list, populated by an second SELECT result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to a model’s list, populated by an third SELECT result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to a model’s list, populated by an fourth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to a model’s list, populated by an fifth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to a model’s list, populated by an sixth SELECT result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with five result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
        {
            return ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to a model’s list, populated by an second SELECT result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to a model’s list, populated by an third SELECT result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to a model’s list, populated by an fourth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to a model’s list, populated by an fifth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to a model’s list, populated by an sixth SELECT result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with five result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            rdr.SetModel(instance, shardId, logger);
            List<TReaderResult0> resultList0;
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;
            if (rdr.NextResult())
            {
                resultList0 = Mapper.ToList<TReaderResult0>(rdr, shardId, logger);
            }
            else
            {
                resultList0 = new List<TReaderResult0>();
            }
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, Mapper.DummyType, Mapper.DummyType, Mapper.DummyType, TModel>(instance, shardId, sprocName, 62, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to a model’s list, populated by an second SELECT result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to a model’s list, populated by an third SELECT result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to a model’s list, populated by an fourth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to a model’s list, populated by an fifth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to a model’s list, populated by an sixth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to a model’s list, populated by an sevemtj SELECT result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with six result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
            where TReaderResult5 : new()
        {
            return ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to a model’s list, populated by an second SELECT result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to a model’s list, populated by an third SELECT result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to a model’s list, populated by an fourth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to a model’s list, populated by an fifth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to a model’s list, populated by an sixth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to a model’s list, populated by an sevemtj SELECT result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with six result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
            where TReaderResult5 : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            rdr.SetModel(instance, shardId, logger);
            List<TReaderResult0> resultList0;
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;
            List<TReaderResult5> resultList5;
            if (rdr.NextResult())
            {
                resultList0 = Mapper.ToList<TReaderResult0>(rdr, shardId, logger);
            }
            else
            {
                resultList0 = new List<TReaderResult0>();
            }
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            if (rdr.NextResult())
            {
                resultList5 = (List<TReaderResult5>)Mapper.ToList<TReaderResult5>(rdr, shardId, logger);
            }
            else
            {
                resultList5 = new List<TReaderResult5>();
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, Mapper.DummyType, Mapper.DummyType, TModel>(instance, shardId, sprocName, 126, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5> , ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to a model’s list, populated by an second SELECT result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to a model’s list, populated by an third SELECT result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to a model’s list, populated by an fourth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to a model’s list, populated by an fifth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to a model’s list, populated by an sixth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to a model’s list, populated by an sevemtj SELECT result.</typeparam>
        /// <typeparam name="TReaderResult6">A type with attributes that correspond to a model’s list, populated by an eighth SELECT result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with seven result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
            (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
            where TReaderResult5 : new()
            where TReaderResult6 : new()
        {
            return ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to a model’s list, populated by an second SELECT result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to a model’s list, populated by an third SELECT result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to a model’s list, populated by an fourth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to a model’s list, populated by an fifth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to a model’s list, populated by an sixth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to a model’s list, populated by an sevemtj SELECT result.</typeparam>
        /// <typeparam name="TReaderResult6">A type with attributes that correspond to a model’s list, populated by an eighth SELECT result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with seven result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6>
            (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
            where TReaderResult5 : new()
            where TReaderResult6 : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            rdr.SetModel(instance, shardId, logger);
            List<TReaderResult0> resultList0;
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;
            List<TReaderResult5> resultList5;
            List<TReaderResult6> resultList6;
            if (rdr.NextResult())
            {
                resultList0 = Mapper.ToList<TReaderResult0>(rdr, shardId, logger);
            }
            else
            {
                resultList0 = new List<TReaderResult0>();
            }
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            if (rdr.NextResult())
            {
                resultList5 = (List<TReaderResult5>)Mapper.ToList<TReaderResult5>(rdr, shardId, logger);
            }
            else
            {
                resultList5 = new List<TReaderResult5>();
            }
            if (rdr.NextResult())
            {
                resultList6 = (List<TReaderResult6>)Mapper.ToList<TReaderResult6>(rdr, shardId, logger);
            }
            else
            {
                resultList6 = new List<TReaderResult6>();
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, Mapper.DummyType, TModel>(instance, shardId, sprocName, 254, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultList6, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to a model’s list, populated by an second SELECT result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to a model’s list, populated by an third SELECT result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to a model’s list, populated by an fourth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to a model’s list, populated by an fifth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to a model’s list, populated by an sixth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to a model’s list, populated by an sevemtj SELECT result.</typeparam>
        /// <typeparam name="TReaderResult6">A type with attributes that correspond to a model’s list, populated by an eighth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult7">A type with attributes that correspond to a model’s list, populated by an nineth SELECT result.</typeparam>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with eight result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
    (
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TModel : new()
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
            where TReaderResult5 : new()
            where TReaderResult6 : new()
            where TReaderResult7 : new()
        {
            return ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>(new TModel(), shardId, sprocName, notUsed, rdr, parameters, connectionDescription, logger);
        }

        /// <summary>
        /// A <see cref="ArgentSea.QueryResultModelHandler{TArg, TModel}" /> compatible method which uses Mapping attributes to return a instance of TModel using data reader (SELECT) results.
        /// </summary>
        /// <typeparam name="TModel">The type of the return value, populated by the single-row SELECT query result.</typeparam>
        /// <typeparam name="TReaderResult0">A type with attributes that correspond to a model’s list, populated by an second SELECT result.</typeparam>
        /// <typeparam name="TReaderResult1">A type with attributes that correspond to a model’s list, populated by an third SELECT result.</typeparam>
        /// <typeparam name="TReaderResult2">A type with attributes that correspond to a model’s list, populated by an fourth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult3">A type with attributes that correspond to a model’s list, populated by an fifth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult4">A type with attributes that correspond to a model’s list, populated by an sixth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult5">A type with attributes that correspond to a model’s list, populated by an sevemtj SELECT result.</typeparam>
        /// <typeparam name="TReaderResult6">A type with attributes that correspond to a model’s list, populated by an eighth SELECT result.</typeparam>
        /// <typeparam name="TReaderResult7">A type with attributes that correspond to a model’s list, populated by an nineth SELECT result.</typeparam>
        /// <param name="instance">An existing object instance whose attributes can be populated with data.</param>
        /// <param name="shardId">The shard identifier.</param>
        /// <param name="sprocName">The name of the stored procedure or function, which is used for logging, if any.</param>
        /// <param name="notUsed">An optional argument required by the delegate definition.</param>
        /// <param name="rdr">The open data reader with eight result sets.</param>
        /// <param name="parameters">The output parameter set, which is not used.</param>
        /// <param name="connectionDescription">The connection description is used in logging.</param>
        /// <param name="logger">A logging instance.</param>
        /// <returns>An instance of TModel or default/null.</returns>
        /// <exception cref="ArgentSea.InvalidMapTypeException">Thrown when the property data type is not supported by the MapTo* atribute type.</exception>
        /// <exception cref="ArgentSea.UnexpectedMultiRowResultException">Thrown when the data reader root type has multiple rows.</exception>
        public static TModel ModelFromReaderResultsHandler<TModel, TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7>
    (
            TModel instance,
            short shardId,
            string sprocName,
            object notUsed,
            DbDataReader rdr,
            DbParameterCollection parameters,
            string connectionDescription,
            ILogger logger)
            where TReaderResult0 : new()
            where TReaderResult1 : new()
            where TReaderResult2 : new()
            where TReaderResult3 : new()
            where TReaderResult4 : new()
            where TReaderResult5 : new()
            where TReaderResult6 : new()
            where TReaderResult7 : new()
        {
            ValidateDataReader(sprocName, rdr, connectionDescription, logger);
            rdr.SetModel(instance, shardId, logger);
            List<TReaderResult0> resultList0;
            List<TReaderResult1> resultList1;
            List<TReaderResult2> resultList2;
            List<TReaderResult3> resultList3;
            List<TReaderResult4> resultList4;
            List<TReaderResult5> resultList5;
            List<TReaderResult6> resultList6;
            List<TReaderResult7> resultList7;
            if (rdr.NextResult())
            {
                resultList0 = Mapper.ToList<TReaderResult0>(rdr, shardId, logger);
            }
            else
            {
                resultList0 = new List<TReaderResult0>();
            }
            if (rdr.NextResult())
            {
                resultList1 = (List<TReaderResult1>)Mapper.ToList<TReaderResult1>(rdr, shardId, logger);
            }
            else
            {
                resultList1 = new List<TReaderResult1>();
            }
            if (rdr.NextResult())
            {
                resultList2 = (List<TReaderResult2>)Mapper.ToList<TReaderResult2>(rdr, shardId, logger);
            }
            else
            {
                resultList2 = new List<TReaderResult2>();
            }
            if (rdr.NextResult())
            {
                resultList3 = (List<TReaderResult3>)Mapper.ToList<TReaderResult3>(rdr, shardId, logger);
            }
            else
            {
                resultList3 = new List<TReaderResult3>();
            }
            if (rdr.NextResult())
            {
                resultList4 = (List<TReaderResult4>)Mapper.ToList<TReaderResult4>(rdr, shardId, logger);
            }
            else
            {
                resultList4 = new List<TReaderResult4>();
            }
            if (rdr.NextResult())
            {
                resultList5 = (List<TReaderResult5>)Mapper.ToList<TReaderResult5>(rdr, shardId, logger);
            }
            else
            {
                resultList5 = new List<TReaderResult5>();
            }
            if (rdr.NextResult())
            {
                resultList6 = (List<TReaderResult6>)Mapper.ToList<TReaderResult6>(rdr, shardId, logger);
            }
            else
            {
                resultList6 = new List<TReaderResult6>();
            }
            if (rdr.NextResult())
            {
                resultList7 = (List<TReaderResult7>)Mapper.ToList<TReaderResult7>(rdr, shardId, logger);
            }
            else
            {
                resultList7 = new List<TReaderResult7>();
            }
            var queryKey = typeof(TModel).ToString() + sprocName;
            var lazySqlObjectDelegate = _getRstObjectCache.GetOrAdd(queryKey, new Lazy<Delegate>(() => BuildModelFromResultsExpressions<TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TModel>(instance, shardId, sprocName, 510, logger), LazyThreadSafetyMode.ExecutionAndPublication));
            var sqlObjectDelegate = (Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, List<TReaderResult7>, ILogger, TModel>)lazySqlObjectDelegate.Value;
            return (TModel)sqlObjectDelegate(instance, shardId, sprocName, resultList0, resultList1, resultList2, resultList3, resultList4, resultList5, resultList6, resultList7, logger);
        }
        #endregion

		//private static dynamic AssignRootToResult<TEval, TModel>(string procedureName, IList<TEval> resultList, ILogger logger) where TModel : new() where TEval : new()
		//{
		//	if (resultList is null)
		//	{
		//		throw new Exception($"Procedure {procedureName} failed to return an expected base recordset result.");
		//	}
		//	else if (resultList.Count == 0)
		//	{
  //              logger?.EmptyResult(procedureName);
		//		return default(TModel);
		//	}
		//	else if (resultList.Count > 1)
		//	{
  //              throw new UnexpectedMultiRowResultException(procedureName);
  //          }
  //          else if (typeof(TModel) == typeof(TEval)) 
  //          {
  //              return resultList[0];
  //          }
  //          else
		//	{
  //              throw new Exception($"Evaluated record value did not match expected base model type in procedure {procedureName}.");
  //          }
  //      }

        private static Delegate BuildModelFromResultsExpressions<TReaderResult0, TReaderResult1, TReaderResult2, TReaderResult3, TReaderResult4, TReaderResult5, TReaderResult6, TReaderResult7, TModel>
			(TModel instance, short shardId, string procedureName, int recordSetFlags, ILogger logger)
			where TReaderResult0 : new()
			where TReaderResult1 : new()
			where TReaderResult2 : new()
			where TReaderResult3 : new()
			where TReaderResult4 : new()
			where TReaderResult5 : new()
			where TReaderResult6 : new()
			where TReaderResult7 : new()
		{
            var tModel = typeof(TModel);
            // Build return object
            var expressions = new List<Expression>();
            var expInstance = Expression.Parameter(tModel, "instance");
            var expShardId = Expression.Parameter(typeof(short), "shardId");
            var expProcName = Expression.Parameter(typeof(string), "sprocName");
            var expResultSet0 = Expression.Parameter(typeof(List<TReaderResult0>), "rstResult0");
            var expResultSet1 = Expression.Parameter(typeof(List<TReaderResult1>), "rstResult1");
            var expResultSet2 = Expression.Parameter(typeof(List<TReaderResult2>), "rstResult2");
            var expResultSet3 = Expression.Parameter(typeof(List<TReaderResult3>), "rstResult3");
            var expResultSet4 = Expression.Parameter(typeof(List<TReaderResult4>), "rstResult4");
            var expResultSet5 = Expression.Parameter(typeof(List<TReaderResult5>), "rstResult5");
            var expResultSet6 = Expression.Parameter(typeof(List<TReaderResult6>), "rstResult6");
            var expResultSet7 = Expression.Parameter(typeof(List<TReaderResult7>), "rstResult7");
            //var expResultOut = Expression.Parameter(typeof(TOutResult), "outPrmsResult");
			var expLogger = Expression.Parameter(typeof(ILogger), "logger");
			//var expModel = Expression.Variable(typeof(TModel), "model"); //TODO: come back to this after creating a single row handler for resorsets.
			var expCount = Expression.Variable(typeof(int), "count");
			var expExitTarget = Expression.Label("return");
			var expNull = Expression.Constant(null);
            var expOne = Expression.Constant(1);
			//var isPrmOutUsed = false;
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
            //var miSetResultSetGeneric = typeof(Mapper).GetMethod(nameof(Mapper.AssignRootToResult), BindingFlags.Static | BindingFlags.NonPublic);

            var miFormat = typeof(string).GetMethod(nameof(string.Concat), BindingFlags.Static);
			var modelType = typeof(TModel);

            // 1. Try to create an instance of our result class.
            using (logger?.BuildSqlResultsHandlerScope(procedureName, tModel))
			{

			//	//Set base type to some result value, if we can.
			//	if ((recordSetFlags & 1) == 1 && modelType == typeof(TOutResult))
			//	{
			//		expressions.Add(Expression.Assign(expModel, expResultOut));
			//		isPrmOutUsed = true;
			//	}
			//	else if ((recordSetFlags & 2) == 2 && modelType == typeof(TReaderResult0))
			//	{
			//		expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod(new Type[] { typeof(TReaderResult0), modelType }), expProcName, expResultSet0, expLogger)));
			//		expressions.Add(Expression.IfThen(
			//			Expression.Equal(expModel, expNull), //if (result == null)
			//			Expression.Return(expExitTarget, Expression.Default(tModel), tModel) //return default;
			//			));
			//		isRdrResult0Used = true;
			//	}
			//	else if ((recordSetFlags & 4) == 4 && modelType == typeof(TReaderResult1))
			//	{
			//		expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult1), modelType }), expProcName, expResultSet1, expLogger)));
			//		expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, Expression.Default(tModel), typeof(TModel))));
			//		isRdrResult1Used = true;
			//	}
			//	else if ((recordSetFlags & 8) == 8 && modelType == typeof(TReaderResult2))
			//	{
			//		expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult2), modelType }), expProcName, expResultSet2, expLogger)));
			//		expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, Expression.Default(tModel), typeof(TModel))));
			//		isRdrResult2Used = true;
			//	}
			//	else if ((recordSetFlags & 16) == 16 && modelType == typeof(TReaderResult3))
			//	{
			//		expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult3), modelType }), expProcName, expResultSet3, expLogger)));
			//		expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, Expression.Default(tModel), typeof(TModel))));
			//		isRdrResult3Used = true;
			//	}
			//	else if ((recordSetFlags & 32) == 32 && modelType == typeof(TReaderResult4))
			//	{
			//		expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult4), modelType }), expProcName, expResultSet4, expLogger)));
			//		expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, Expression.Default(tModel), typeof(TModel))));
			//		isRdrResult4Used = true;
			//	}
			//	else if ((recordSetFlags & 64) == 64 && modelType == typeof(TReaderResult5))
			//	{
			//		expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult5), modelType }), expProcName, expResultSet5, expLogger)));
			//		expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, Expression.Default(tModel), typeof(TModel))));
			//		isRdrResult5Used = true;
			//	}
			//	else if ((recordSetFlags & 128) == 128 && modelType == typeof(TReaderResult6))
			//	{
			//		expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult6), modelType }), expProcName, expResultSet6, expLogger)));
			//		expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, Expression.Default(tModel), typeof(TModel))));
			//		isRdrResult6Used = true;
			//	}
			//	else if ((recordSetFlags & 256) == 256 && modelType == typeof(TReaderResult7))
			//	{
			//		expressions.Add(Expression.Assign(expModel, Expression.Call(miSetResultSetGeneric.MakeGenericMethod().MakeGenericMethod(new Type[] { typeof(TReaderResult7), modelType }), expProcName, expResultSet7, expLogger)));
			//		expressions.Add(Expression.IfThen(Expression.Equal(expModel, expNull), Expression.Return(expExitTarget, Expression.Default(tModel), typeof(TModel))));
			//		isRdrResult7Used = true;
			//	}
			//	else
			//	{
			//		//match not found, so just create a new instance and we'll try again on properties
			//		expressions.Add(Expression.Assign(expModel, expInstance));
			//	}

				var props = modelType.GetProperties();

                //Iterate over any List<> properties and set any List<resultSet> that match.
                foreach (var prop in props)
                {
                    if ((recordSetFlags & 2) == 2 && prop.PropertyType == expResultSet0.Type && !isRdrResult0Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), expResultSet0));
                        isRdrResult0Used = true;
                    }
                    else if ((recordSetFlags & 2) == 2 && prop.PropertyType.IsAssignableFrom(expResultSet0.Type) && !isRdrResult0Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), Expression.Convert(expResultSet0, prop.PropertyType)));
                        isRdrResult0Used = true;
                    }
                    else if ((recordSetFlags & 4) == 4 && prop.PropertyType == expResultSet1.Type && !isRdrResult1Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), expResultSet1));
                        isRdrResult1Used = true;
                    }
                    else if ((recordSetFlags & 4) == 4 && prop.PropertyType.IsAssignableFrom(expResultSet1.Type) && !isRdrResult1Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), Expression.Convert(expResultSet1, prop.PropertyType)));
                        isRdrResult1Used = true;
                    }
                    else if ((recordSetFlags & 8) == 8 && prop.PropertyType == expResultSet2.Type && !isRdrResult2Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), expResultSet2));
                        isRdrResult2Used = true;
                    }
                    else if ((recordSetFlags & 8) == 8 && prop.PropertyType.IsAssignableFrom(expResultSet2.Type) && !isRdrResult2Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), Expression.Convert(expResultSet2, prop.PropertyType)));
                        isRdrResult2Used = true;
                    }
                    else if ((recordSetFlags & 16) == 16 && prop.PropertyType == expResultSet3.Type && !isRdrResult3Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), expResultSet3));
                        isRdrResult3Used = true;
                    }
                    else if ((recordSetFlags & 16) == 16 && prop.PropertyType.IsAssignableFrom(expResultSet3.Type) && !isRdrResult3Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), Expression.Convert(expResultSet3, prop.PropertyType)));
                        isRdrResult3Used = true;
                    }
                    else if ((recordSetFlags & 32) == 32 && prop.PropertyType == expResultSet4.Type && !isRdrResult4Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), expResultSet4));
                        isRdrResult4Used = true;
                    }
                    else if ((recordSetFlags & 32) == 32 && prop.PropertyType.IsAssignableFrom(expResultSet4.Type) && !isRdrResult4Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), Expression.Convert(expResultSet4, prop.PropertyType)));
                        isRdrResult4Used = true;
                    }
                    else if ((recordSetFlags & 64) == 64 && prop.PropertyType == expResultSet5.Type && !isRdrResult5Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), expResultSet5));
                        isRdrResult5Used = true;
                    }
                    else if ((recordSetFlags & 64) == 64 && prop.PropertyType.IsAssignableFrom(expResultSet5.Type) && !isRdrResult5Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), Expression.Convert(expResultSet5, prop.PropertyType)));
                        isRdrResult5Used = true;
                    }
                    else if ((recordSetFlags & 128) == 128 && prop.PropertyType == expResultSet6.Type && !isRdrResult6Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), expResultSet6));
                        isRdrResult6Used = true;
                    }
                    else if ((recordSetFlags & 128) == 128 && prop.PropertyType.IsAssignableFrom(expResultSet6.Type) && !isRdrResult6Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), Expression.Convert(expResultSet6, prop.PropertyType)));
                        isRdrResult6Used = true;
                    }
                    else if ((recordSetFlags & 256) == 256 && prop.PropertyType == expResultSet7.Type && !isRdrResult7Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), expResultSet7));
                        isRdrResult7Used = true;
                    }
                    else if ((recordSetFlags & 256) == 256 && prop.PropertyType.IsAssignableFrom(expResultSet7.Type) && !isRdrResult7Used)
                    {
                        expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), Expression.Convert(expResultSet7, prop.PropertyType)));
                        isRdrResult7Used = true;
                    }
                }
                //Iterate over any object (non-list) properties and set any resultSet that match.
                foreach (var prop in props)
				{
					//if ((recordSetFlags & 1) == 1 && prop.PropertyType.IsAssignableFrom(typeof(TOutResult)) && !isPrmOutUsed)
					//{
					//	expressions.Add(Expression.Assign(Expression.Property(expInstance, prop), expResultOut));
					//	isPrmOutUsed = true;
					//}
					//else 
                    if ((recordSetFlags & 2) == 2 && prop.PropertyType.IsAssignableFrom(typeof(TReaderResult0)) && !isRdrResult0Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));   //var count = 0;
						expressions.Add(Expression.IfThen(                                      //if (resultSet0 != null)
							Expression.NotEqual(expResultSet0, expNull),                        //{ 
							Expression.Assign(expCount, Expression.Property(expResultSet0, miCount)) //count = resultSet0.Count;
							));                                                                 //}
						expressions.Add(Expression.IfThenElse(                                  //if (count == 1)
							Expression.Equal(expCount, expOne),                                 //{ result.prop = resultSet0[0]; }
							Expression.Assign(Expression.Property(expInstance, prop), Expression.Property(expResultSet0, "Item", Expression.Constant(0))),
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
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expInstance, prop), Expression.Property(expResultSet1, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} on unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult1Used = true;
					}
					else if ((recordSetFlags & 8) == 8 && prop.PropertyType.IsAssignableFrom(typeof(TReaderResult2)) && !isRdrResult2Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet2, expNull), Expression.Assign(expCount, Expression.Property(expResultSet2, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expInstance, prop), Expression.Property(expResultSet2, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult2Used = true;
					}
					else if ((recordSetFlags & 16) == 16 && prop.PropertyType.IsAssignableFrom(typeof(TReaderResult3)) && !isRdrResult3Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet3, expNull), Expression.Assign(expCount, Expression.Property(expResultSet3, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expInstance, prop), Expression.Property(expResultSet3, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult3Used = true;
					}
					else if ((recordSetFlags & 32) == 32 && prop.PropertyType.IsAssignableFrom(typeof(TReaderResult4)) && !isRdrResult4Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet4, expNull), Expression.Assign(expCount, Expression.Property(expResultSet4, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expInstance, prop), Expression.Property(expResultSet4, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult4Used = true;
					}
					else if ((recordSetFlags & 64) == 64 && prop.PropertyType.IsAssignableFrom(typeof(TReaderResult5)) && !isRdrResult5Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet5, expNull), Expression.Assign(expCount, Expression.Property(expResultSet5, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expInstance, prop), Expression.Property(expResultSet5, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult5Used = true;
					}
					else if ((recordSetFlags & 128) == 128 && prop.PropertyType.IsAssignableFrom(typeof(TReaderResult6)) && !isRdrResult6Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet6, expNull), Expression.Assign(expCount, Expression.Property(expResultSet6, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expInstance, prop), Expression.Property(expResultSet6, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult6Used = true;
					}
					else if ((recordSetFlags & 256) == 256 && prop.PropertyType.IsAssignableFrom(typeof(TReaderResult7)) && !isRdrResult7Used)
					{
						expressions.Add(Expression.Assign(expCount, Expression.Constant(0)));
						expressions.Add(Expression.IfThen(Expression.NotEqual(expResultSet7, expNull), Expression.Assign(expCount, Expression.Property(expResultSet7, miCount))));
						expressions.Add(Expression.IfThenElse(Expression.Equal(expCount, expOne), Expression.Assign(Expression.Property(expInstance, prop), Expression.Property(expResultSet7, "Item", Expression.Constant(0))),
							Expression.IfThen(Expression.GreaterThan(expCount, expOne), Expression.Call(miLogError,
								Expression.Call(miFormat, Expression.Constant($"Could not set property {prop.Name} because procedure {procedureName} unexpectedly returned {{0}} results instead of one."), expCount)))));
						isRdrResult7Used = true;
					}
				}
                var expExit = Expression.Label(expExitTarget);
				expressions.Add(expExit); //Exit procedure
			}

			expressions.Add(expInstance); //return model
			var expBlock = Expression.Block(modelType, new ParameterExpression[] { expCount }, expressions); //+variables

            if (recordSetFlags == 2)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TModel>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 3)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TReaderResult0>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 6)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expResultSet1, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 7)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expResultSet1, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 14)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 15)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 30)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 31)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 62)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 63)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 126)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 127)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 254)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expResultSet6, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 255)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expResultSet6, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 510)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expResultSet6, expResultSet7, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, List<TReaderResult7>, ILogger, TModel>>(expBlock, prms);
                logger?.CreatedExpressionTreeForModel(tModel, procedureName, expBlock);
                return lambda.Compile();
            }
            if (recordSetFlags == 511)
            {
                var prms = new ParameterExpression[] { expInstance, expShardId, expProcName, expResultSet0, expResultSet1, expResultSet2, expResultSet3, expResultSet4, expResultSet5, expResultSet6, expResultSet7, expLogger };
                var lambda = Expression.Lambda<Func<TModel, short, string, List<TReaderResult0>, List<TReaderResult1>, List<TReaderResult2>, List<TReaderResult3>, List<TReaderResult4>, List<TReaderResult5>, List<TReaderResult6>, List<TReaderResult7>, ILogger, TModel>>(expBlock, prms);
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


        public class DummyType
		{
		}
	}
}
