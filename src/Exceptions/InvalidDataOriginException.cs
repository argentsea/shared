// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    public sealed class InvalidDataOriginException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDataOriginException" /> class with an error message.
        /// </summary>
        public InvalidDataOriginException()
            : base("The data origin of the provided key does not match the expected origin value. Possibly this key is referencing a different data source.")
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDataOriginException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidDataOriginException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDataOriginException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public InvalidDataOriginException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidDataOriginException" /> class which includes the orgin values in the error message.
        /// </summary>
        /// <param name="expectedOrigin"></param>
        /// <param name="actualOrigin"></param>
        public InvalidDataOriginException(char expectedOrigin, char actualOrigin)
            : base($"The data origin “{ actualOrigin }” of the provided key does not match the expected origin value of “{actualOrigin}”. Possibly this key is referencing a different data source.")
        {
        }
    }
}
