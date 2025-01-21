// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace ArgentSea
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public abstract class ParameterMapAttributeBase : Attribute
    {
        /// <summary>
        /// Provide property mapping metadata to a database parameter or column.
        /// </summary>
        /// <param name="name">That name of the colum and/or parameter that this should map to.</param>
        /// <param name="sqlType">The (provider specific) database type to use.</param>
        public ParameterMapAttributeBase(string name, int sqlType)
        {
            ParameterName = name;
            ColumnName = name;
            Name = name;
            SqlType = sqlType;
            IsRecordIdentifier = false;
        }

        /// <summary>
        /// Provide property mapping metadata to a database parameter or column.
        /// </summary>
        /// <param name="name">That name of the colum and/or parameter that this should map to.</param>
        /// <param name="sqlType">The (provider specific) database type to use.</param>
        /// <param name="isRecordKey">Indicates that if this column or result is null, the entire object result should be null because the record itself was not found. Typically this is set on key columns because they are never null unless the record does not exist. </param>
		public ParameterMapAttributeBase(string name, int sqlType, bool isRecordIdentifier)
		{
            ParameterName = name;
            ColumnName = name;
            Name = name;
            SqlType = sqlType;
            IsRecordIdentifier = isRecordIdentifier;
		}
       
        public string Name { get; private set; }

        public virtual string ParameterName { get; private set; }

        public virtual string ColumnName { get; private set; }

        public int SqlType { get; private set; }

        public virtual string SqlTypeName  { get; private set; }

        public bool IsRecordIdentifier { get; private set; }

        public abstract bool IsValidType(Type candidate);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract void AppendInParameterExpressions(IList<Expression> expressions, ParameterExpression expSprocParameters, ParameterExpression expIgnoreParameters, HashSet<string> parameterNames, Expression expProperty, Type propertyType, ParameterExpression expLogger, ILogger logger);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract void AppendSetOutParameterExpressions(IList<Expression> expressions, ParameterExpression expSprocParameters, ParameterExpression expIgnoreParameters, HashSet<string> parameterNames, ParameterExpression expLogger, ILogger logger);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract void AppendReadOutParameterExpressions(Expression expProperty, IList<Expression> expressions, ParameterExpression expSprocParameters, ParameterExpression expPrm, Type propertyType, ParameterExpression expLogger, ILogger logger);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract void AppendReaderExpressions(Expression expProperty, IList<MethodCallExpression> columnLookupExpressions, IList<Expression> expressions, ParameterExpression prmSqlRdr, ParameterExpression expOrdinals, ParameterExpression expOrdinal, ref int propIndex, Type propertyType, ParameterExpression expLogger, ILogger logger);
    }
}
