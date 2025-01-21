// © John Hicks. All rights reserved. Licensed under the MIT license.
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
using System.ComponentModel;

namespace ArgentSea
{
    /// <summary>
    /// This class is used by provider specific implementations. It is unlikely that you would reference this in consumer code.
    /// </summary>
    public static class ExpressionHelpers
    {
		#region Database call expressions
		public static Expression InParmHelper(string parameterName, ParameterExpression expSprocParameters, Expression propValue, Type staticType, string addMethod, ConstantExpression thirdArg, ConstantExpression forthArg, ParameterExpression expIgnoreParameters)
        {
            List<Type> methodSignatureTypes = new List<Type>(); //to get correct method overload
            List<Expression> methodArgumentExpressions = new List<Expression>(); //parameter, variable, or constant expressions
            var expParameterName = Expression.Constant(parameterName, typeof(string));
            //var expShoudIgnore = Expression.Call(typeof(Mapper).GetMethod(nameof(IgnoreThisParameter), new[] { typeof(string), typeof(HashSet<string>) }), new Expression[] { expParameterName, expIgnoreParameters });
            var expShouldIgnore = Expression.Call(typeof(ExpressionHelpers).GetMethod(nameof(DontIgnoreThisParameter), BindingFlags.Static | BindingFlags.NonPublic), new Expression[] { expParameterName, expIgnoreParameters });

            methodSignatureTypes.Add(typeof(DbParameterCollection)); //Arg0: prms parameter in this signature
            methodSignatureTypes.Add(typeof(string)); //Arg1: parameterName parameter in this signature
            methodSignatureTypes.Add(propValue.Type); //Arg2: <value> parameter in this signature

            methodArgumentExpressions.Add(expSprocParameters);
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

        public static void OutParameterBuilder(string parameterName, ParameterExpression expSprocParameters, IList<Expression> expressions, Type staticType, string addMethod, ConstantExpression secondArg, ConstantExpression thirdArg, HashSet<string> parameterNames, ParameterExpression expIgnoreParameters, ILogger logger)
        {
            if (parameterNames.Add(parameterName))
            {
                List<Type> methodSignatureTypes = new List<Type>(); //to get correct method overload
                List<Expression> methodArgumentExpressions = new List<Expression>(); //parameter, variable, or constant expressions
                var expParameterName = Expression.Constant(parameterName, typeof(string));
                var expShouldIgnore = Expression.Call(typeof(ExpressionHelpers).GetMethod(nameof(DontIgnoreThisParameter), BindingFlags.Static | BindingFlags.NonPublic), new Expression[] { expParameterName, expIgnoreParameters });

                methodSignatureTypes.Add(typeof(DbParameterCollection)); //Arg0: prms parameter in this signature
                methodSignatureTypes.Add(typeof(string)); //Arg1: parameterName parameter in this signature
                methodArgumentExpressions.Add(expSprocParameters);
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
                expressions.Add(Expression.IfThen(expShouldIgnore, expCall));
            }
        }
		#endregion
		#region Expression builders by type
		public static void InParameterStringExpressionBuilder(string parameterName, int length, Type staticType, string methodName, ConstantExpression expLocale, IList<Expression> expressions, ParameterExpression expSprocParameters, ParameterExpression expIgnoreParameters, HashSet<string> parameterNames, Expression propValue, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
            if (parameterNames.Add(parameterName))
            {
                if (propertyType.IsEnum)
                {
                    var miEnumToString = typeof(Enum).GetMethod(nameof(Enum.ToString), new Type[] { });
                    var expEnumToString = Expression.Call(propValue, miEnumToString);
                    expressions.Add(InParmHelper(parameterName, expSprocParameters, expEnumToString, staticType, methodName, Expression.Constant(length, typeof(int)), expLocale, expIgnoreParameters));

                }
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
                    expressions.Add(InParmHelper(parameterName, expSprocParameters, expIf, staticType, methodName, Expression.Constant(length, typeof(int)), expLocale, expIgnoreParameters));
                }
                else
                {
                    expressions.Add(InParmHelper(parameterName, expSprocParameters, propValue, staticType, methodName, Expression.Constant(length, typeof(int)), expLocale, expIgnoreParameters));
                }
            }
        }

        public static void InParameterEnumXIntExpressionBuilder(string parameterName, Type staticType, string addMethodName, Type nullableBaseType, IList<Expression> expressions, ParameterExpression expSprocParameters, ParameterExpression expIgnoreParameters, HashSet<string> parameterNames, Expression propValue, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
            if (parameterNames.Add(parameterName))
            {
                if (propertyType.IsEnum)
                {
                    var expConvert = Expression.Convert(propValue, Nullable.GetUnderlyingType(nullableBaseType));
                    expressions.Add(InParmHelper(parameterName, expSprocParameters, expConvert, staticType, addMethodName, null, null, expIgnoreParameters));
                }
				else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && Nullable.GetUnderlyingType(propertyType).IsEnum) //Nullable Enum
                {

                    var piNullableHasValue = propertyType.GetProperty(nameof(Nullable<int>.HasValue));
                    var piNullableGetValue = propertyType.GetProperty(nameof(Nullable<int>.Value));

                    var expIf = Expression.Condition(
                        Expression.Property(propValue, piNullableHasValue),
                       Expression.Convert(Expression.Property(propValue, piNullableGetValue), nullableBaseType),
                        Expression.Constant(null, nullableBaseType)
                        );
                    expressions.Add(InParmHelper(parameterName, expSprocParameters, expIf, staticType, addMethodName, null, null, expIgnoreParameters));
                }
                else
                {
                    expressions.Add(InParmHelper(parameterName, expSprocParameters, propValue, staticType, addMethodName, null, null, expIgnoreParameters));
                }
            }
        }
        public static void InParameterSimpleBuilder(string parameterName, Type propertyType, ParameterExpression expSprocParameters, ParameterExpression expIgnoreParameters, Expression expProperty, IList<Expression> expressions, Type staticType, string addMethod, ConstantExpression thirdArg, ConstantExpression forthArg, HashSet<string> parameterNames, ParameterExpression expLogger, ILogger logger)
        {
            if (parameterNames.Add(parameterName))
            {
				expressions.Add(InParmHelper(parameterName, expSprocParameters, expProperty, staticType, addMethod, thirdArg, forthArg, expIgnoreParameters));

            }
        }

		public static void ReadOutParameterStringExpressions(string parameterName, Type staticType, string getMethodName, Expression expProperty, IList<Expression> expressions, ParameterExpression expPrms, ParameterExpression expPrm, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {

            var miGetString = staticType.GetMethod(getMethodName);
            var expGetString = Expression.Call(miGetString, expPrm);
            if (propertyType.IsEnum)
            {
                var miEnumParse = typeof(Enum).GetMethod(nameof(Enum.Parse), new[] { typeof(Type), typeof(string) });
                var expSet = Expression.Assign(expProperty, Expression.Convert(Expression.Call(miEnumParse, Expression.Constant(propertyType, typeof(Type)), Expression.Call(miGetString, expPrm)), propertyType));
				expressions.Add(Expression.IfThenElse(Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))),
                    expSet,
                    Expression.Call(typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyType, typeof(Type)) })));
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
				expressions.Add(Expression.IfThenElse(Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))),
                    Expression.Assign(expProperty, expIf),
                    Expression.Call(typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyType.ReflectedType, typeof(Type)) })));
            }
            else
            {
                expressions.Add(Expression.IfThen(Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))), Expression.Assign(expProperty, expGetString)));
            }
        }

        public static void ReadOutParameterEnumXIntExpressions(string parameterName, Type staticType, string getMethodName, string nullableGetMethodName, Expression expProperty, IList<Expression> expressions, ParameterExpression expPrms, ParameterExpression expPrm, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
            //var miGetParameter = typeof(ExpressionHelpers).GetMethod(nameof(ExpressionHelpers.GetParameter), BindingFlags.Static | BindingFlags.NonPublic);
            //var expAssign = Expression.Assign(expPrm, Expression.Call(miGetParameter, expPrms, Expression.Constant(parameterName, typeof(string))));
            //expressions.Add(expAssign);
            //logger?.SqlExpressionLog(expAssign);

            if (propertyType.IsEnum)
            {
                var miGet = staticType.GetMethod(getMethodName);
                var expGet = Expression.Call(miGet, expPrm);
                var expIf = Expression.IfThenElse(Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))),
                    Expression.Assign(expProperty, Expression.Convert(expGet, propertyType)),
                    Expression.Call(typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyType, typeof(Type)) }));
                expressions.Add(expIf);
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
                        Expression.Call(typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyType, typeof(Type)) }));
                    expressions.Add(expIf);
                }
                else
                {
                    var expIf = Expression.IfThenElse(Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))),
                        Expression.Assign(expProperty, expGetNl),
                        Expression.Call(typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyType, typeof(Type)) }));
                    expressions.Add(expIf);
                }
            }
            else
            {
                var miGet = staticType.GetMethod(getMethodName);
                var expGet = Expression.Call(miGet, expPrm);
                var expIf = Expression.IfThenElse(Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))),
                    Expression.Assign(expProperty, expGet),
                    Expression.Call(typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyType, typeof(Type)) })
                    );
                expressions.Add(expIf);
            }
        }

        public static void ReadOutParameterSimpleValueExpressions(string parameterName, Type staticType, string getMethodName, string nullableGetMethodName, Expression expProperty, IList<Expression> expressions, ParameterExpression expPrms, ParameterExpression expPrm, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
            //var miGetParameter = typeof(ExpressionHelpers).GetMethod(nameof(GetParameter), BindingFlags.Static | BindingFlags.NonPublic);
            //var expAssign = Expression.Assign(expPrm, Expression.Call(miGetParameter, expPrms, Expression.Constant(parameterName, typeof(string))));
            //expressions.Add(expAssign);
            //logger?.SqlExpressionLog(expAssign);

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
                Expression.Call(typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyType, typeof(Type)) })
                );
            expressions.Add(expIf);
        }
        public static void ReadOutParameterBinaryExpressions(string parameterName, Type staticType, string getMethodName, Expression expProperty, IList<Expression> expressions, ParameterExpression expPrms, ParameterExpression expPrm, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
            var expSet = Expression.Assign(expProperty, Expression.Call(staticType.GetMethod(getMethodName), expPrm));
            var expIf = Expression.IfThenElse(
                Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))),
                expSet,
                Expression.Call(typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyType, typeof(Type)) })
                );
            expressions.Add(expIf);
        }

        public static void ReadOutParameterArrayExpressions(string parameterName, Type staticType, string getMethodName, Expression expProperty, IList<Expression> expressions, ParameterExpression expPrms, ParameterExpression expPrm, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
            if (!propertyType.IsArray)
            {
                throw new Exception("The property must be an array type to have an array attribute.");
            }
            Type genericType = propertyType.GetElementType();
            var mi = staticType.GetMethod(getMethodName).MakeGenericMethod(genericType);

            var expSet = Expression.Assign(expProperty, Expression.Call(mi, expPrm));
            var expIf = Expression.IfThenElse(
                Expression.NotEqual(expPrm, Expression.Constant(null, typeof(DbParameter))),
                expSet,
                Expression.Call(typeof(LoggingExtensions).GetMethod(nameof(LoggingExtensions.SqlParameterNotFound)), new Expression[] { expLogger, Expression.Constant(parameterName, typeof(string)), Expression.Constant(propertyType, typeof(Type)) })
                );
            expressions.Add(expIf);
        }


        public static void ReaderStringExpressions(string columnName, Expression expProperty, IList<MethodCallExpression> columnLookupExpressions, IList<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
            var miGetTypedFieldValue = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetString));
            var expGetField = Expression.Call(prmSqlRdr, miGetTypedFieldValue, new[] { expOrdinal });

            var miGetFieldOrdinal = typeof(ExpressionHelpers).GetMethod(nameof(ExpressionHelpers.GetFieldOrdinal), BindingFlags.NonPublic | BindingFlags.Static);
            columnLookupExpressions.Add(Expression.Call(miGetFieldOrdinal, new Expression[] { prmSqlRdr, Expression.Constant(columnName, typeof(string)), Expression.Constant(propertyType.ToString(), typeof(string)), expLogger }));
            expressions.Add(Expression.Assign(expOrdinal, Expression.ArrayAccess(expOrdinals, new[] { Expression.Constant(propIndex, typeof(int)) })));
            propIndex++;

            if (propertyType.IsEnum)
            {
                var miEnumParse = typeof(Enum).GetMethod(nameof(Enum.Parse), new[] { typeof(Type), typeof(string) });
                var expEnumAssign = Expression.Assign(expProperty, Expression.Convert(Expression.Call(miEnumParse, new Expression[] { Expression.Constant(propertyType, typeof(Type)), expGetField }), propertyType));

				expressions.Add(Expression.IfThen(
                    Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
                           expEnumAssign
                    ));
            }
            else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && Nullable.GetUnderlyingType(propertyType).IsEnum)
            {
                var miEnumParse = typeof(Enum).GetMethod(nameof(Enum.Parse), new[] { typeof(Type), typeof(string) });
                var expEnumParse = Expression.Convert(Expression.Call(miEnumParse, Expression.Constant(Nullable.GetUnderlyingType(propertyType), typeof(Type)), expGetField), propertyType);
                var expEnumAssign = Expression.Assign(expProperty, expEnumParse);

                var miIsDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull));
				expressions.Add(Expression.IfThen(
                    Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
                    Expression.IfThenElse(
                           //if
                           Expression.Call(prmSqlRdr, miIsDbNull, new[] { expOrdinal }),
                           //then
                           Expression.Assign(expProperty, Expression.Constant(null, propertyType)),
                           //else
                           expEnumAssign
                           )
                    ));
            }
            else
            {
                var miIsDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull));
				expressions.Add(Expression.IfThen(
                    Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
                    Expression.IfThenElse(
                           //if
                           Expression.Call(prmSqlRdr, miIsDbNull, new[] { expOrdinal }),
                           //then
                           Expression.Assign(expProperty, Expression.Constant(null, propertyType)),
                           //else
                           Expression.Assign(expProperty, expGetField)
                           )
                    ));
            }
        }

        public static void ReaderEnumXIntExpressions(string columnName, Expression expProperty, Type baseType, IList<MethodCallExpression> columnLookupExpressions, IList<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
			//This approach won't work because the base type in the DB might be different than the base type for the enum.
			//Type baseType = propertyType;
			//if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
			//{
			//	baseType = Nullable.GetUnderlyingType(propertyType);
			//}
			//if (baseType.IsEnum)
			//{
			//	baseType = Enum.GetUnderlyingType(baseType);
			//}
			var miGetTypedFieldValue = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue)).MakeGenericMethod(baseType);
            var expGetField = Expression.Call(prmSqlRdr, miGetTypedFieldValue, new[] { expOrdinal });
            var miGetFieldOrdinal = typeof(ExpressionHelpers).GetMethod(nameof(ExpressionHelpers.GetFieldOrdinal), BindingFlags.NonPublic | BindingFlags.Static);
            columnLookupExpressions.Add(Expression.Call(miGetFieldOrdinal, new Expression[] { prmSqlRdr, Expression.Constant(columnName, typeof(string)), Expression.Constant(propertyType.ToString(), typeof(string)), expLogger }));

            expressions.Add(Expression.Assign(expOrdinal, Expression.ArrayAccess(expOrdinals, new[] { Expression.Constant(propIndex, typeof(int)) })));
            propIndex++;

            if (propertyType.IsEnum)
            {
                expressions.Add(Expression.IfThen(
					Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
					Expression.Assign(expProperty, Expression.Convert(expGetField, propertyType))
					));
            }
            else if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var miIsDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull));
                expressions.Add(Expression.IfThen(
					Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
					Expression.IfThenElse(
						   //if
						   Expression.Call(prmSqlRdr, miIsDbNull, new[] { expOrdinal }),
						   //then
						   Expression.Assign(expProperty, Expression.Constant(null, propertyType)),
						   //else
						   Expression.Assign(expProperty, Expression.Convert(expGetField, propertyType))
						   )
					));
            }
            else
            {
                expressions.Add(Expression.IfThen(
					Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
					Expression.Assign(expProperty, expGetField)
					));
            }
        }

        //long, bit, array...
        public static void ReaderSimpleValueExpressions(string columnName, Expression expProperty, IList<MethodCallExpression> columnLookupExpressions, IList<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
            var miGetFieldOrdinal = typeof(ExpressionHelpers).GetMethod(nameof(ExpressionHelpers.GetFieldOrdinal), BindingFlags.NonPublic | BindingFlags.Static);
            columnLookupExpressions.Add(Expression.Call(miGetFieldOrdinal, new Expression[] { prmSqlRdr, Expression.Constant(columnName, typeof(string)), Expression.Constant(propertyType.ToString(), typeof(string)), expLogger }));

            expressions.Add(Expression.Assign(expOrdinal, Expression.ArrayAccess(expOrdinals, new[] { Expression.Constant(propIndex, typeof(int)) })));
            propIndex++;

            if ((propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) || (propertyType.IsArray))
            {
				var baseType = propertyType;
				if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					baseType = Nullable.GetUnderlyingType(propertyType);
				}
				var miGetTypedFieldValue = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue)).MakeGenericMethod(baseType);
				Expression expGetField = Expression.Call(prmSqlRdr, miGetTypedFieldValue, new[] { expOrdinal });
				if (baseType != propertyType)
				{
					expGetField = Expression.Convert(expGetField, propertyType);
				}
				var miIsDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull));
                expressions.Add(Expression.IfThen(
					Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
					Expression.IfThenElse(
						   //if
						   Expression.Call(prmSqlRdr, miIsDbNull, new[] { expOrdinal }),
						   //then
						   Expression.Assign(expProperty, Expression.Constant(null, propertyType)),
						   //else
						   Expression.Assign(expProperty, expGetField)
						   )
					));
            }
            else
            {
				var miGetTypedFieldValue = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue)).MakeGenericMethod(propertyType);
				var expGetField = Expression.Call(prmSqlRdr, miGetTypedFieldValue, new[] { expOrdinal });
                expressions.Add(Expression.IfThen(
					Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
					Expression.Assign(expProperty, expGetField)
					));
            }
        }
		//double (NaN), single (NaN), Guid (Guid.Empty)
		public static void ReaderNullableValueTypeExpressions(string columnName, Expression expProperty, ConstantExpression expNullResult, IList<MethodCallExpression> columnLookupExpressions, IList<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, Type propertyType, ParameterExpression expLogger, ILogger logger)
        {
			Type baseType = propertyType;
			if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				baseType = Nullable.GetUnderlyingType(propertyType);
			}
			if (baseType.IsEnum)
			{
				baseType = Enum.GetUnderlyingType(baseType);
			}
			var miGetTypedFieldValue = typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue)).MakeGenericMethod(baseType);
			Expression expGetField = Expression.Call(prmSqlRdr, miGetTypedFieldValue, new[] { expOrdinal });
			var miGetFieldOrdinal = typeof(ExpressionHelpers).GetMethod(nameof(ExpressionHelpers.GetFieldOrdinal), BindingFlags.NonPublic | BindingFlags.Static);
            columnLookupExpressions.Add(Expression.Call(miGetFieldOrdinal, new Expression[] { prmSqlRdr, Expression.Constant(columnName, typeof(string)), Expression.Constant(propertyType.ToString(), typeof(string)), expLogger }));

            expressions.Add(Expression.Assign(expOrdinal, Expression.ArrayAccess(expOrdinals, new[] { Expression.Constant(propIndex, typeof(int)) })));
            propIndex++;
            var miIsDbNull = typeof(DbDataReader).GetMethod(nameof(DbDataReader.IsDBNull));

            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
				if (baseType != propertyType)
				{
					expGetField = Expression.Convert(expGetField, propertyType);
				}
                expressions.Add(Expression.IfThen(
					Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
					Expression.IfThenElse(
						   //if
						   Expression.Call(prmSqlRdr, miIsDbNull, new[] { expOrdinal }),
						   //then
						   Expression.Assign(expProperty, Expression.Constant(null, propertyType)),
						   //else
						   Expression.Assign(expProperty, expGetField)
						   )
					));
            }
            else
            {
                expressions.Add(Expression.IfThen(
					Expression.NotEqual(expOrdinal, Expression.Constant(-1, typeof(int))),
					Expression.IfThenElse(
						   //if
						   Expression.Call(prmSqlRdr, miIsDbNull, new[] { expOrdinal }),
						   //then
						   Expression.Assign(expProperty, expNullResult),
						   //else
						   Expression.Assign(expProperty, expGetField)
						   )
					));
            }
        }
        #endregion

		internal static bool DontIgnoreThisParameter(string parameterName, HashSet<string> ignoreParameters)
        {
			return !((ignoreParameters is null) || ignoreParameters.Contains(parameterName));
        }

        internal static void TryInstantiateMapToModel(PropertyInfo prop, Expression expProperty, IList<Expression> expressions)
        {
            if (prop.CanWrite)
            {
                expressions.Add(Expression.IfThen(Expression.Equal(expProperty, Expression.Constant(null)), Expression.Assign(expProperty, Expression.New(prop.PropertyType))));
            }
        }

        //Return null if not found (rather than error, as DbParameterCollection does)
        internal static DbParameter GetParameter(DbParameterCollection parameters, string parameterName)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameterName == parameters[i].ParameterName)
                {
                    return parameters[i];
                }
            }
            var comparer = System.Globalization.CultureInfo.InvariantCulture.CompareInfo;
            CompareOptions co = CompareOptions.IgnoreCase;
            for (int i = 0; i < parameters.Count; i++)
            {
                if (comparer.Compare(parameterName, parameters[i].ParameterName, co) == 0)
                {
                    return parameters[i];
                }
            }
            co = CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth;
            for (int i = 0; i < parameters.Count; i++)
            {
                if (comparer.Compare(parameterName, parameters[i].ParameterName, co) == 0)
                {
                    return parameters[i];
                }
            }
            return null;
        }
        //Return -1 if not found (rather than error, as rdr.GetOrdinal does), otherwise essentially identical code.
        internal static int GetFieldOrdinal(DbDataReader rdr, string fieldName, string typeName, ILogger logger)
        {
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
            logger.SqlFieldNotFound(fieldName, typeName);
            return -1;
        }
		internal static bool IsRequiredParameterDbNull(DbParameter prm, string modelName, string parameterName, ILogger logger)
		{
			if (prm is null)
			{
				throw new Exception($"The parameter {parameterName}, which was requested for model {modelName}, is a null object. This indicates a code problem, as it should be DbNull or have a value.");
			}
			if (DBNull.Value.Equals(prm.Value))
			{
				logger?.RequiredPropertyIsDbNull(modelName, parameterName);
				return true;
			}
			return false;
		}
        internal static bool IsRequiredColumnDbNull(DbDataReader rdr, int ordinal, string modelName, ILogger logger)
        {
            if (rdr is null)
            {
                throw new Exception($"The data reader provide is null.");
            }
            //if (DBNull.Value.Equals(rdr?.IsDBNull(ordinal)))
            if (rdr.IsDBNull(ordinal))
            {
                logger?.RequiredPropertyIsDbNull(modelName, rdr.GetName(ordinal));
                return true;
            }
            return false;
        }
        internal static Expression ReturnNullIfPrmNull(ParameterExpression expPrm, string parameterName, Type tModel, LabelTarget exitLabel, Expression expLogger)
		{
			//Add quick exit if this parameter is dbNull
			var miGetIsDbNull = typeof(ExpressionHelpers).GetMethod(nameof(ExpressionHelpers.IsRequiredParameterDbNull), BindingFlags.Static | BindingFlags.NonPublic);
			var expIsDbNull = Expression.Call(miGetIsDbNull, new Expression[] { expPrm, Expression.Constant(tModel.ToString(), typeof(string)), Expression.Constant(parameterName, typeof(string)), expLogger });
			return Expression.IfThen(expIsDbNull, Expression.Return(exitLabel, Expression.Default(tModel))); //Expression.Constant(null, tModel)));0
		}
        internal static Expression ReturnNullIfColNull(ParameterExpression expRdr, ParameterExpression expOrdinal, Type tModel, LabelTarget exitLabel, Expression expLogger)
        {
            //Add quick exit if this parameter is dbNull
            var miGetIsDbNull = typeof(ExpressionHelpers).GetMethod(nameof(ExpressionHelpers.IsRequiredColumnDbNull), BindingFlags.Static | BindingFlags.NonPublic);
            var expIsDbNull = Expression.Call(miGetIsDbNull, new Expression[] { expRdr, expOrdinal, Expression.Constant(tModel.ToString(), typeof(string)), expLogger });
            return Expression.IfThen(expIsDbNull, Expression.Return(exitLabel, Expression.Default(tModel))); //Expression.Constant(null, tModel)));
        }

        public static void GetShardKeyType(PropertyInfo prop, Type propType, ParameterMapAttributeBase attrPM, MemberExpression childProperty)
        {
            var tDataId = propType.GetGenericArguments()[1];
            if (!attrPM.IsValidType(tDataId))
            {
                throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
            }

        }
        public static MapShardKeyAttribute GetMapShardKeyAttribute(PropertyInfo prop, Type propType, out bool isNullable, out bool isShardKey, out bool isShardChild, out bool isShardGrandChild, out bool isShardGreatGrandChild)
        {
            if (!propType.IsGenericType)
            {
                isNullable = false;
                isShardKey = false;
                isShardChild = false;
                isShardGrandChild = false;
                isShardGreatGrandChild = false;
                return null;
            }
            isNullable = propType.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (isNullable)
            {
                propType = Nullable.GetUnderlyingType(propType);
                if (!propType.IsGenericType)
                {
                    isShardKey = false;
                    isShardChild = false;
                    isShardGrandChild = false;
                    isShardGreatGrandChild = false;
                    return null;
                }
            }
            var typeName = propType.Name;
            if (Attribute.IsDefined(prop, typeof(MapShardKeyAttribute)))
            {
                isShardKey = typeName == "ShardKey`1";
                isShardChild = typeName == "ShardKey`2";
                isShardGrandChild = typeName == "ShardKey`3";
                isShardGreatGrandChild = typeName == "ShardKey`4";
                return prop.GetCustomAttribute<MapShardKeyAttribute>(true);
            }
            else
            {
                isShardKey = false;
                isShardChild = false;
                isShardGrandChild = false;
                isShardGreatGrandChild = false;
                return null;
            }

        }

        /// <summary>
        /// Method make public to share the ShardKey rendering logic with libraries such as ArgentSea.Orleans. Not intended for client use.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool ShardKeyInMapProperties(PropertyInfo prop, Type propType, MapShardKeyAttribute shdAttr, bool isNullable, bool isShardKey, bool isShardChild, bool isShardGrandChild, bool isShardGreatGrandChild, Expression expProperty, List<Expression> expressions, List<ParameterExpression> variables, ParameterExpression prmSqlPrms, ParameterExpression expIgnoreParameters, ParameterExpression expLogger, HashSet<string> noDupPrmNameList, MethodInfo miLogTrace, ref bool foundPrms, ILogger logger)
        {
            Expression expOriginalProperty = expProperty;

            var foundRecordId = false;
            var foundChildId = false;
            var foundGrandChildId = false;
            var foundGreatGrandChildId = false;
            expressions.Add(Expression.Call(miLogTrace, expLogger, Expression.Constant(prop.Name)));

            Expression expDetectNullOrEmpty;
            if (isNullable)
            {
                expProperty = Expression.Property(expProperty, propType.GetProperty(nameof(Nullable<int>.Value)));
                propType = Nullable.GetUnderlyingType(propType);
                expDetectNullOrEmpty = Expression.Property(expOriginalProperty, prop.PropertyType.GetProperty(nameof(Nullable<int>.HasValue)));
            }
            else
            {
                expDetectNullOrEmpty = Expression.NotEqual(expOriginalProperty, Expression.Property(null, propType.GetProperty(nameof(ShardKey<int>.Empty))));
            }


            if (shdAttr.ShardParameter is not null)
            {
                var expShardProperty = Expression.Property(expProperty, propType.GetProperty(nameof(ShardKey<int>.ShardId)));
                ParameterExpression expNullableShardId = Expression.Variable(typeof(Nullable<>).MakeGenericType(typeof(short)), prop.Name + "_NullableShardId");
                variables.Add(expNullableShardId);
                expressions.Add(Expression.IfThenElse(
                    expDetectNullOrEmpty,
                    Expression.Assign(expNullableShardId, Expression.Convert(expShardProperty, expNullableShardId.Type)),
                    Expression.Assign(expNullableShardId, Expression.Constant(null, expNullableShardId.Type))
                    ));
                shdAttr.ShardParameter.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expNullableShardId, expNullableShardId.Type, expLogger, logger);
            }

            var attrPMs = prop.GetCustomAttributes<ParameterMapAttributeBase>(true);

            foreach (var attrPM in attrPMs)
            {
                if (attrPM.Name == shdAttr.RecordIdName)
                {
                    foundRecordId = true;
                    var tDataRecordId = propType.GetGenericArguments()[0];
                    if (!attrPM.IsValidType(tDataRecordId))
                    {
                        throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
                    }
                    var expRecordProperty = Expression.Property(expProperty, propType.GetProperty(nameof(ShardKey<int>.RecordId)));
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
                    foundPrms = true;
                }
                if ((isShardChild || isShardGrandChild || isShardGreatGrandChild) && attrPM.Name == shdAttr.ChildIdName)
                {
                    foundChildId = true;

                    var tDataId = propType.GetGenericArguments()[1];
                    if (!attrPM.IsValidType(tDataId))
                    {
                        throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
                    }
                    var expChildProperty = Expression.Property(expProperty, propType.GetProperty(nameof(ShardKey<int, int>.ChildId)));
                    ParameterExpression expNullableChildId;
                    if (tDataId.IsValueType)
                    {
                        expNullableChildId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tDataId), prop.Name + "_NullableChildId");
                    }
                    else
                    {
                        expNullableChildId = Expression.Variable(tDataId, prop.Name + "_NullableChildId");
                    }
                    variables.Add(expNullableChildId);
                    expressions.Add(Expression.IfThenElse(
                        expDetectNullOrEmpty,
                        Expression.Assign(expNullableChildId, Expression.Convert(expChildProperty, expNullableChildId.Type)),
                        Expression.Assign(expNullableChildId, Expression.Constant(null, expNullableChildId.Type))
                        ));
                    attrPM.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expNullableChildId, expNullableChildId.Type, expLogger, logger);
                    foundPrms = true;
                }
                if ((isShardGrandChild || isShardGreatGrandChild) && attrPM.Name == shdAttr.GrandChildIdName)
                {
                    foundGrandChildId = true;
                    var tDataId = propType.GetGenericArguments()[2];
                    if (!attrPM.IsValidType(tDataId))
                    {
                        throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
                    }
                    var expChildProperty = Expression.Property(expProperty, propType.GetProperty(nameof(ShardKey<int, int, int>.GrandChildId)));
                    ParameterExpression expNullableChildId;
                    if (tDataId.IsValueType)
                    {
                        expNullableChildId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tDataId), prop.Name + "_NullableGrandChildId");
                    }
                    else
                    {
                        expNullableChildId = Expression.Variable(tDataId, prop.Name + "_NullableGrandChildId");
                    }
                    variables.Add(expNullableChildId);
                    expressions.Add(Expression.IfThenElse(
                        expDetectNullOrEmpty,
                        Expression.Assign(expNullableChildId, Expression.Convert(expChildProperty, expNullableChildId.Type)),
                        Expression.Assign(expNullableChildId, Expression.Constant(null, expNullableChildId.Type))
                        ));
                    attrPM.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expNullableChildId, expNullableChildId.Type, expLogger, logger);
                    foundPrms = true;
                }
                if (isShardGreatGrandChild && attrPM.Name == shdAttr.GreatGrandChildIdName)
                {
                    foundGreatGrandChildId = true;
                    var tDataId = propType.GetGenericArguments()[3];
                    if (!attrPM.IsValidType(tDataId))
                    {
                        throw new InvalidMapTypeException(prop, attrPM.SqlType, attrPM.SqlTypeName);
                    }
                    var expChildProperty = Expression.Property(expProperty, propType.GetProperty(nameof(ShardKey<int, int, int, int>.GreatGrandChildId)));
                    ParameterExpression expNullableChildId;
                    if (tDataId.IsValueType)
                    {
                        expNullableChildId = Expression.Variable(typeof(Nullable<>).MakeGenericType(tDataId), prop.Name + "_NullableGreatGrandChildId");
                    }
                    else
                    {
                        expNullableChildId = Expression.Variable(tDataId, prop.Name + "_NullableGreatGrandChildId");
                    }
                    variables.Add(expNullableChildId);
                    expressions.Add(Expression.IfThenElse(
                        expDetectNullOrEmpty,
                        Expression.Assign(expNullableChildId, Expression.Convert(expChildProperty, expNullableChildId.Type)),
                        Expression.Assign(expNullableChildId, Expression.Constant(null, expNullableChildId.Type))
                        ));
                    attrPM.AppendInParameterExpressions(expressions, prmSqlPrms, expIgnoreParameters, noDupPrmNameList, expNullableChildId, expNullableChildId.Type, expLogger, logger);
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
            return true;
        }

    }
}
