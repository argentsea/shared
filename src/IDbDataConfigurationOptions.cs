using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.Immutable;


// appsettings.json:
//      "DbConnections": [
//          {
//              "DbConnectionId": 0,
//              "DbConnection": {
//                  "SecurityKey": "0",
//                  "DataResilienceKey": "remote",
//                  "Server": "10.10.10.10",
//                  "Database": "MyDb3"
//              }
//          },
//          {
//              "DbConnectionId": 1,
//              "DbConnection": {
//                  "SecurityKey": "1",
//                  "DataResilienceKey": "remote",
//                  "Server": "10.10.10.10",
//                  "Database": "MyDb4"
//              }
//          }
//      ]
//  }

namespace ArgentSea
{
	public interface IDbDataConfigurationOptions
	{
		IDbConnectionConfiguration[] DbConnectionsInternal { get; }
	}
	public interface IDbConnectionConfiguration
	{
		string DatabaseKey { get; set; }
		string SecurityKey { get; set; }
		string DataResilienceKey { get; set; }
		IConnectionConfiguration DataConnectionInternal { get; }
	}
}
