// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    public interface IKeyedChildModel<TRecord, TChild> where TRecord : IComparable where TChild : IComparable
    {
        ShardChild<TRecord, TChild> Key { get; }
    }
}
