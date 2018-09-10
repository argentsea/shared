using System;
using System.Data.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Polly;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace ArgentSea
{
    /// <summary>
    /// This interface is used by provider specific implementations. It is unlikely that you would reference this in consumer code.
    /// The interface defines the minimum capability of a connection definition.
    /// </summary>
    public interface IConnectionConfiguration
    {

        string GetConnectionString();

        string ConnectionDescription
        {
            get;
        }

        void SetConfigurationOptions(DataSecurityOptions securityOptions, DataResilienceOptions resilienceStrategiesOptions);
    }
}
