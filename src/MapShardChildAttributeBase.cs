using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace ArgentSea
{
	public abstract class MapShardChildAttributeBase : Attribute
	{
		public MapShardChildAttributeBase(DataOrigin origin, string shardIdParameterName, string recordIdParameterName, string childIdParameterName)
		{
			this.Origin = origin;
			this.ShardIdParameterName = shardIdParameterName;
			this.RecordIdParameterName = recordIdParameterName;
			this.ChildIdParameterName = childIdParameterName;
		}

		public DataOrigin Origin { get; set; }

		public virtual string ShardIdParameterName { get; set; }

		public virtual string RecordIdParameterName { get; set; }

		public virtual string ChildIdParameterName { get; set; }

		public virtual string ShardIdFieldName { get; set; }

		public virtual string RecordIdFieldName { get; set; }

		public virtual string ChildIdFieldName { get; set; }
	}
}
