// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace ArgentSea
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public abstract class ParameterMapAttributeBase : Attribute
    {
        public ParameterMapAttributeBase(string name, int sqlType)
        {
            ParameterName = name;
            ColumnName = name;
            Name = name;
            SqlType = sqlType;
			IsRequired = false;
        }
		public ParameterMapAttributeBase(string name, int sqlType, bool isRequired)
		{
            ParameterName = name;
            ColumnName = name;
            Name = name;
            SqlType = sqlType;
			IsRequired = isRequired;
		}

        public string Name { get; private set; }

        public virtual string ParameterName { get; private set; }

        public virtual string ColumnName { get; private set; }

        public int SqlType { get; private set; }

		public bool IsRequired { get; private set; }

        //public ShardUsage ShardPosition { get; set; } = ShardUsage.NotApplicable;

        public abstract bool IsValidType(Type candidate);

        protected internal abstract void AppendInParameterExpressions(IList<Expression> expressions, ParameterExpression expSprocParameters, ParameterExpression expIgnoreParameters, HashSet<string> parameterNames, Expression expProperty, Type propertyType, ParameterExpression expLogger, ILogger logger);

        protected internal abstract void AppendSetOutParameterExpressions(IList<Expression> expressions, ParameterExpression expSprocParameters, ParameterExpression expIgnoreParameters, HashSet<string> parameterNames, ParameterExpression expLogger, ILogger logger);

        protected internal abstract void AppendReadOutParameterExpressions(Expression expProperty, IList<Expression> expressions, ParameterExpression expSprocParameters, ParameterExpression expPrm, Type propertyType, ParameterExpression expLogger, ILogger logger);

        protected internal abstract void AppendReaderExpressions(Expression expProperty, IList<MethodCallExpression> columnLookupExpressions, IList<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, Type propertyType, ParameterExpression expLogger, ILogger logger);

    }
}
