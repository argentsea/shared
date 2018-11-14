// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    public class SimpleDbConnection : IDataConnection
    {
        public string ConnectionDescription { get; set; }

        public string ConnectionString { get; set; }

        public string GetConnectionString()
        {
            return ConnectionString;
        }

        public int? RetryCount { get; set; }

        public int? CircuitBreakerFailureCount { get; set; }

        public int? CircuitBreakerTestInterval { get; set; }

        public SequenceLengthening? RetryLengthening { get; set; }

        public int? RetryInterval { get; set; }

        public TimeSpan GetRetryTimespan(int attempt)
        {
            long result;
            var retryLengthening = SequenceLengthening.Fibonacci;
            int retryInterval = 250;
            if (this.RetryLengthening.HasValue)
            {
                retryLengthening = this.RetryLengthening.Value;
            }
            if (this.RetryInterval.HasValue)
            {
                retryInterval = this.RetryInterval.Value;
            }
            switch (retryLengthening)
            {
                case SequenceLengthening.HalfSquare:
                    result = ((attempt * attempt) / 2) * retryInterval;
                    break;
                case SequenceLengthening.Linear:
                    result = attempt * retryInterval;
                    break;
                case SequenceLengthening.Squaring:
                    result = retryInterval * (long)Math.Pow(2, attempt - 1);
                    break;
                default: //Finonacci is default
                    result = (attempt + (attempt - 1)) * retryInterval;
                    break;
            }
            return TimeSpan.FromMilliseconds(result);
        }

        public void SetAmbientConfiguration(DataConnectionConfigurationBase notUsed1, DataConnectionConfigurationBase notUsed2, DataConnectionConfigurationBase notUsed3)
        {
            //
        }
    }
}
