using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    public interface IDataConnection
    {
        /// <summary>
        /// When overridden in a derived class, returns a description that can be used for logging connection errors.
        /// </summary>
        string ConnectionDescription { get; }

        /// <summary>
        /// When overridden in a derived class, returns the ADO.NET connection string from the various connection propeties. Typically, a ConnectionStringBuilder is used for this purpose.
        /// </summary>
        /// <returns></returns>
        string GetConnectionString();

        int? RetryCount { get;  }

        TimeSpan GetRetryTimespan(int attemptCount);

        int? CircuitBreakerFailureCount { get; }

        int? CircuitBreakerTestInterval { get; }

        void SetAmbientConfiguration(DataConnectionConfigurationBase globalProperties, DataConnectionConfigurationBase shardSetProperties, DataConnectionConfigurationBase shardProperties);
    }
}
