// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System.Data;

namespace ArgentSea
{
    public abstract class Query
    {
        private readonly string _sql;
        private readonly string[] _parameterNames;
        private readonly string _name;

        public Query(string sql, string name, string[] parameterNames)
        {
            _sql = sql;
            _parameterNames = parameterNames;
            _name = name;
        }
        public string Sql { get => _sql; }

        public string Name { get => _name; }

        public string[] ParameterNames { get => _parameterNames;  }

        public abstract CommandType Type { get; }
    }
}
