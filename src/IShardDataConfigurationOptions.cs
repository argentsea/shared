using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.Immutable;


// UserSecrets.json:
// "DataSources": 
//      "Security": [
//          {
//              "SecurityKey": "0",
//              "UserName": "user",
//              "Password": "123456"
//          },
//          {
//              "SecurityKey": "1",
//              "UserName": "account",
//              "Password": "7890"
//          }
//      ]
//  }
//
// appsettings.json:
//  "DataSources":
//      "ResilienceStrategies": [
//          {
//              "ResilienceKey": "remote",
//              "RetryCount": "6",
//              "RetryInterval": "250",
//              "RetryLengthening": "Finonacci",
//              "CircuitBreakerFailureCount": "20",
//              "CircuitBreakerTestInterval": "5000"
//          }
//      ]
//      {
//      "ShardSets": [
//          "ShardSetName": "0",
//          "ResilienceKey": "remote",
//          "Shards": [
//              {
//                  "ShardConnectionId": 0,
//                  "ReadConnection": {
//                      "SecurityKey": "0",
//                      "Server": "10.10.10.10",
//                      "Database": "MyDb1"
//                  },
//                  {
//                  "WriteConnection": {
//                      "SecurityKey": "0",
//                      "Server": "10.10.10.10",
//                      "Database": "MyDb1"
//                  }
//              },
//              {
//                  "ShardConnectionKey": 1,
//                  "ReadConnection": {
//                      "SecurityKey": "1",
//                      "Server": "10.10.10.10",
//                      "Database": "MyDb2"
//                  },
//                  {
//                  "WriteConnection": {
//                      "SecurityKey": "1",
//                      "Server": "10.10.10.10",
//                      "Database": "MyDb2"
//                  }
//              }
//          ]
//      ],
//      "DbConnections": [
//          {
//              "DbConnectionId": 0,
//              "DbConnection": {
//                  "SecurityKey": "0",
//                  "ResilienceKey": "remote",
//                  "Server": "10.10.10.10",
//                  "Database": "MyDb3"
//              }
//          },
//          {
//              "DbConnectionId": 1,
//              "DbConnection": {
//                  "SecurityKey": "1",
//                  "ResilienceKey": "remote",
//                  "Server": "10.10.10.10",
//                  "Database": "MyDb4"
//              }
//          }
//      ]
//  }

namespace ArgentSea
{
    //  "DataSources":
    //      {
    //      "ShardSets": [
    //          "ShardSetName": "0",
    //          "ResilienceKey": "remote",
    //          "Shards": [
    //              {
    //                  "ShardConnectionId": 0,
    //                  "ReadConnection": {
    //                      "SecurityKey": "0",
    //                      "Server": "10.10.10.10",
    //                      "Database": "MyDb1"
    //                  },
    //                  {
    //                  "WriteConnection": {
    //                      "SecurityKey": "0",
    //                      "Server": "10.10.10.10",
    //                      "Database": "MyDb1"
    //                  }
    //              },
    //              {
    //                  "ShardConnectionKey": 1,
    //                  "ReadConnection": {
    //                      "SecurityKey": "1",
    //                      "Server": "10.10.10.10",
    //                      "Database": "MyDb2"
    //                  },
    //                  {
    //                  "WriteConnection": {
    //                      "SecurityKey": "1",
    //                      "Server": "10.10.10.10",
    //                      "Database": "MyDb2"
    //                  }
    //              }
    //          ]
    //      ],


    /// <summary>
    /// This interface is used by provider specific implementations. It is unlikely that you would implement this in consumer code.
    /// </summary>
    /// <typeparam name="TShard"></typeparam>
    public interface IShardDataConfigurationOptions<TShard> where TShard : IComparable
    {
		IShardConnectionsConfiguration<TShard>[] ShardSetsInternal { get; }

	}

    /// <summary>
    /// This interface is used by provider specific implementations. It is unlikely that you would implement this in consumer code.
    /// </summary>
    /// <typeparam name="TShard"></typeparam>
	public interface IShardConnectionsConfiguration<TShard> where TShard : IComparable
    {
		string ShardSetName { get; set; }
		IShardConnectionConfiguration<TShard>[] ShardsInternal { get; }
	}

    /// <summary>
    /// This interface is used by provider specific implementations. It is unlikely that you would implement this in consumer code.
    /// </summary>
    /// <typeparam name="TShard"></typeparam>
	public interface IShardConnectionConfiguration<TShard> where TShard : IComparable
    {
		TShard ShardId { get; set; }
		IConnectionConfiguration ReadConnectionInternal { get; }
		IConnectionConfiguration WriteConnectionInternal { get; }
	}
}
