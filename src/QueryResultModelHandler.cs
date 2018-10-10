using System;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace ArgentSea
{
	/// <summary>
	/// This delegate will be invoked for each shard (probably on different threads) to convert the cmd.Execute result to model types.
	/// </summary>
	/// <typeparam name="TShard">The Type of the ShardId.</typeparam>
	/// <typeparam name="TModel">The Type of the expected result</typeparam>
	/// <typeparam name="TArg">The Type of an optional parameter that can be passed to the parsing function. If not used, simply use Type "object" and pass null if required.</typeparam>
	/// <param name="shardId">The value of the Shard Number.</param>
	/// <param name="sprocName">Used to uniquely identify any cached Expression Trees (along with TModel type) and also included in any logging information.</param>
	/// <param name="parameter">Optional paramater(s) that can be passed to the parsing process.</param>
	/// <param name="rdr">A data reader instance supplied by the data provider when a query is executed.</param>
	/// <param name="parameters">An output parameter set supplied by the data provider when a query is executed.</param>
	/// <param name="connectionDescription">Provides any logging writes with connection information to help troubleshoot any errors.</param>
	/// <param name="logger">A logger instance for writing logs.</param>
	/// <returns>An object of the defined type.</returns>
	public delegate TModel QueryResultModelHandler<TShard, TArg, TModel>(TShard shardId, string sprocName, TArg optionalArgument, DbDataReader rdr, DbParameterCollection parameters, string connectionDescription, ILogger logger) 
		where TModel : class, new() 
		where TShard: IComparable;

}
