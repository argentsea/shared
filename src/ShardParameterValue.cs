using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace ArgentSea
{
    public class ShardParameterValues<TShard> where TShard : IComparable
    {
        public TShard ShardId { get; set; }

        public string parameterName { get; set; }

        public object Value { get; set; }
    }
}
