// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System.Data;
using System.Runtime.CompilerServices;

namespace ArgentSea
{
    public class Statement : Query
    {
        public Statement(string sql, string[] parameterNames, [CallerMemberName] string name = "")
            : base(sql, name, parameterNames)
        {

        }
        public override CommandType Type { get => CommandType.Text; }
    }
}
