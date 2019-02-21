// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace ArgentSea
{
    /// <summary>
    /// This interface is used by provider specific implementations. It is unlikely that you would reference this in consumer code.
    /// The interface defines the capabilities of a database providers service.
    /// </summary>
    public interface IDataProviderServiceFactory
    {
        bool GetIsErrorTransient(Exception exception);

        DbConnection NewConnection(string connectionString);

        DbCommand NewCommand(string storedProcedureName, DbConnection connection);

        void SetParameters(DbCommand cmd, string[] parmeterNames, DbParameterCollection parameters, IDictionary<string, object> parameterValues);

    }

}
