// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    /// <summary>
    /// The exception is thrown when an output parameter is expected, but not found, when ExecuteQueryToValueAsync is invoked.
    /// </summary>
    public sealed class UnexpectedSqlResultException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedSqlResultException" /> class with no error message.
        /// </summary>
        public UnexpectedSqlResultException() : base("The query results are missing an expected value.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedSqlResultException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UnexpectedSqlResultException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedSqlResultException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public UnexpectedSqlResultException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
