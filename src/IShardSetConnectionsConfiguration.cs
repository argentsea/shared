// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    /// <summary>
    /// This interface is used by provider specific implementations. It is unlikely that you would implement this in consumer code.
    /// </summary>
    /// <typeparam name="TShard"></typeparam>
	public interface IShardSetConnectionsConfiguration<TShard> where TShard : IComparable
    {
        string ShardSetName { get; set; }
        TShard DefaultShardId { get; set; }
        IShardConnectionConfiguration<TShard>[] ShardsInternal { get; }
    }
}
