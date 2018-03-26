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
        /// Initializes a new instance of the <see cref="UnexpectedSqlResultException" /> class with no error message.
        /// </summary>
        public InvalidMapTypeException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedSqlResultException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidMapTypeException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedSqlResultException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public InvalidMapTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
        public InvalidMapTypeException(PropertyInfo property, SqlDbType sqlType)
            : base($"Sql type mismatch: Class {property.DeclaringType} cannot map property {property.Name} of type {property.GetType().ToString()} to SQL type {sqlType.ToString()}.")
        {
        }
    }
}
