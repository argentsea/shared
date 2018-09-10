using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace ArgentSea
{
    /// <summary>
    /// Abstract class implementing the data connection functionality shared by PostgreSQL and SQL Server implementations.
    /// </summary>
    public abstract class DataConnectionConfiguration : IConnectionConfiguration
    {
        private ImmutableDictionary<string, SecurityConfiguration> _credentials = null;
        private ImmutableDictionary<string, DataResilienceConfiguration> _resilienceStrategies = null;
        private string _dataResilienceKey = null;
        private string _securityKey = null;
        protected bool hasConnectionPropertyChanged = true;

        public void SetConfigurationOptions(DataSecurityOptions securityOptions, DataResilienceOptions resilienceStrategiesOptions)
        {
            if (securityOptions?.Credentials is null)
            {
                throw new Exception($"The data access system cannot obtain required security information. Your application data connection configuration may be missing the required “Credentials” section.");
            }
            var sbdr = ImmutableDictionary.CreateBuilder<string, SecurityConfiguration>();
            foreach (var crd in securityOptions.Credentials)
            {
                sbdr.Add(crd.SecurityKey, crd);
            }
            _credentials = sbdr.ToImmutable();

            var rbdr = ImmutableDictionary.CreateBuilder<string, DataResilienceConfiguration>();
            if (resilienceStrategiesOptions?.DataResilienceStrategies is null)
            {
                rbdr.Add(string.Empty, new DataResilienceConfiguration());
            }
            else
            {
                foreach (var rs in resilienceStrategiesOptions.DataResilienceStrategies)
                {
                    rbdr.Add(rs.DataResilienceKey, rs);
                }
            }
            _resilienceStrategies = rbdr.ToImmutable();
        }

        public abstract string ConnectionDescription { get; }

        public abstract string GetConnectionString();

        protected DataResilienceConfiguration GetResilienceStrategy(ILogger logger)
        {
            if (_resilienceStrategies != null && _resilienceStrategies.Count > 0 && !string.IsNullOrEmpty(_dataResilienceKey))
            {
                if (!_resilienceStrategies.TryGetValue(_dataResilienceKey, out var rstrategy))
                {
                    logger.LogWarning($"Connection {this.ConnectionDescription} specifies a resiliance strategy of “{_dataResilienceKey}”, but that could not be found in the list of configured strategies. Using a default strategy instead.");
                    rstrategy = new DataResilienceConfiguration();
                }
                return rstrategy;
            }
            return new DataResilienceConfiguration();
        }

        protected SecurityConfiguration GetSecurityConfiguration()
        {
            if (_credentials != null && _credentials.Count > 0 && !string.IsNullOrEmpty(_securityKey))
            {
                if (!_credentials.TryGetValue(_securityKey, out var securityInfo))
                {
                    throw new Exception($"Connection {this.ConnectionDescription} specifies a credential strategy of “{ _securityKey }” is not defined in in the list of credentials.");
                }
                return securityInfo;
            }
            return null;
        }

        public string SecurityKey
        {
            get
            {
                return _securityKey;
            }
            set
            {
                hasConnectionPropertyChanged = true;
                _securityKey = value;
            }
        }

        public string DataResilienceKey
        {
            get
            {
                return _dataResilienceKey;
            }
            set
            {
                _dataResilienceKey = value;
            }
        }

        //private static Policy GetCommandResiliencePolicy(string sprocName, IDataProviderServiceFactory dataProviderServices, DataResilienceConfiguration resilienceStrategy, string connectionName, Action<string, string, int, Exception> commandRetryHandler)
        //{
        //    Policy result;
        //    var retryPolicy = Policy.Handle<DbException>(ex =>
        //            dataProviderServices.GetIsErrorTransient(ex)
        //        )
        //        .WaitAndRetryAsync(
        //            retryCount: resilienceStrategy.RetryCount,
        //            sleepDurationProvider: attempt => resilienceStrategy.HandleRetryTimespan(attempt),
        //            //onRetry: (exception, timeSpan, retryCount, context) => this.HandleCommandRetry(sprocName, connectionName, retryCount, exception)
        //            onRetry: (exception, timeSpan, retryCount, context) => commandRetryHandler(sprocName, connectionName, retryCount, exception)
        //        );

        //    if (resilienceStrategy.CircuitBreakerFailureCount < 1)
        //    {
        //        result = retryPolicy;
        //    }
        //    else
        //    {
        //        var circuitBreakerPolicy = Policy.Handle<Exception>().CircuitBreakerAsync(
        //            exceptionsAllowedBeforeBreaking: resilienceStrategy.CircuitBreakerFailureCount,
        //            durationOfBreak: TimeSpan.FromMilliseconds(resilienceStrategy.CircuitBreakerTestInterval)
        //            );
        //        result = circuitBreakerPolicy.Wrap(retryPolicy);
        //    }
        //    return result;
        //}

    }
}
