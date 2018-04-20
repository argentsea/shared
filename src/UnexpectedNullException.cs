using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    class UnexpectedNullException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UnexpectedSqlResultException" /> class with no error message.
		/// </summary>
		public UnexpectedNullException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UnexpectedSqlResultException" /> class with a specified error message.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public UnexpectedNullException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UnexpectedSqlResultException" /> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public UnexpectedNullException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		public UnexpectedNullException(Type expectedType, string columnName)
			: base($"The database column {columnName} unexpectedly returned a “null” value and cannot be assigned to a {expectedType.ToString()}.")
		{
		}

	}
}
