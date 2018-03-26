using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace ArgentSea
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public abstract class ParameterMapAttribute : Attribute
    {
        //public enum ShardUsage
        //{
        //    NotApplicable,
        //    IsShardNumber,
        //    IsRecordId,
        //    IsChildId,
        //    IsConcurrencyStamp
        //}

        public ParameterMapAttribute(string parameterName, SqlDbType sqlType)
        {
            ParameterName = parameterName;
            SqlType = sqlType;
        }
        public string ParameterName { get; private set; }
        public SqlDbType SqlType { get; private set; }

        //public ShardUsage ShardPosition { get; set; } = ShardUsage.NotApplicable;

        public abstract bool IsValidType(Type candidate);

        protected internal abstract void AppendInParameterExpressions(IList<Expression> expressions, ParameterExpression prms, ParameterExpression expIgnoreParameters, HashSet<string> parameterNames, MemberExpression expProperty, Type propertyType, ParameterExpression expLogger, ILogger logger);

        protected internal abstract void AppendSetOutParameterExpressions(IList<Expression> expressions, ParameterExpression prms, ParameterExpression expIgnoreParameters, HashSet<string> parameterNames, Type propertyType, ParameterExpression expLogger, ILogger logger);

        protected internal abstract void AppendReadOutParameterExpressions(Expression expProperty, IList<Expression> expressions, ParameterExpression expPrms, ParameterExpression expPrm, PropertyInfo propertyInfo, ParameterExpression expLogger, ILogger logger);

        protected internal abstract void AppendReaderExpressions(MemberExpression expProperty, IList<MethodCallExpression> columnLookupExpressions, IList<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, PropertyInfo propertyInfo, ParameterExpression expLogger, ILogger logger);

        //protected internal abstract void AppendTvpExpressions(ParameterExpression expRecord, MemberExpression expProperty, IList<Expression> setExpressions, IList<NewExpression> sqlMetaDataTypeExpressions, HashSet<string> parameterNames, ref int ordinal, Type propertyType, ParameterExpression expLogger, ILogger logger);

    }
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class MapToModel : Attribute
    {

    }

}
