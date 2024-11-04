using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    public class MapAttributeMissingException : Exception
    {
        public enum ShardElement {
            ShardId,
            RecordId,
            ChildId,
            GrandChildId,
            GreatGrandChildId
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MapAttributeMissingException" /> class with no error message.
        /// </summary>
        public MapAttributeMissingException()
            : base("No mapping attributes could be found on the model class provided to the mapper.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapAttributeMissingException" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public MapAttributeMissingException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapAttributeMissingException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MapAttributeMissingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MapAttributeMissingException(ShardElement element, string attributeName)
            : base(MakeMessage(element, attributeName))
        {
            this.Element = element;
            this.AttributeName = attributeName;
        }
        private static string MakeMessage(ShardElement element, string attributeName)
        {
            switch (element)
            {
                case ShardElement.ShardId:
                    return($"The shard attribute specified a ShardId argument attribute named “{attributeName}”, but the attribute was not found. Remove this argument if you do not have a Shard Id parameter or column, or ensure that the specified name exactly matches the attribute name.");
                case ShardElement.RecordId:
                    return ($"The shard attribute specified a RecordId argument attribute named “{attributeName}”, but the attribute was not found. Ensure that the specified name exactly matches the attribute name.");
                case ShardElement.ChildId:
                    return ($"The ShardChild attribute specified a child id attribute named “{attributeName}”, but the attribute was not found. Ensure that the name specified in the ShardChild exactly matches the attribute name.");
                default:
                    return ($"The shard attribute specified a child id attribute named “{attributeName}”, but the attribute was not found. Ensure that the name specified exactly matches the attribute name.");
            }
        }
        public ShardElement Element { get; }

        public string AttributeName { get; }
    }
}
