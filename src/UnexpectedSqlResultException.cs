using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    public sealed class UnexpectedSqlResultException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedSqlResultException" /> class with no error message.
        /// </summary>
        public UnexpectedSqlResultException()
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
