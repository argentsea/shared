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
    public abstract class MapShardKeyAttribute : Attribute
    {
        ParameterMapAttributeBase _shardId;
        //public MapShardKeyAttribute(char origin, string recordIdName)
        //{
        //    this.Origin = origin;
        //    this.ShardIdName = null;
        //    this.RecordIdName = recordIdName;
        //    this.ChildIdName = null;
        //    this.GrandChildIdName = null;
        //    this.GreatGrandChildIdName = null;
        //}

        //public MapShardKeyAttribute(string shardIdName, char origin, string recordIdName)
        //{
        //    this.Origin = origin;
        //    this.ShardIdName = shardIdName;
        //    this.RecordIdName = recordIdName;
        //    this.ChildIdName = null;
        //    this.GrandChildIdName = null;
        //    this.GreatGrandChildIdName = null;
        //}

        //public MapShardKeyAttribute(char origin, string recordIdName, string childIdName)
        //{
        //    this.Origin = origin;
        //    this.ShardIdName = null;
        //    this.RecordIdName = recordIdName;
        //    this.ChildIdName = childIdName;
        //    this.GrandChildIdName = null;
        //    this.GreatGrandChildIdName = null;
        //}

        //public MapShardKeyAttribute(string shardIdName, char origin, string recordIdName, string childIdName)
        //{
        //    this.Origin = origin;
        //    this.ShardIdName = shardIdName;
        //    this.RecordIdName = recordIdName;
        //    this.ChildIdName = childIdName;
        //    this.GrandChildIdName = null;
        //    this.GreatGrandChildIdName = null;
        //}

        //public MapShardKeyAttribute(char origin, string recordIdName, string childIdName, string grandChildIdName)
        //{
        //    this.Origin = origin;
        //    this.ShardIdName = null;
        //    this.RecordIdName = recordIdName;
        //    this.ChildIdName = childIdName;
        //    this.GrandChildIdName = null;
        //    this.GreatGrandChildIdName = null;
        //}

        //public MapShardKeyAttribute(string shardIdName, char origin, string recordIdName, string childIdName, string grandChildIdName)
        //{
        //    this.Origin = origin;
        //    this.ShardIdName = shardIdName;
        //    this.RecordIdName = recordIdName;
        //    this.ChildIdName = childIdName;
        //    this.GrandChildIdName = grandChildIdName;
        //    this.GreatGrandChildIdName = grandChildIdName;
        //}

        //public MapShardKeyAttribute(char origin, string recordIdName, string childIdName, string grandChildIdName, string greatGrandChildIdName)
        //{
        //    this.Origin = origin;
        //    this.ShardIdName = null;
        //    this.RecordIdName = recordIdName;
        //    this.ChildIdName = childIdName;
        //    this.GrandChildIdName = grandChildIdName;
        //    this.GreatGrandChildIdName = greatGrandChildIdName;
        //}

        public MapShardKeyAttribute(ParameterMapAttributeBase shardId, char origin, string recordIdName, string childIdName, string grandChildIdName, string greatGrandChildIdName)
        {
            this.Origin = origin;

            _shardId = shardId;
            this.RecordIdName = recordIdName;
            this.ChildIdName = childIdName;
            this.GrandChildIdName = grandChildIdName;
            this.GreatGrandChildIdName = greatGrandChildIdName;
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

        public ParameterMapAttributeBase ShardParameter
        {
            get { return _shardId;  }
        }
    }
}
