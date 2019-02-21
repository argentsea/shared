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
    /// This property attribute is used to map multiple paramters to a ShardChild object.
    /// This recordIdName attribute and childIdName attributes must exactly match the names of the corresponding MapTo attributes which are also on the same property.
    /// </summary>
    /// <example>
    /// For example, you could implement the mapping for a ShardChild property like this:
    /// <code>
    /// [MapShardChild('C', "ParentRecordId", "ChildRecordId")]
    /// [MapToSqlSmallInt("ParentRecordId")]
    /// [MapToSqlNVarChar("ChildRecordId", 255)]
    /// public ShardChild&lt;byte, short, string&gt;? ChildShard2 { get; set; } = null;
    /// </code>
    /// </example>
	public class MapShardChildAttribute : Attribute
	{
        public MapShardChildAttribute(char origin, string shardIdName, string recordIdName, string childIdName)
        {
            this.Origin = origin;
            this.ShardIdName = shardIdName;
            this.RecordIdName = recordIdName;
            this.ChildIdName = childIdName;
        }

        public MapShardChildAttribute(char origin, string recordIdName, string childIdName)
        {
            this.Origin = origin;
            this.ShardIdName = null;
            this.RecordIdName = recordIdName;
            this.ChildIdName = childIdName;
        }

        public char Origin { get; set; }

		public virtual string ShardIdName { get; set; }

		public virtual string RecordIdName { get; set; }

		public virtual string ChildIdName { get; set; }

	}
}
