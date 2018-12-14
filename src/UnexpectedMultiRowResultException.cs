// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    public sealed class UnexpectedMultiRowResultException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedMultiRowResultException" /> class with an error message.
        /// </summary>
        public UnexpectedMultiRowResultException()
            : base("The database returned multiple records when only one was expected.")
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedMultiRowResultException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        //public UnexpectedMultiRowResultException(string message)
        //    : base(message)
        //{
        //}

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedMultiRowResultException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public UnexpectedMultiRowResultException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public UnexpectedMultiRowResultException(string procedureName)
            : base($"Procedure {procedureName} returned multiple records when only one was expected.")
        {

        }
    }
}
