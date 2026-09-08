// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace ArgentSea
{
    /// <summary>
    /// Base attribute for mapping collection properties to provider-specific structured parameters
    /// (e.g., SQL table-valued parameters). The element type uses standard MapToSql* attributes
    /// for column mapping.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public abstract class CollectionMapAttributeBase : Attribute
    {
        protected CollectionMapAttributeBase(string parameterName)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }

        /// <summary>
        /// Appends expression tree nodes that add the collection as a structured parameter.
        /// Called by the Mapper's IterateInMapProperties when this attribute is detected.
        /// </summary>
        public abstract void AppendCollectionInParameterExpressions(
            List<Expression> expressions,
            ParameterExpression prmSqlPrms,
            Expression expCollection,
            Type elementType,
            ParameterExpression expLogger,
            ILogger logger);
    }
}
