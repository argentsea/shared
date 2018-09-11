using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    /// <summary>
    /// This options class contains an array of resilience strategies (retry and circuit breaking settings). To specify as particular resilience strategy, a connection can specify a ResilienceKey.
    /// </summary>
    /// <example>
    /// For example, you might configure your appsettings.json like this:
    /// <code>
    ///"ResilienceStrategies": [
    ///	{
    ///	    "ResilienceKey": "remote",
    ///	    "RetryCount": "6",
    ///	    "RetryInterval": "250",
    ///	    "RetryLengthening": "Finonacci",
    ///	    "CircuitBreakerFailureCount": "20",
    ///	    "CircuitBreakerTestInterval": "5000"
    ///	},
    ///	{
    ///	    "ResilienceKey": "local",
    ///	    "RetryCount": "6",
    ///	    "RetryInterval": "150",
    ///	    "RetryLengthening": "Linear",
    ///	    "CircuitBreakerFailureCount": "10",
    ///	    "CircuitBreakerTestInterval": "5000"
    ///	}
    ///]
    ///</code>
    ///</example>
    public class DataResilienceOptions
    {
		public DataResilienceConfiguration[] DataResilienceStrategies { get; set; }
	}
}
