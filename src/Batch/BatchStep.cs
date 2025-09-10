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
    public abstract class BatchStep<TResult>
    {
        protected internal abstract Task<TResult> ExecuteAsync(short shardId, DbConnection connection, DbTransaction transaction, string connectionName, IDataProviderServiceFactory services, ILogger logger, CancellationToken cancellationToken);
    }
}
