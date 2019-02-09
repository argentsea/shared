// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    /// <summary>
    /// This exception is thrown when a parameter or record column unexpectedly contains a Db Null and cannot be assigned to a non-nullable property.
    /// </summary>
    class UnexpectedNullException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UnexpectedNullException" /> class with no error message.
		/// </summary>
		public UnexpectedNullException()
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedNullException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UnexpectedNullException(string message)
			: base(message)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedNullException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public UnexpectedNullException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedNullException" /> class.
        /// </summary>
        /// <param name="expectedType"></param>
        /// <param name="columnName"></param>

        public UnexpectedNullException(Type expectedType, string columnName)
			: base($"The database column {columnName} unexpectedly returned a “null” value and cannot be assigned to a {expectedType.ToString()}.")
		{
            this.ExpectedType = expectedType;
            this.ColumnName = columnName;
		}
        public Type ExpectedType { get; }

        public string ColumnName { get;  }
	}
}
