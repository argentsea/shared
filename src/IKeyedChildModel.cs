// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    public interface IKeyedChildModel<TShard, TRecord, TChild> where TShard : IComparable where TRecord : IComparable where TChild : IComparable
    {
        ShardChild<TShard, TRecord, TChild> Key { get; }
    }
}
