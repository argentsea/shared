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
    /// <summary>
    /// A batch enables a series of commands to execute under the same transaction.
    /// For example, you would need a batch to bulk load data into temporary tables prior to processing them in subsequent steps.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    public abstract class BatchBase<TResult> : ICollection, IEnumerable<BatchStep<TResult>>
    {
        private readonly object syncRoot = new object();
        protected readonly List<BatchStep<TResult>> _processes = new List<BatchStep<TResult>>();

        public bool IsSynchronized => true;

        public object SyncRoot => syncRoot;

        public BatchStep<TResult> this[int index] => _processes[index];

        public void CopyTo(Array array, int index)
            => _processes.CopyTo((BatchStep<TResult>[])array, index);

        public int Count => _processes.Count;

        public IEnumerator GetEnumerator() => _processes.GetEnumerator();

        IEnumerator<BatchStep<TResult>> IEnumerable<BatchStep<TResult>>.GetEnumerator() => _processes.GetEnumerator();

        abstract internal protected Task<TResult> Execute(short shardId, DbConnection connection, DbTransaction transaction, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken);

        public bool Remove(BatchStep<TResult> item) => _processes.Remove(item);

        public void Clear()
        {
            _processes.Clear();
        }
    }
}
