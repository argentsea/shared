// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace ArgentSea
{
    /// <summary>
    /// This property attribute is used to map multiple paramters to a ShardKey object.
    /// This recordIdName attribute name must exactly match the names of the corresponding MapTo attributes which are also on the same property.
    /// </summary>
    /// <example>
    /// For example, you could implement the mapping for a ShardKey property like this:
    /// <code>
    /// [MapShardKey('U', "RecordId")]
    /// [MapToSqlInt("RecordId")]
    /// public ShardKey&lt;int&gt;? Id { get; set; } = null;
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MapShardKeyAttribute : Attribute
    {
        public MapShardKeyAttribute(char origin, string shardIdName, string recordIdName)
        {
            this.Origin = origin;
            this.ShardIdName = shardIdName;
            this.RecordIdName = recordIdName;
        }
        public MapShardKeyAttribute(char origin, string recordIdName)
        {
            this.Origin = origin;
            this.ShardIdName = null;
            this.RecordIdName = recordIdName;
        }

        public char Origin { get; set; }

		public virtual string ShardIdName { get; set; }

		public virtual string RecordIdName { get; set; }

	}
}
