using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    public class NoMappingAttributesFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NoMappingAttributesFoundException" /> class with no error message.
        /// </summary>
        public NoMappingAttributesFoundException()
            : base("No mapping attributes could be found on the model class provided to the mapper.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoMappingAttributesFoundException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public NoMappingAttributesFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoMappingAttributesFoundException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public NoMappingAttributesFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
