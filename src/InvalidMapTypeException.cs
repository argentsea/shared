using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;


namespace ArgentSea
{
    public sealed class InvalidMapTypeException : Exception
    {
		/// <summary>
		/// Initializes a new instance of the <see cref="InvalidMapTypeException" /> class with no error message.
		/// </summary>
		public InvalidMapTypeException()
        {
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="InvalidMapTypeException" /> class with a specified error message.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public InvalidMapTypeException(string message)
            : base(message)
        {
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="InvalidMapTypeException" /> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public InvalidMapTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
		/// <summary>
		/// Initializes a new instance of the <see cref="InvalidMapTypeException" /> class.
		/// </summary>
		/// <param name="property">The property decorated with the mapping attribute.</param>
		/// <param name="sqlType">The stored procedure parameter type (int, not enum, due to provider discrepancies).</param>
		public InvalidMapTypeException(PropertyInfo property, int sqlType)
			: base($"Sql type mismatch: Class {property.DeclaringType} cannot map property {property.Name} of type {property.PropertyType.GetType().ToString()} to data type enumeration with numeric value of {sqlType.ToString()}.")
		{
		}
		public InvalidMapTypeException(string variableName, Type type, int sqlType)
			: base($"Sql type mismatch: {variableName} cannot be mapped because type {type.ToString()} does not map to data type enumeration with numeric value of {sqlType.ToString()}.")
		{
		}
	}
}
