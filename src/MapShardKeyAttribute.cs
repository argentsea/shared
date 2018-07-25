﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;

namespace ArgentSea
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MapShardKeyAttribute : Attribute
    {
        public MapShardKeyAttribute(DataOrigin origin, string shardIdName, string recordIdName)
		{
			this.Origin = origin;
			this.ShardIdName = shardIdName;
			this.RecordIdName = recordIdName;
		}
        public MapShardKeyAttribute(char originValue, string shardIdName, string recordIdName)
        {
            this.Origin = new DataOrigin(originValue);
            this.ShardIdName = shardIdName;
            this.RecordIdName = recordIdName;
        }
        public MapShardKeyAttribute(char originValue, string recordIdName)
        {
            this.Origin = new DataOrigin(originValue);
            this.ShardIdName = null;
            this.RecordIdName = recordIdName;
        }
        public MapShardKeyAttribute(DataOrigin origin, string recordIdName)
        {
            this.Origin = origin;
            this.ShardIdName = null;
            this.RecordIdName = recordIdName;
        }

        public DataOrigin Origin { get; set; }

		public virtual string ShardIdName { get; set; }

		public virtual string RecordIdName { get; set; }

	}
}