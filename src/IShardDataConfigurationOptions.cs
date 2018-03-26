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
//              "DataResilienceKey": "remote",
//              "RetryCount": "6",
//              "RetryInterval": "250",
//              "RetryLengthening": "Finonacci",
//              "CircuitBreakerFailureCount": "20",
//              "CircuitBreakerTestInterval": "5000"
//          }
//      ]
//      {
//      "ShardSets": [
//          "ShardSetKey": "0",
//          "DataResilienceKey": "remote",
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
	//  "DataSources":
	//      {
	//      "ShardSets": [
	//          "ShardSetKey": "0",
	//          "DataResilienceKey": "remote",
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


	public interface IShardDataConfigurationOptions<TShard>
	{
		IShardConnectionsConfiguration<TShard>[] ShardSetsInternal { get; }

	}
	public interface IShardConnectionsConfiguration<TShard>
	{
		string ShardSetKey { get; set; }
		string SecurityKey { get; set; }
		string DataResilienceKey { get; set; }
		IShardConnectionConfiguration<TShard>[] ShardsInternal { get; }
	}

	public interface IShardConnectionConfiguration<TShard>
	{
		TShard ShardNumber { get; set; }
		IConnectionConfiguration ReadConnectionInternal { get; }
		IConnectionConfiguration WriteConnectionInternal { get; }
	}
}
