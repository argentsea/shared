using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    /// <summary>
    /// The exception is thrown when a statement or procedure has a parameter names set, but a query is missing a parameter in the set.
    /// </summary>
    public sealed class ParameterNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterNotFoundException" /> class with no error message.
        /// </summary>
        public ParameterNotFoundException()
            : base ("A parameter name is define in the statement or procedure’s parameter names set, but a corresponding parameter was not provided.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterNotFoundException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ParameterNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterNotFoundException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public ParameterNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
