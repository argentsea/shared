//using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;

namespace ArgentSea
{
    public delegate TResult SqlShardObjectConverter<TShard, TResult, TPrm>(TShard shardNumber, TPrm parameter, IDataReader rdr, DbParameterCollection prms, ILogger logger);

    public delegate TResult SqlDbObjectConverter<TResult, TPrm>(TPrm parameter, IDataReader rdr, DbParameterCollection prms, ILogger logger);

}
