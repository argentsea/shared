// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    public sealed class InvalidShardKeyMetadataException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidShardKeyMetadataException" /> class with an error message.
        /// </summary>
        public InvalidShardKeyMetadataException()
            : base("The metadata embedded in the serialized shardkey data does not match the presecribed data types required by the current shardkey definition. The data is corrupt.")
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidShardKeyMetadataException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidShardKeyMetadataException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidShardKeyMetadataException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public InvalidShardKeyMetadataException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidShardKeyMetadataException" /> class which includes the orgin values in the error message.
        /// </summary>
        public InvalidShardKeyMetadataException(Type expected)
            : base($"The metadata embedded in the serialized shardkey does not match the prescribed data type of {expected.ToString()} required by the current shardkey definition. The data is corrupt.")
        {
        }
    }
}
