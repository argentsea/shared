using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.Immutable;

namespace ArgentSea
{
    /// <summary>
    /// This interface is used by provider specific implementations. It is unlikely that you would implement this in consumer code.
    /// </summary>
    public interface IDbDataConfigurationOptions
	{
		IDbConnectionConfiguration[] DbConnectionsInternal { get; }
	}

    /// <summary>
    /// This interface is used by provider specific implementations. It is unlikely that you would implement this in consumer code.
    /// </summary>
    public interface IDbConnectionConfiguration
	{
		string DatabaseKey { get; set; }
		string SecurityKey { get; set; }
		string DataResilienceKey { get; set; }
		IConnectionConfiguration DataConnectionInternal { get; }
	}
}
