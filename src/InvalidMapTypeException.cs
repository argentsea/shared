// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Reflection;


namespace ArgentSea
{
    /// <summary>
    /// This exception is raise when a model property has a database mapping attribute that is not of the correct type.
    /// </summary>
    /// <example>An attempt to map a string property to an integer parameter would generate this error.</example>
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
			: base($"Sql type mismatch: Class {property.DeclaringType} cannot map property “{property.Name}“ of type “{property.PropertyType.ToString()}” to database type enumeration with numeric value of {sqlType.ToString()}.")
		{
            this.VariableName = property.Name;
            this.VariableType = property.PropertyType;
            this.SqlType = sqlType;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidMapTypeException" /> class.
        /// </summary>
        /// <param name="variableName">The variable decorated with the mapping attribute.</param>
        /// <param name="sqlType">The stored procedure parameter type (int, not enum, due to provider discrepancies).</param>
        public InvalidMapTypeException(string variableName, Type type, int sqlType)
			: base($"Sql type mismatch: {variableName} cannot be mapped because type {type.ToString()} does not map to database type enumeration with numeric value of {sqlType.ToString()}.")
		{
            this.VariableName = variableName;
            this.VariableType = type;
            this.SqlType = sqlType;
		}
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidMapTypeException" /> class.
        /// </summary>
        /// <param name="variableName">The variable decorated with the mapping attribute.</param>
        /// <param name="sqlType">The integer stored procedure parameter type.</param>
        /// <param name="sqlTypeName">The name of the stored procedure parameter type.</param>
        public InvalidMapTypeException(string variableName, Type type, int sqlType, string sqlTypeName)
            : base($"Sql type mismatch: {variableName} cannot be mapped because type {type.ToString()} does not map to database type {sqlTypeName} ({sqlType.ToString()}).")
        {
            this.VariableName = variableName;
            this.VariableType = type;
            this.SqlType = sqlType;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidMapTypeException" /> class.
        /// </summary>
        /// <param name="property">The property decorated with the mapping attribute.</param>
        /// <param name="sqlType">The integer stored procedure parameter type.</param>
        /// <param name="sqlTypeName">The name of the stored procedure parameter type.</param>
        public InvalidMapTypeException(PropertyInfo property, int sqlType, string sqlTypeName)
            : base($"Sql type mismatch: Class {property.DeclaringType} cannot map property “{property.Name}“ of type “{property.PropertyType.ToString()}” to database type {sqlTypeName} ({sqlType.ToString()}).")
        {
            this.VariableName = property.Name;
            this.VariableType = property.PropertyType;
            this.SqlType = sqlType;
        }

        public string VariableName { get; }

        public Type VariableType { get; }

        public int SqlType { get; }
	}
}
