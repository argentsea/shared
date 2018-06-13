using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace ArgentSea
{
	public class MapShardChildAttribute : Attribute
	{
		public MapShardChildAttribute(DataOrigin origin, string shardIdName, string recordIdName, string childIdName)
		{
			this.Origin = origin;
			this.ShardIdName = shardIdName;
			this.RecordIdName = recordIdName;
			this.ChildIdName = childIdName;
		}

        public MapShardChildAttribute(char originValue, string shardIdName, string recordIdName, string childIdName)
        {
            this.Origin = new DataOrigin(originValue);
            this.ShardIdName = shardIdName;
            this.RecordIdName = recordIdName;
            this.ChildIdName = childIdName;
        }
        public MapShardChildAttribute(DataOrigin origin, string recordIdName, string childIdName)
        {
            this.Origin = origin;
            this.ShardIdName = null;
            this.RecordIdName = recordIdName;
            this.ChildIdName = childIdName;
        }

        public MapShardChildAttribute(char originValue, string recordIdName, string childIdName)
        {
            this.Origin = new DataOrigin(originValue);
            this.ShardIdName = null;
            this.RecordIdName = recordIdName;
            this.ChildIdName = childIdName;
        }

        public DataOrigin Origin { get; set; }

		public virtual string ShardIdName { get; set; }

		public virtual string RecordIdName { get; set; }

		public virtual string ChildIdName { get; set; }

	}
}
