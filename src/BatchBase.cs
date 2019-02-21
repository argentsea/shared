// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Common;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ArgentSea
{
    public abstract class BatchBase<TShard, TResult> : ICollection, IEnumerable<BatchStep<TShard, TResult>> where TShard : IComparable
    {
        private readonly object syncRoot = new object();
        protected readonly List<BatchStep<TShard, TResult>> _processes = new List<BatchStep<TShard, TResult>>();

        public bool IsSynchronized => true;

        public object SyncRoot => syncRoot;

        public BatchStep<TShard, TResult> this[int index] => _processes[index];

        public void CopyTo(Array array, int index)
            => _processes.CopyTo((BatchStep<TShard, TResult>[])array, index);

        public int Count => _processes.Count;

        public IEnumerator GetEnumerator() => _processes.GetEnumerator();

        IEnumerator<BatchStep<TShard, TResult>> IEnumerable<BatchStep<TShard, TResult>>.GetEnumerator() => _processes.GetEnumerator();

        abstract internal protected Task<TResult> Execute(TShard shardId, DbConnection connection, DbTransaction transaction, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken);

        public bool Remove(BatchStep<TShard, TResult> item) => _processes.Remove(item);

        public void Clear()
        {
            _processes.Clear();
        }
    }
}
