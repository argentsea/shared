using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;


namespace ArgentSea
{
	public sealed class MultipleMapAttributesException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MultipleMapAttributesException" /> class with no error message.
		/// </summary>
		public MultipleMapAttributesException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MultipleMapAttributesException" /> class with a specified error message.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public MultipleMapAttributesException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MultipleMapAttributesException" /> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public MultipleMapAttributesException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MultipleMapAttributesException" /> class.
		/// </summary>
		/// <param name="property">The property that is decorated with multiple mapping attributes.</param>
		public MultipleMapAttributesException(PropertyInfo property)
			: base($"Multiple data mapping attributes found. Class {property.DeclaringType} cannot map property {property.Name} to multiple database parameters. If this is required, you must explicitly code data property assignments.")
		{
		}
	}
}
