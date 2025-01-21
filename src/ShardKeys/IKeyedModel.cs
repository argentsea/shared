// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{

    public interface IKeyedModel<TRecord> where TRecord : IComparable
    {
        ShardKey<TRecord> Key { get; }
    }

    public interface IKeyedModel<TRecord, TChild> where TRecord : IComparable where TChild : IComparable
    {
        ShardKey<TRecord, TChild> Key { get; }
    }

    public interface IKeyedModel<TRecord, TChild, TGrandChild> where TRecord : IComparable where TChild : IComparable where TGrandChild : IComparable
    {
        ShardKey<TRecord, TChild, TGrandChild> Key { get; }
    }

    public interface IKeyedModel<TRecord, TChild, TGrandChild, TGreatGrandChild> where TRecord : IComparable where TChild : IComparable where TGrandChild : IComparable where TGreatGrandChild : IComparable
    {
        ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> Key { get; }
    }
}
