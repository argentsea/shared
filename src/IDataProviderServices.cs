using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace ArgentSea
{
    public interface IDataProviderServices
    {
        bool GetIsErrorTransient(Exception exception);

        DbConnection NewConnection(string connectionString);

        DbCommand NewCommand(string storedProcedureName, DbConnection connection);

        void SetParameters(DbCommand cmd, DbParameterCollection parameters);

        //string NormalizeParameterName(string parameterName);

        //string NormalizeFieldName(string fieldName);
    }

}
