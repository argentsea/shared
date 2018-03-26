using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArgentSea
{
    public class DataResult
    {
        private static readonly DataResult _success = new DataResult { Succeeded = true };
        private List<DataException> _errors = new List<DataException>();

        /// <summary>
        /// Flag indicating whether if the operation succeeded or not.
        /// </summary>
        /// <value>True if the operation succeeded, otherwise false.</value>
        public bool Succeeded { get; protected set; }

        /// <summary>
        /// An <see cref="IEnumerable{T}"/> of <see cref="DataException"/>s containing an errors
        /// that occurred during the data operation.
        /// </summary>
        /// <value>An <see cref="IEnumerable{T}"/> of <see cref="DataException"/>s.</value>
        public IEnumerable<DataException> Errors => _errors;

        /// <summary>
        /// Returns an <see cref="DataResult"/> indicating a successful data operation.
        /// </summary>
        /// <returns>An <see cref="DataResult"/> indicating a successful operation.</returns>
        public static DataResult Success => _success;

        /// <summary>
        /// Creates an <see cref="DataResult"/> indicating a failed data operation, with a list of <paramref name="errors"/> if applicable.
        /// </summary>
        /// <param name="errors">An optional array of <see cref="DataException"/>s which caused the operation to fail.</param>
        /// <returns>An <see cref="DataResult"/> indicating a failed data operation, with a list of <paramref name="errors"/> if applicable.</returns>
        public static DataResult Failed(params DataException[] errors)
        {
            var result = new DataResult { Succeeded = false };
            if (errors != null)
            {
                result._errors.AddRange(errors);
            }
            return result;
        }

        /// <summary>
        /// Converts the value of the current <see cref="DataResult"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>A string representation of the current <see cref="DataResult"/> object.</returns>
        /// <remarks>
        /// If the operation was successful the ToString() will return "Succeeded" otherwise it returned 
        /// "Failed : " followed by a comma delimited list of error codes from its <see cref="Errors"/> collection, if any.
        /// </remarks>
        public override string ToString()
        {
            return Succeeded ?
                   "Succeeded" :
                   string.Format("{0} : {1}", "Failed", string.Join(",", Errors.Select(x => x.Code).ToList()));
        }
    }
}