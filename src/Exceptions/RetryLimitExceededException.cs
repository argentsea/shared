// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArgentSea
{
    /// <summary>
    /// This exception is raise when a transient failure occurs, but automatic retries were not successful.
    /// </summary>
    public sealed class RetryLimitExceededException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RetryLimitExceededException" /> class with no error message.
        /// </summary>
        public RetryLimitExceededException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryLimitExceededException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public RetryLimitExceededException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryLimitExceededException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public RetryLimitExceededException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
