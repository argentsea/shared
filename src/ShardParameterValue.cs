// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace ArgentSea
{
    public class ShardParameterValue<TShard> where TShard : IComparable
    {
        public TShard ShardId { get; set; }

        public string parameterName { get; set; }

        public object Value { get; set; }
    }
}
