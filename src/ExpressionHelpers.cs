using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace ArgentSea
{
    public static class ExpressionHelpers
    {
        public static Expression InParmHelper(string parameterName, ParameterExpression prms, Expression propValue, Type staticType, string addMethod, ConstantExpression thirdArg, ConstantExpression forthArg, ParameterExpression expIgnoreParameters)
        {
            List<Type> methodSignatureTypes = new List<Type>(); //to get correct method overload
            List<Expression> methodArgumentExpressions = new List<Expression>(); //parameter, variable, or constant expressions
            var expParameterName = Expression.Constant(ToParameterName(parameterName), typeof(string));
            //var expShoudIgnore = Expression.Call(typeof(Mapper).GetMethod(nameof(IgnoreThisParameter), new[] { typeof(string), typeof(HashSet<string>) }), new Expression[] { expParameterName, expIgnoreParameters });
            var expShouldIgnore = Expression.Call(typeof(ShardMapper<>).GetMethod(nameof(DontIgnoreThisParameter), BindingFlags.Static | BindingFlags.NonPublic), new Expression[] { expParameterName, expIgnoreParameters });

            methodSignatureTypes.Add(typeof(DbParameterCollection)); //Arg0: prms parameter in this signature
            methodSignatureTypes.Add(typeof(string)); //Arg1: parameterName parameter in this signature
            methodSignatureTypes.Add(propValue.Type); //Arg2: <value> parameter in this signature

            methodArgumentExpressions.Add(prms);
            methodArgumentExpressions.Add(expParameterName);
            methodArgumentExpressions.Add(propValue);

            if (!(thirdArg is null))
            {
                methodSignatureTypes.Add(thirdArg.Type); //Optional Arg3: length/precision parameter in this signature
                methodArgumentExpressions.Add(thirdArg);
            }
            if (!(forthArg is null))
            {
                methodSignatureTypes.Add(forthArg.Type); //Optional Arg4: codepage/scale parameter in this signature
                methodArgumentExpressions.Add(forthArg);
            }
            var mSql = staticType.GetMethod(addMethod, methodSignatureTypes.ToArray()); //get methodinfo for the correct overload
            return Expression.IfThen(
                expShouldIgnore,
                Expression.Call(mSql, methodArgumentExpressions.ToArray()));
        }

        public static void OutParameterBuilder(string parameterName, ParameterExpression prms, IList<Expression> expressions, Type staticType, string addMethod, ConstantExpression secondArg, ConstantExpression thirdArg, HashSet<string> parameterNames, ParameterExpression expIgnoreParameters, ILogger logger)
        {
            var dataName = ToParameterName(parameterName);
            if (parameterNames.Add(dataName))
            {
                List<Type> methodSignatureTypes = new List<Type>(); //to get correct method overload
                List<Expression> methodArgumentExpressions = new List<Expression>(); //parameter, variable, or constant expressions
                var expParameterName = Expression.Constant(ToParameterName(parameterName), typeof(string));
                var expShouldIgnore = Expression.Call(typeof(ShardMapper<>).GetMethod(nameof(DontIgnoreThisParameter), BindingFlags.Static | BindingFlags.NonPublic), new Expression[] { expParameterName, expIgnoreParameters });

                methodSignatureTypes.Add(typeof(DbParameterCollection)); //Arg0: prms parameter in this signature
                methodSignatureTypes.Add(typeof(string)); //Arg1: parameterName parameter in this signature
                methodArgumentExpressions.Add(prms);
                methodArgumentExpressions.Add(expParameterName);
                if (!(secondArg is null))
                {
                    methodSignatureTypes.Add(secondArg.Type); //Optional Arg2: length/scale parameter in this signature
                    methodArgumentExpressions.Add(secondArg);
                }
                if (!(thirdArg is null))
                {
                    methodSignatureTypes.Add(thirdArg.Type); //Optional Arg3: codepage/precision parameter in this signature
                    methodArgumentExpressions.Add(thirdArg);
                }
                var mSql = staticType.GetMethod(addMethod, methodSignatureTypes.ToArray()); //get methodinfo for the correct overload
                var expCall = Expression.Call(mSql, methodArgumentExpressions.ToArray());
                var expResult = Expression.IfThen(expShouldIgnore, expCall);
                expressions.Add(expResult);
                logger.SqlExpressionLog(expResult);
            }
        }

        #region Expression builders
        public static void InParameterStringExpressionBuilder(string parameterName, int length, Type staticType, string methodName, ConstantExpression expLocale, IList<Expression> expressions, ParameterExpression prms, ParameterExpression expIgnoreParameters, HashSet<string> parameterNames, MemberExpression propValue, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
            var dataName = ToParameterName(parameterName);
            if (parameterNames.Add(dataName))
            {
                if (propertyType.IsEnum)
                {
                    var miEnumToString = typeof(Enum).GetMethod(nameof(Enum.ToString), new Type[] { });
                    var expEnumToString = Expression.Call(propValue, miEnumToString);
                    var expInParm = InParmHelper(dataName, prms, expEnumToString, staticType, methodName, Expression.Constant(length, typeof(int)), expLocale, expIgnoreParameters);
                    expressions.Add(expInParm);
                    logger.SqlExpressionLog(expInParm);

                }
                //else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && Nullable.GetUnderlyingType(propertyType) == typeof(ShardKey<,>)) //Nullable ShardKey
                //{
                //    var piNullableHasValue = propertyType.GetProperty(nameof(Nullable<int>.HasValue));
                //    var piNullableGetValue = propertyType.GetProperty(nameof(Nullable<int>.Value));

                //    var expProp = Expression.Condition(
                //        Expression.Property(propValue, piNullableHasValue),
                //       Expression.Convert(Expression.Property(Expression.Property(propValue, piNullableGetValue), ShardPropertyName(shardPosition)), typeof(string)),
                //        Expression.Constant(null, typeof(string))
                //        );
                //    var expInParm = InParmHelper(dataName, prms, expProp, staticType, methodName, Expression.Constant(length, typeof(int)), expLocale, expIgnoreParameters);
                //    expressions.Add(expInParm);
                //    logger.SqlExpressionLog(expInParm);
                //}
                //else if (propertyType == typeof(ShardKey<,>))
                //{
                //    var expProp = Expression.Property(propValue, ShardPropertyName(shardPosition));
                //    var expInParm = InParmHelper(dataName, prms, expProp, staticType, methodName, Expression.Constant(length, typeof(int)), expLocale, expIgnoreParameters);
                //    expressions.Add(expInParm);
                //    logger.SqlExpressionLog(expInParm);

                //}
                else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && Nullable.GetUnderlyingType(propertyType).IsEnum) //Nullable Enum
                {
                    var miEnumToString = typeof(Enum).GetMethod(nameof(Enum.ToString), new Type[] { });
                    var piNullableHasValue = propertyType.GetProperty(nameof(Nullable<int>.HasValue));
                    var piNullableGetValue = propertyType.GetProperty(nameof(Nullable<int>.Value));

                    var expIf = Expression.Condition(
                        Expression.Property(propValue, piNullableHasValue),
                        Expression.Call(Expression.Property(propValue, piNullableGetValue), miEnumToString),
                        Expression.Constant(null, typeof(string))
                        );
                    var expInParm = InParmHelper(dataName, prms, expIf, staticType, methodName, Expression.Constant(length, typeof(int)), expLocale, expIgnoreParameters);
                    expressions.Add(expInParm);
                    logger.SqlExpressionLog(expInParm);
                }
                else
                {
                    var expInParm = InParmHelper(dataName, prms, propValue, staticType, methodName, Expression.Constant(length, typeof(int)), expLocale, expIgnoreParameters);
                    expressions.Add(expInParm);
                    logger.SqlExpressionLog(expInParm);
                }
            }
        }

        public static void InParameterEnumXIntExpressionBuilder(string parameterName, Type staticType, string addMethodName, Type nullableBaseType, IList<Expression> expressions, ParameterExpression prms, ParameterExpression expIgnoreParameters, HashSet<string> parameterNames, MemberExpression propValue, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
            var dataName = ToParameterName(parameterName);
            if (parameterNames.Add(dataName))
            {
                if (propertyType.IsEnum)
                {
                    var expConvert = Expression.Convert(propValue, Nullable.GetUnderlyingType(nullableBaseType));
                    var expInParm = InParmHelper(dataName, prms, expConvert, staticType, addMethodName, null, null, expIgnoreParameters);
                    expressions.Add(expInParm);
                    logger.SqlExpressionLog(expInParm);
                }
                //else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && Nullable.GetUnderlyingType(propertyType) == typeof(ShardKey<,>)) //Nullable ShardKey
                //{
                //    var piNullableHasValue = propertyType.GetProperty(nameof(Nullable<int>.HasValue));
                //    var piNullableGetValue = propertyType.GetProperty(nameof(Nullable<int>.Value));
                //    var nullableShardBaseType = GetShardPropertyType(shardPosition, Nullable.GetUnderlyingType(propertyType), true);

                //    var expProp = Expression.Condition(
                //        Expression.Property(propValue, piNullableHasValue),
                //       Expression.Convert(Expression.Property(Expression.Property(propValue, piNullableGetValue), ShardPropertyName(shardPosition)), nullableShardBaseType),
                //        Expression.Constant(null, typeof(string))
                //        );
                //    var expInParm = InParmHelper(dataName, prms, expProp, staticType, addMethodName, null, null, expIgnoreParameters);
                //    expressions.Add(expInParm);
                //    logger.SqlExpressionLog(expInParm);
                //}
                //else if (propertyType == typeof(ShardKey<,>))
                //{
                //    var expProp = Expression.Property(propValue, ShardPropertyName(shardPosition));
                //    var expInParm = InParmHelper(dataName, prms, expProp, staticType, addMethodName, null, null, expIgnoreParameters);
                //    expressions.Add(expInParm);
                //    logger.SqlExpressionLog(expInParm);

                //}
                else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && Nullable.GetUnderlyingType(propertyType).IsEnum) //Nullable Enum
                {

                    var piNullableHasValue = propertyType.GetProperty(nameof(Nullable<int>.HasValue));
                    var piNullableGetValue = propertyType.GetProperty(nameof(Nullable<int>.Value));

                    var expIf = Expression.Condition(
                        Expression.Property(propValue, piNullableHasValue),
                       Expression.Convert(Expression.Property(propValue, piNullableGetValue), nullableBaseType),
                        Expression.Constant(null, nullableBaseType)
                        );
                    var expInParm = InParmHelper(dataName, prms, expIf, staticType, addMethodName, null, null, expIgnoreParameters);
                    expressions.Add(expInParm);
                    logger.SqlExpressionLog(expInParm);
                }
                else
                {
                    var expInParm = InParmHelper(dataName, prms, propValue, staticType, addMethodName, null, null, expIgnoreParameters);
                    expressions.Add(expInParm);
                    logger.SqlExpressionLog(expInParm);
                }
            }
        }
        public static void InParameterSimpleBuilder(string parameterName, Type propertyType, ParameterExpression prms, ParameterExpression expIgnoreParameters, MemberExpression expProperty, IList<Expression> expressions, Type staticType, string addMethod, ConstantExpression thirdArg, ConstantExpression forthArg, HashSet<string> parameterNames, ParameterExpression expLogger, ILogger logger)
        {
            var dataName = ToParameterName(parameterName);
            if (parameterNames.Add(dataName))
            {

                //if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && Nullable.GetUnderlyingType(propertyType) == typeof(ShardKey<,>)) //Nullable ShardKey
                //{
                //    var piNullableHasValue = propertyType.GetProperty(nameof(Nullable<int>.HasValue));
                //    var piNullableGetValue = propertyType.GetProperty(nameof(Nullable<int>.Value));
                //    var nullableShardBaseType = GetShardPropertyType(shardPosition, Nullable.GetUnderlyingType(propertyType), true);

                //    var expProp = Expression.Condition(
                //        Expression.Property(expProperty, piNullableHasValue),
                //       Expression.Convert(Expression.Property(Expression.Property(expProperty, piNullableGetValue), ShardPropertyName(shardPosition)), nullableShardBaseType),
                //        Expression.Constant(null, typeof(string))
                //        );
                //    var expInParm = InParmHelper(dataName, prms, expProp, staticType, addMethod, null, null, expIgnoreParameters);
                //    expressions.Add(expInParm);
                //    logger.SqlExpressionLog(expInParm);
                //}
                //else if (propertyType == typeof(ShardKey<,>))
                //{
                //    var expProp = Expression.Property(expProperty, ShardPropertyName(shardPosition));
                //    var expInParm = InParmHelper(dataName, prms, expProp, staticType, addMethod, null, null, expIgnoreParameters);
                //    expressions.Add(expInParm);
                //    logger.SqlExpressionLog(expInParm);

                //}
                //else
                //{
                    var expIn = InParmHelper(dataName, prms, expProperty, staticType, addMethod, thirdArg, forthArg, expIgnoreParameters);
                    expressions.Add(expIn);
                    logger.SqlExpressionLog(expIn);
                //}

            }
        }

        public static void ReadOutParameterStringExpressions(string parameterName, Type staticType, string getMethodName, Expression expProperty, IList<Expression> expressions, ParameterExpression expPrms, ParameterExpression expPrm, PropertyInfo propertyInfo, ParameterExpression expLogger, ILogger logger)
        {
            var miGetParameter = typeof(ShardMapper<>).GetMethod(nameof(GetParameter), BindingFlags.Static | BindingFlags.NonPublic);
            var expAssign = Expression.Assign(expPrm, Expression.Call(miGetParameter, expPrms, Expression.Constant(ToParameterName(parameterName), typeof(string))));
            expressions.Add(expAssign);
            logger.SqlExpressionLog(expAssign);

            var propertyType = propertyInfo.PropertyType;

            var miGetString = staticType.GetMethod(getMethodName);
            var expGetString = Expression.Call(miGetString, expPrm);
            if (propertyType.IsEnum)
            {
                var miEnumParse = typeof(Enum).GetMethod(nameof(Enum.Parse), new[] { typeof(Type), typeof(string) });
                var expSet = Expression.Assign(expProperty, Expression.Convert(Expression.Call(miEnumParse, Expression.Constant(propertyType, typeof(Type)), Expression.Call(miGetString, expPrm)), propertyType));
                var expIf = Expression.IfThenElse(Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))),
                    expSet,
                    Expression.Call(typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyInfo, typeof(PropertyInfo)) }));
                expressions.Add(expIf);
                logger.SqlExpressionLog(expIf);
            }
            else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && Nullable.GetUnderlyingType(propertyType).IsEnum) //Nullable Enum
            {
                var miEnumParse = typeof(Enum).GetMethod(nameof(Enum.Parse), new[] { typeof(Type), typeof(string) });
                var expEnumParse = Expression.Convert(Expression.Call(miEnumParse, Expression.Constant(Nullable.GetUnderlyingType(propertyType), typeof(Type)), expGetString), propertyType);
                //var piNullableValue = propertyType.GetProperty(nameof(Nullable<int>.Value));
                var expIf = Expression.Condition(
                    Expression.Equal(expGetString, Expression.Constant(null, typeof(string))),
                    Expression.Constant(null, propertyType),
                    expEnumParse
                    );
                var expIfNull = Expression.IfThenElse(Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))),
                    Expression.Assign(expProperty, expIf),
                    Expression.Call(typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyInfo, typeof(PropertyInfo)) }));
                expressions.Add(expIfNull);
                logger.SqlExpressionLog(expIfNull);
            }
            else
            {
                var expIf = Expression.IfThen(Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))), Expression.Assign(expProperty, expGetString));
                expressions.Add(expIf);
                logger.SqlExpressionLog(expIf);
            }
        }

        public static void ReadOutParameterEnumXIntExpressions(string parameterName, Type staticType, string getMethodName, string nullableGetMethodName, Expression expProperty, IList<Expression> expressions, ParameterExpression expPrms, ParameterExpression expPrm, PropertyInfo propertyInfo, ParameterExpression expLogger, ILogger logger)
        {
            var miGetParameter = typeof(ShardMapper<>).GetMethod(nameof(GetParameter), BindingFlags.Static | BindingFlags.NonPublic);
            var expAssign = Expression.Assign(expPrm, Expression.Call(miGetParameter, expPrms, Expression.Constant(ToParameterName(parameterName), typeof(string))));
            expressions.Add(expAssign);
            logger.SqlExpressionLog(expAssign);
            var propertyType = propertyInfo.PropertyType;

            if (propertyType.IsEnum)
            {
                var miGet = staticType.GetMethod(getMethodName);
                var expGet = Expression.Call(miGet, expPrm);
                var expIf = Expression.IfThenElse(Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))),
                    Expression.Assign(expProperty, Expression.Convert(expGet, propertyType)),
                    Expression.Call(typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyInfo, typeof(PropertyInfo)) }));
                expressions.Add(expIf);
                logger.SqlExpressionLog(expIf);
            }
            else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var miGetNl = staticType.GetMethod(nullableGetMethodName);
                var expGetNl = Expression.Call(miGetNl, expPrm);
                var baseType = Nullable.GetUnderlyingType(propertyType);
                if (baseType.IsEnum)
                {
                    var expIf = Expression.IfThenElse(Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))),
                        Expression.Assign(expProperty, Expression.Convert(expGetNl, propertyType)),
                        Expression.Call(typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyInfo, typeof(PropertyInfo)) }));
                    expressions.Add(expIf);
                    logger.SqlExpressionLog(expIf);
                }
                else
                {
                    var expIf = Expression.IfThenElse(Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))),
                        Expression.Assign(expProperty, expGetNl),
                        Expression.Call(typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyInfo, typeof(PropertyInfo)) }));
                    expressions.Add(expIf);
                    logger.SqlExpressionLog(expIf);
                }
            }
            else
            {
                var miGet = staticType.GetMethod(getMethodName);
                var expGet = Expression.Call(miGet, expPrm);
                var expIf = Expression.IfThenElse(Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))),
                    Expression.Assign(expProperty, expGet),
                    Expression.Call(typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyInfo, typeof(PropertyInfo)) })
                    );
                expressions.Add(expIf);
                logger.SqlExpressionLog(expIf);
            }
        }

        public static void ReadOutParameterSimpleValueExpressions(string parameterName, Type staticType, string getMethodName, string nullableGetMethodName, Expression expProperty, IList<Expression> expressions, ParameterExpression expPrms, ParameterExpression expPrm, PropertyInfo propertyInfo, ParameterExpression expLogger, ILogger logger)
        {
            var miGetParameter = typeof(ShardMapper<>).GetMethod(nameof(GetParameter), BindingFlags.Static | BindingFlags.NonPublic);
            var expAssign = Expression.Assign(expPrm, Expression.Call(miGetParameter, expPrms, Expression.Constant(ToParameterName(parameterName), typeof(string))));
            expressions.Add(expAssign);
            logger.SqlExpressionLog(expAssign);
            var propertyType = propertyInfo.PropertyType;

            MethodCallExpression expGet;
            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                expGet = Expression.Call(staticType.GetMethod(nullableGetMethodName), expPrm);
            }
            else
            {
                expGet = Expression.Call(staticType.GetMethod(getMethodName), expPrm);
            }
            var expIf = Expression.IfThenElse(Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))),
                Expression.Assign(expProperty, expGet),
                Expression.Call(typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyInfo, typeof(PropertyInfo)) })
                );
            expressions.Add(expIf);
            logger.SqlExpressionLog(expIf);
        }
        public static void ReadOutParameterBinaryExpressions(string parameterName, Type staticType, string getMethodName, Expression expProperty, IList<Expression> expressions, ParameterExpression expPrms, ParameterExpression expPrm, PropertyInfo propertyInfo, ParameterExpression expLogger, ILogger logger)
        {
            var miGetParameter = typeof(ShardMapper<>).GetMethod(nameof(GetParameter), BindingFlags.Static | BindingFlags.NonPublic);
            var expAssignPrm = Expression.Assign(expPrm, Expression.Call(miGetParameter, expPrms, Expression.Constant(ToParameterName(parameterName), typeof(string))));
            expressions.Add(expAssignPrm);
            logger.SqlExpressionLog(expAssignPrm);

            var expSet = Expression.Assign(expProperty, Expression.Call(staticType.GetMethod(getMethodName), expPrm));
            var expIf = Expression.IfThenElse(
                Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))),
                expSet,
                Expression.Call(typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyInfo, typeof(PropertyInfo)) })
                );
            expressions.Add(expIf);
            logger.SqlExpressionLog(expIf);
        }

        public static void ReaderStringExpressions(string parameterName, MemberExpression expProperty, IList<MethodCallExpression> columnLookupExpressions, IList<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
            var miGetTypedFieldValue = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue)).MakeGenericMethod(typeof(string));
            var expGetField = Expression.Call(prmSqlRdr, miGetTypedFieldValue, new[] { expOrdinal });

            var miGetFieldOrdinal = typeof(ShardMapper<>).GetMethod(nameof(GetFieldOrdinal), BindingFlags.NonPublic | BindingFlags.Static);
            columnLookupExpressions.Add(Expression.Call(miGetFieldOrdinal, new Expression[] { prmSqlRdr, Expression.Constant(parameterName, typeof(string)) }));
            var expAssign = Expression.Assign(expOrdinal, Expression.ArrayAccess(expOrdinals, new[] { Expression.Constant(propIndex, typeof(int)) }));
            expressions.Add(expAssign);
            logger.SqlExpressionLog(expAssign);

            if (propertyType.IsEnum)
            {
                var miEnumParse = typeof(Enum).GetMethod(nameof(Enum.Parse), new[] { typeof(Type), typeof(string) });
                var expEnumAssign = Expression.Assign(expProperty, Expression.Convert(Expression.Call(miEnumParse, new Expression[] { Expression.Constant(propertyType, typeof(Type)), expGetField }), propertyType));

                var expIf = Expression.IfThen(
                    Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
                           expEnumAssign
                    );
                expressions.Add(expIf);
                logger.SqlExpressionLog(expIf);
            }
            else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && Nullable.GetUnderlyingType(propertyType).IsEnum)
            {
                var miEnumParse = typeof(Enum).GetMethod(nameof(Enum.Parse), new[] { typeof(Type), typeof(string) });
                var expEnumParse = Expression.Convert(Expression.Call(miEnumParse, Expression.Constant(Nullable.GetUnderlyingType(propertyType), typeof(Type)), expGetField), propertyType);
                var expEnumAssign = Expression.Assign(expProperty, expEnumParse);

                var miIsDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull));
                var expIf = Expression.IfThen(
                    Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
                    Expression.IfThenElse(
                           //if
                           Expression.Call(prmSqlRdr, miIsDbNull, new[] { expOrdinal }),
                           //then
                           Expression.Assign(expProperty, Expression.Constant(null, propertyType)),
                           //else
                           expEnumAssign
                           )
                    );
                expressions.Add(expIf);
                logger.SqlExpressionLog(expIf);
            }
            else
            {
                var miIsDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull));
                var expIf = Expression.IfThen(
                    Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
                    Expression.IfThenElse(
                           //if
                           Expression.Call(prmSqlRdr, miIsDbNull, new[] { expOrdinal }),
                           //then
                           Expression.Assign(expProperty, Expression.Constant(null, propertyType)),
                           //else
                           Expression.Assign(expProperty, expGetField)
                           )
                    );
                expressions.Add(expIf);
                logger.SqlExpressionLog(expIf);
            }
        }

        public static void ReaderEnumXIntExpressions(string parameterName, MemberExpression expProperty, Type baseType, IList<MethodCallExpression> columnLookupExpressions, IList<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
            var miGetTypedFieldValue = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue)).MakeGenericMethod(baseType);
            var expGetField = Expression.Call(prmSqlRdr, miGetTypedFieldValue, new[] { expOrdinal });
            var miGetFieldOrdinal = typeof(ShardMapper<>).GetMethod(nameof(GetFieldOrdinal), BindingFlags.NonPublic | BindingFlags.Static);
            columnLookupExpressions.Add(Expression.Call(miGetFieldOrdinal, new Expression[] { prmSqlRdr, Expression.Constant(parameterName, typeof(string)) }));

            var expAssign = Expression.Assign(expOrdinal, Expression.ArrayAccess(expOrdinals, new[] { Expression.Constant(propIndex, typeof(int)) }));
            expressions.Add(expAssign);
            logger.SqlExpressionLog(expAssign);

            if (propertyType.IsEnum)
            {
                var expIf = Expression.IfThen(
                    Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
                    Expression.Assign(expProperty, Expression.Convert(expGetField, propertyType))
                    );
                expressions.Add(expIf);
                logger.SqlExpressionLog(expIf);
            }
            else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var miIsDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull));
                var expIf = Expression.IfThen(
                    Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
                    Expression.IfThenElse(
                           //if
                           Expression.Call(prmSqlRdr, miIsDbNull, new[] { expOrdinal }),
                           //then
                           Expression.Assign(expProperty, Expression.Constant(null, propertyType)),
                           //else
                           Expression.Assign(expProperty, Expression.Convert(expGetField, propertyType))
                           )
                    );
                expressions.Add(expIf);
                logger.SqlExpressionLog(expIf);
            }
            else
            {
                var expIf = Expression.IfThen(
                    Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
                    Expression.Assign(expProperty, expGetField)
                    );
                expressions.Add(expIf);
                logger.SqlExpressionLog(expIf);
            }
        }

        //long, bit, ...
        public static void ReaderSimpleValueExpressions(string parameterName, Expression expProperty, IList<MethodCallExpression> columnLookupExpressions, IList<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
            var miGetTypedFieldValue = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue)).MakeGenericMethod(propertyType);
            var expGetField = Expression.Call(prmSqlRdr, miGetTypedFieldValue, new[] { expOrdinal });
            var miGetFieldOrdinal = typeof(ShardMapper<>).GetMethod(nameof(GetFieldOrdinal), BindingFlags.NonPublic | BindingFlags.Static);
            columnLookupExpressions.Add(Expression.Call(miGetFieldOrdinal, new Expression[] { prmSqlRdr, Expression.Constant(parameterName, typeof(string)) }));

            var expAssign = Expression.Assign(expOrdinal, Expression.ArrayAccess(expOrdinals, new[] { Expression.Constant(propIndex, typeof(int)) }));
            expressions.Add(expAssign);
            logger.SqlExpressionLog(expAssign);

            if ((propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) || (propertyType.IsArray))
            {
                var miIsDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull));
                var expIf = Expression.IfThen(
                    Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
                    Expression.IfThenElse(
                           //if
                           Expression.Call(prmSqlRdr, miIsDbNull, new[] { expOrdinal }),
                           //then
                           Expression.Assign(expProperty, Expression.Constant(null, propertyType)),
                           //else
                           Expression.Assign(expProperty, expGetField)
                           )
                    );
                expressions.Add(expIf);
                logger.SqlExpressionLog(expIf);
            }
            else
            {
                var expIf = Expression.IfThen(
                    Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
                    Expression.Assign(expProperty, expGetField)
                    );
                expressions.Add(expIf);
                logger.SqlExpressionLog(expIf);
            }
        }

        public static void ReaderFloatingPointExpressions(string parameterName, MemberExpression expProperty, ConstantExpression expNullResult, IList<MethodCallExpression> columnLookupExpressions, IList<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
            var miGetTypedFieldValue = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue)).MakeGenericMethod(propertyType);
            var expGetField = Expression.Call(prmSqlRdr, miGetTypedFieldValue, new[] { expOrdinal });
            var miGetFieldOrdinal = typeof(ShardMapper<>).GetMethod(nameof(GetFieldOrdinal), BindingFlags.NonPublic | BindingFlags.Static);
            columnLookupExpressions.Add(Expression.Call(miGetFieldOrdinal, new Expression[] { prmSqlRdr, Expression.Constant(parameterName, typeof(string)) }));

            var expAssign = Expression.Assign(expOrdinal, Expression.ArrayAccess(expOrdinals, new[] { Expression.Constant(propIndex, typeof(int)) }));
            expressions.Add(expAssign);
            logger.SqlExpressionLog(expAssign);

            var miIsDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull));

            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var expIf = Expression.IfThen(
                    Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
                    Expression.IfThenElse(
                           //if
                           Expression.Call(prmSqlRdr, miIsDbNull, new[] { expOrdinal }),
                           //then
                           Expression.Assign(expProperty, Expression.Constant(null, propertyType)),
                           //else
                           Expression.Assign(expProperty, expGetField)
                           )
                    );
                expressions.Add(expIf);
                logger.SqlExpressionLog(expIf);
            }
            else
            {
                var expIf = Expression.IfThen(
                    Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
                    Expression.IfThenElse(
                           //if
                           Expression.Call(prmSqlRdr, miIsDbNull, new[] { expOrdinal }),
                           //then
                           Expression.Assign(expProperty, expNullResult),
                           //else
                           Expression.Assign(expProperty, expGetField)
                           )
                    );
                expressions.Add(expIf);
                logger.SqlExpressionLog(expIf);
            }
        }
        #endregion

        //public static Expression PropertyReadShardKeyValue(MemberExpression propValue, Type propertyType, ParameterMapAttribute.ShardUsage shardPosition, Type nullableBaseType)
        //{
        //    //ShardKey? requires sub properties of Nullable<value> (or null can't propagate)
        //    string shdPosName;
        //    switch (shardPosition)
        //    {
        //        case ParameterMapAttribute.ShardUsage.IsShardNumber:
        //            shdPosName = nameof(ShardKey<int, int>.ShardNumber);
        //            break;
        //        case ParameterMapAttribute.ShardUsage.IsRecordId:
        //            shdPosName = nameof(ShardKey<int, int>.RecordID);
        //            break;
        //        case ParameterMapAttribute.ShardUsage.IsConcurrencyStamp:
        //            shdPosName = nameof(ShardKey<int, int>.ConcurrencyStamp);
        //            break;
        //        default:
        //            throw new System.Exception("");
        //    }

        //    if (propertyType == typeof(Nullable<>) && Nullable.GetUnderlyingType(propertyType) == typeof(ShardKey<,>))
        //    {
          //    }
        //    else //if (propertyType == typeof(ShardKey<,>))
        //    {
        //        var piShardPos = propertyType.GetProperty(shdPosName);
        //        return Expression.Property(propValue, piShardPos);
        //    }
        //}
        //private static string ShardPropertyName(ParameterMapAttribute.ShardUsage shardPosition)
        //{
        //    string shdPosName;
        //    switch (shardPosition)
        //    {
        //        case ParameterMapAttribute.ShardUsage.IsShardNumber:
        //            shdPosName = nameof(ShardKey<int, int>.ShardNumber);
        //            break;
        //        case ParameterMapAttribute.ShardUsage.IsRecordId:
        //            shdPosName = nameof(ShardKey<int, int>.RecordID);
        //            break;
        //        case ParameterMapAttribute.ShardUsage.IsConcurrencyStamp:
        //            shdPosName = nameof(ShardKey<int, int>.ConcurrencyStamp);
        //            break;
        //        default:
        //            throw new System.Exception("Invalid shard position specified.");
        //    }
        //    return shdPosName;
        //}
        //private static Type GetShardPropertyType(ParameterMapAttribute.ShardUsage shardPosition, Type shardType, bool makeNullable)
        //{
        //    var types = shardType.GetGenericArguments();
        //    Type result;
        //    switch (shardPosition)
        //    {
        //        case ParameterMapAttribute.ShardUsage.IsShardNumber:
        //            result = types[0];
        //            break;
        //        case ParameterMapAttribute.ShardUsage.IsRecordId:
        //            result = types[1];
        //            break;
        //        case ParameterMapAttribute.ShardUsage.IsChildId:
        //            if (types.Length > 2)
        //            {
        //                result = types[2];
        //            }
        //            else
        //            {
        //                throw new Exception("ShardChild position specified for ShardKey (which doesn't have a Shard Child).");
        //            }
        //            break;
        //        case ParameterMapAttribute.ShardUsage.IsConcurrencyStamp:
        //            result = typeof(DateTime);
        //            break;
        //        default:
        //            throw new System.Exception("Invalid shard position specified.");
        //    }
        //    if (makeNullable)
        //    {
        //        var nulType = typeof(Nullable<>);
        //        if (result.IsValueType && result != nulType)
        //        {
        //            result = nulType.MakeGenericType(result);
        //        }
        //    }
        //    return result;
        //}


		//public static BlockExpression MakeShardKeyExpressions<TShard, TRecord>(string propertyName, MemberExpression expProperty, ParameterExpression expVarShardNumber, ParameterExpression expVarRecordId, ParameterExpression expVarTimeStamp, LabelTarget expExit, List<Expression> blkExpressions, char originSource, Type propertyType, ParameterExpression expLogger, ILogger logger) where TShard : IComparable where TRecord : IComparable
		//{
		//    Type[] constructorTypes;
		//    Expression[] constructorValues;
		//    ParameterExpression expVarResult;
		//    var varExpressions = new List<ParameterExpression>();
		//    varExpressions.Add(expVarShardNumber);
		//    varExpressions.Add(expVarRecordId);
		//    var piGetByteValue = typeof(Nullable<byte>).GetProperty(nameof(Nullable<byte>.Value));
		//    var piGetIntValue = typeof(Nullable<int>).GetProperty(nameof(Nullable<int>.Value));

		//    var expDataOrigin = Expression.New(typeof(DataOrigin).GetConstructor(new[] { typeof(char) }), new[] { Expression.Constant(originSource, typeof(char)) });

		//    if (!(expVarTimeStamp is null))
		//    {
		//        varExpressions.Add(expVarTimeStamp);
		//        constructorTypes = new[] { typeof(DataOrigin), typeof(byte), typeof(int), typeof(byte[]) };
		//        constructorValues = new Expression[] { expDataOrigin, Expression.Property(expVarShardNumber, piGetByteValue), Expression.Property(expVarRecordId, piGetIntValue), expVarTimeStamp };
		//    }
		//    else
		//    {
		//        constructorTypes = new[] { typeof(DataOrigin), typeof(byte), typeof(int) };
		//        constructorValues = new Expression[] { expDataOrigin, Expression.Property(expVarShardNumber, piGetByteValue), Expression.Property(expVarRecordId, piGetIntValue) };
		//    }
		//    ConstantExpression expNullShardKey;
		//    NewExpression expNewShardKey;
		//    //if (propertyType == typeof(ShardKey<TShard, TRecord>?))
		//    if (propertyType == typeof(Nullable<>) && Nullable.GetUnderlyingType(propertyType) == typeof(ShardKey<,>))
		//    {
		//        expNullShardKey = Expression.Constant(null, propertyType);
		//        expNewShardKey = Expression.New(typeof(ShardKey<TShard, TRecord>?).GetConstructor(new[] { typeof(ShardKey<TShard, TRecord>) }), Expression.New(typeof(ShardKey<TShard, TRecord>).GetConstructor(constructorTypes), constructorValues));
		//    }
		//    else
		//    {
		//        expNullShardKey = Expression.Constant(ShardKey<TShard, TRecord>.Empty, propertyType);
		//        expNewShardKey = Expression.New(typeof(ShardKey<TShard, TRecord>).GetConstructor(constructorTypes), constructorValues);
		//    }
		//    expVarResult = Expression.Variable(propertyType, "result");
		//    varExpressions.Add(expVarResult);

		//    //var expHasValue = Expression.Condition(
		//    //    Expression.Property(expVarRecordId, typeof(int?).GetProperty(nameof(Nullable<int>.HasValue))),
		//    //        Expression.Condition(
		//    //            Expression.Property(expVarShardNumber, typeof(byte?).GetProperty(nameof(Nullable<byte>.HasValue))),
		//    //            Expression.Assign(expVarResult, expNewShardKey),
		//    //            Expression.Assign(expVarResult, expNullShardKey)),
		//    //        Expression.Assign(expVarResult, expNullShardKey));



		//    var expHasValue = Expression.Condition(
		//        Expression.AndAlso(Expression.Property(expVarShardNumber, typeof(byte?).GetProperty(nameof(Nullable<byte>.HasValue))),
		//                       Expression.Property(expVarRecordId, typeof(int?).GetProperty(nameof(Nullable<int>.HasValue)))),
		//        Expression.Assign(expVarResult, expNewShardKey),
		//            //Expression.Block(new Expression[] {
		//            Expression.Assign(expVarResult, expNullShardKey)
		//        //Expression.Call(typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.NullShardKeyArguments)), new Expression[] { expLogger, expVarShardNumber, expVarRecordId })
		//        //})
		//        );
		//    blkExpressions.Add(expHasValue);
		//    logger.SqlExpressionLog(expHasValue);


		//    var expLogNull = Expression.Call(typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.NullShardKeyArguments)), new Expression[] { expLogger, Expression.Constant(propertyName, typeof(string)), expVarShardNumber, expVarRecordId });
		//    blkExpressions.Add(expLogNull);
		//    logger.SqlExpressionLog(expLogNull);
		//    var expAssignResult = Expression.Assign(expProperty, expVarResult);
		//    blkExpressions.Add(expAssignResult);
		//    logger.SqlExpressionLog(expAssignResult);
		//    var expExitBlock = Expression.Label(expExit);
		//    blkExpressions.Add(expExitBlock);
		//    logger.SqlExpressionLog(expExitBlock);
		//    blkExpressions.Add(expVarResult);
		//    logger.SqlExpressionLog(expVarResult);
		//    return Expression.Block(varExpressions, blkExpressions);
		//}
		//public static BlockExpression MakeShardChildExpressions<TShard, TRecord, TChild>(string propertyName, MemberExpression expProperty, ParameterExpression expVarShardNumber, ParameterExpression expVarRecordId, ParameterExpression expVarChildId, ParameterExpression expVarTimeStamp, LabelTarget expExit, List<Expression> blkExpressions, char originSource, Type propertyType, ParameterExpression expLogger, ILogger logger) where TShard: IComparable where TRecord: IComparable where TChild: IComparable
		//{
		//    Type[] shardConstructorTypes;
		//    Expression[] shardConstructorValues;
		//    var varExpressions = new List<ParameterExpression>();
		//    varExpressions.Add(expVarShardNumber);
		//    varExpressions.Add(expVarRecordId);
		//    varExpressions.Add(expVarChildId);
		//    var piGetByteValue = typeof(Nullable<byte>).GetProperty(nameof(Nullable<byte>.Value));
		//    var piGetIntValue = typeof(Nullable<int>).GetProperty(nameof(Nullable<int>.Value));

		//    var expDataOrigin = Expression.New(typeof(DataOrigin).GetConstructor(new[] { typeof(char) }), new[] { Expression.Constant(originSource, typeof(char)) });

		//    if (!(expVarTimeStamp is null))
		//    {
		//        varExpressions.Add(expVarTimeStamp);
		//        shardConstructorTypes = new[] { typeof(DataOrigin), typeof(byte), typeof(int), typeof(byte[]) };
		//        shardConstructorValues = new Expression[] { expDataOrigin, Expression.Property(expVarShardNumber, piGetByteValue), Expression.Property(expVarRecordId, piGetIntValue), expVarTimeStamp };
		//    }
		//    else
		//    {
		//        shardConstructorTypes = new[] { typeof(DataOrigin), typeof(byte), typeof(int) };
		//        shardConstructorValues = new Expression[] { expDataOrigin, Expression.Property(expVarShardNumber, piGetByteValue), Expression.Property(expVarRecordId, piGetIntValue) };
		//    } 
		//    var expNewShardKey = Expression.New(typeof(ShardKey<TShard, TRecord>).GetConstructor(shardConstructorTypes), shardConstructorValues);
		//    var expVarResult = Expression.Variable(propertyType, "result");
		//    varExpressions.Add(expVarResult);

		//    var expIfHasValue = Expression.AndAlso(Expression.Property(expVarShardNumber, typeof(byte?).GetProperty(nameof(Nullable<byte>.HasValue))),
		//                           Expression.AndAlso(
		//                                Expression.Property(expVarRecordId, typeof(int?).GetProperty(nameof(Nullable<int>.HasValue))),
		//                                Expression.Property(expVarChildId, typeof(short?).GetProperty(nameof(Nullable<short>.HasValue)))));
		//    var expNewChild = Expression.New(typeof(ShardChild<TShard, TRecord, TChild>).GetConstructor(new[] { typeof(ShardKey<TShard, TRecord>), typeof(short) }),
		//                new Expression[] { expNewShardKey,
		//                    Expression.Property(expVarChildId, typeof(Nullable<short>).GetProperty(nameof(Nullable<short>.Value))) });



		//    if (propertyType == typeof(ShardChild<TShard, TRecord, TChild>?))
		//    {
		//        var expIf = Expression.Condition(expIfHasValue,
		//            Expression.Assign(expVarResult, Expression.New(typeof(ShardChild<TShard, TRecord, TChild>?).GetConstructor(new[] { typeof(ShardChild<TShard, TRecord, TChild>) }), new[] { expNewChild })),
		//            Expression.Assign(expVarResult, Expression.Constant(null, propertyType))
		//        );
		//        blkExpressions.Add(expIf);
		//        logger.SqlExpressionLog(expIf);
		//    }
		//    else
		//    {
		//        var expIf = Expression.Condition(expIfHasValue,
		//            Expression.Assign(expVarResult, expNewChild),
		//            Expression.Assign(expVarResult, Expression.Constant(ShardChild<TShard, TRecord, TChild>.Empty, propertyType))
		//        );
		//        blkExpressions.Add(expIf);
		//        logger.SqlExpressionLog(expIf);
		//    }
		//    var expHasValue = Expression.Call(typeof(SqlLoggerExtensions).GetMethod(nameof(SqlLoggerExtensions.NullShardChildArguments)), new Expression[] { expLogger, Expression.Constant(propertyName, typeof(string)), expVarShardNumber, expVarRecordId, expVarChildId });
		//    blkExpressions.Add(expHasValue);
		//    logger.SqlExpressionLog(expHasValue);
		//    var expAssignResult = Expression.Assign(expProperty, expVarResult);
		//    blkExpressions.Add(expAssignResult);
		//    logger.SqlExpressionLog(expAssignResult);

		//    var expExitBlock = Expression.Label(expExit);
		//    blkExpressions.Add(expExitBlock);
		//    logger.SqlExpressionLog(expExitBlock);
		//    blkExpressions.Add(expVarResult);
		//    logger.SqlExpressionLog(expVarResult);
		//    return Expression.Block(varExpressions, blkExpressions);
		//}
		public static bool DontIgnoreThisParameter(string parameterName, HashSet<string> ignoreParameters)
        {
            return !(!(ignoreParameters is null) && !ignoreParameters.Contains(parameterName));
        }

        public static string ToFieldName(string parameterName)
        {
            if (!string.IsNullOrEmpty(parameterName) && parameterName.StartsWith("@"))
            {
                parameterName = parameterName.Substring(1);
            }
            return parameterName;
        }
        public static string ToParameterName(string parameterName)
        {
            if (!string.IsNullOrEmpty(parameterName) && !parameterName.StartsWith("@"))
            {
                parameterName = "@" + parameterName;
            }
            return parameterName;
        }
        //Return null if not found (rather than error, as DbParameterCollection does)
        public static DbParameter GetParameter(DbParameterCollection prms, string fieldName)
        {
            fieldName = ToParameterName(fieldName);
            for (int i = 0; i < prms.Count; i++)
            {
                if (fieldName == prms[i].ParameterName)
                {
                    return prms[i];
                }
            }
            var comparer = System.Globalization.CultureInfo.InvariantCulture.CompareInfo;
            CompareOptions co = CompareOptions.IgnoreCase;
            for (int i = 0; i < prms.Count; i++)
            {
                if (comparer.Compare(fieldName, prms[i].ParameterName, co) == 0)
                {
                    return prms[i];
                }
            }
            co = CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth;
            for (int i = 0; i < prms.Count; i++)
            {
                if (comparer.Compare(fieldName, prms[i].ParameterName, co) == 0)
                {
                    return prms[i];
                }
            }
            return null;
        }
        //TODO: Not DRY (also in Mapper)
        //Return -1 if not found (rather than error, as rdr.GetOrdinal does), otherwise identical code.
        public static int GetFieldOrdinal(DbDataReader rdr, string fieldName)
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
    }
}
