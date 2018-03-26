using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
	//EXAMPLE:
	//"ResilienceStrategies": [
	//	{
	//	    "DataResilienceKey": "remote",
	//	    "RetryCount": "6",
	//	    "RetryInterval": "250",
	//	    "RetryLengthening": "Finonacci",
	//	    "CircuitBreakerFailureCount": "20",
	//	    "CircuitBreakerTestInterval": "5000"
	//	},
	//	{
	//	    "DataResilienceKey": "local",
	//	    "RetryCount": "6",
	//	    "RetryInterval": "150",
	//	    "RetryLengthening": "Finonacci",
	//	    "CircuitBreakerFailureCount": "10",
	//	    "CircuitBreakerTestInterval": "5000"
	//	}
	//]
	public class DataResilienceConfigurationOptions
    {
		public List<DataResilienceConfiguration> DataResilienceStrategies = new List<DataResilienceConfiguration>();
	}
}
