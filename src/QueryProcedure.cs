// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System.Data;
using System.Runtime.CompilerServices;

namespace ArgentSea
{
    public class QueryProcedure : Query
    {
        public QueryProcedure(string sprocName)
            : base(sprocName, sprocName, null)
        {

        }
        public QueryProcedure(string sprocName, string[] parameterNames)
            : base(sprocName, sprocName, parameterNames)
        {

        }
        public override CommandType Type { get => CommandType.StoredProcedure; }
    }
}
