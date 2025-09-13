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
    /// This error is raised upon an attempt to create an Empty ShardKey or ShardChild, but the IDs are not zero.
    /// Essentially, any shard object with a DataOrigin of '0' (Empty) must also have zeroed IDs (be equal to ShardKey.Empty or ShardChild.Empty).
    /// </summary>
    public sealed class InvalidShardArgumentsException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidShardArgumentsException" /> class with no error message.
        /// </summary>
        public InvalidShardArgumentsException()
            : base($"An empty shard key type must have zeros for shard and record values.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidShardArgumentsException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public InvalidShardArgumentsException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidShardArgumentsException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public InvalidShardArgumentsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
