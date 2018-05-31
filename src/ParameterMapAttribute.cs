﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace ArgentSea
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public abstract class ParameterMapAttribute : Attribute
    {
        //public enum ShardUsage
        //{
        //    NotApplicable,
        //    IsShardId,
        //    IsRecordId,
        //    IsChildId,
        //    IsConcurrencyStamp
        //}

        public ParameterMapAttribute(string parameterName, int sqlType)
        {
            ParameterName = parameterName;
            SqlType = sqlType;
			IsRequired = false;
        }
		public ParameterMapAttribute(string parameterName, int sqlType, bool isRequired)
		{
			ParameterName = parameterName;
			SqlType = sqlType;
			IsRequired = isRequired;
		}
		public string ParameterName { get; private set; }

		public int SqlType { get; private set; }

		public bool IsRequired { get; private set; }

        //public ShardUsage ShardPosition { get; set; } = ShardUsage.NotApplicable;

        public abstract bool IsValidType(Type candidate);

        protected internal abstract void AppendInParameterExpressions(IList<Expression> expressions, ParameterExpression expSprocParameters, ParameterExpression expIgnoreParameters, HashSet<string> parameterNames, MemberExpression expProperty, Type propertyType, ParameterExpression expLogger, ILogger logger);

        protected internal abstract void AppendSetOutParameterExpressions(IList<Expression> expressions, ParameterExpression expSprocParameters, ParameterExpression expIgnoreParameters, HashSet<string> parameterNames, Type propertyType, ParameterExpression expLogger, ILogger logger);

        protected internal abstract void AppendReadOutParameterExpressions(Expression expProperty, IList<Expression> expressions, ParameterExpression expSprocParameters, ParameterExpression expPrm, Type propertyType, ParameterExpression expLogger, ILogger logger);

        protected internal abstract void AppendReaderExpressions(Expression expProperty, IList<MethodCallExpression> columnLookupExpressions, IList<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, PropertyInfo propertyInfo, ParameterExpression expLogger, ILogger logger);

        //protected internal abstract void AppendTvpExpressions(ParameterExpression expRecord, MemberExpression expProperty, IList<Expression> setExpressions, IList<NewExpression> sqlMetaDataTypeExpressions, HashSet<string> parameterNames, ref int ordinal, Type propertyType, ParameterExpression expLogger, ILogger logger);

    }
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class MapToModel : Attribute
    {

    }

}
