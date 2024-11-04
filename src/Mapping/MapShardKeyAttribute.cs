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



    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public abstract class MapShardKeyAttribute : Attribute
    {
        ParameterMapAttributeBase _shardId;

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
        /// <param name="shardId">Optional information about shard data to be passed to the database about this record.</param>
        /// <param name="origin">The origin char value representing the type of value.</param>
        /// <param name="recordIdName">The name of the data column, which must exactly match the database attribute.</param>
        /// <param name="childIdName">The name of the optional 2nd data column, if a compound key, which must exactly match the corresponding database attribute.</param>
        /// <param name="grandChildIdName">The name of the optional 3rd data column, if a compound key, which must exactly match the corresponding database attribute.</param>
        /// <param name="greatGrandChildIdName">The name of the optional 4th data column, if a compound key, which must exactly match the corresponding database attribute.</param>
        /// <param name="isRecordIdentifier">A optional value which indicates whether the property is the record identifier. Defaults to True if not set.</param>
        public MapShardKeyAttribute(ParameterMapAttributeBase shardId, char origin, string recordIdName, string childIdName, string grandChildIdName, string greatGrandChildIdName, bool isRecordIdentifier)
        {
            this.Origin = origin;

            _shardId = shardId;
            this.RecordIdName = recordIdName;
            this.ChildIdName = childIdName;
            this.GrandChildIdName = grandChildIdName;
            this.GreatGrandChildIdName = greatGrandChildIdName;
            this.IsRecordIdentifier = isRecordIdentifier;
        }

        public char Origin { get; set; }

		public virtual string ShardIdName {
            get {
                return this._shardId?.ColumnName;
            }
        }

		public virtual string RecordIdName { get; }

        public virtual string ChildIdName { get; }

        public virtual string GrandChildIdName { get; }

        public virtual string GreatGrandChildIdName { get; }

        public virtual bool IsRecordIdentifier { get; }

        public ParameterMapAttributeBase ShardParameter
        {
            get { return _shardId;  }
        }
    }
}
