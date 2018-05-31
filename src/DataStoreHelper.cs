using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.Immutable;
using System.Threading.Tasks;
using System.Data.Common;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Polly;

namespace ArgentSea
{
	/// <summary>
	/// This class exists to consolodate the redundant (not-DRY) logic between DbDataStores and ShardDataStores
	/// </summary>
    internal class DataStoreHelper
    {
		private static readonly double TimestampToMilliseconds = (double)TimeSpan.TicksPerSecond / (Stopwatch.Frequency * TimeSpan.TicksPerMillisecond);

		public static ImmutableDictionary<string, SecurityConfiguration> BuildCredentialDictionary(IOptions<DataSecurityOptions> securityOptions)
		{
			if (securityOptions?.Value?.Credentials is null)
			{
				throw new Exception($"The data access system cannot obtain required security information. Your application data connection configuration may be missing the required “Credentials” section.");
			}
			var sbdr = ImmutableDictionary.CreateBuilder<string, SecurityConfiguration>();
			foreach (var crd in securityOptions.Value.Credentials)
			{
				sbdr.Add(crd.SecurityKey, crd);
			}
			return sbdr.ToImmutable();

		}
		public static ImmutableDictionary<string, DataResilienceConfiguration> BuildResilienceStrategies(IOptions<DataResilienceOptions> resilienceStrategiesOptions)
		{
			var rbdr = ImmutableDictionary.CreateBuilder<string, DataResilienceConfiguration>();
			if (resilienceStrategiesOptions?.Value?.DataResilienceStrategies is null)
			{
				rbdr.Add(string.Empty, new DataResilienceConfiguration());
			}
			else
			{
				foreach (var rs in resilienceStrategiesOptions.Value.DataResilienceStrategies)
				{
					rbdr.Add(rs.DataResilienceKey, rs);
				}
			}
			return rbdr.ToImmutable();
		}
		public static DataResilienceConfiguration GetResilienceStrategy(ImmutableDictionary<string, DataResilienceConfiguration> resilienceStrategies, string resilienceStrategyKey, string connectionName, ILogger logger)
		{
			if (resilienceStrategies != null && resilienceStrategies.Count > 0 && !string.IsNullOrEmpty(resilienceStrategyKey))
			{
				if (!resilienceStrategies.TryGetValue(resilienceStrategyKey, out var rstrategy))
				{
					logger.LogWarning($"Connection {connectionName} specifies a resiliance strategy that could not be found in the list of configured strategies. Using a default strategy instead.");
					rstrategy = new DataResilienceConfiguration();
				}
				return rstrategy;
			}
			return new DataResilienceConfiguration();
		}
		private static Policy GetCommandResiliencePolicy(string sprocName, IDataProviderServiceFactory dataProviderServices, DataResilienceConfiguration resilienceStrategy, string connectionName, Action<string, string, int, Exception> commandRetryHandler)
		{
			Policy result;
			var retryPolicy = Policy.Handle<DbException>(ex =>
					dataProviderServices.GetIsErrorTransient(ex)
				)
				.WaitAndRetryAsync(
					retryCount: resilienceStrategy.RetryCount,
					sleepDurationProvider: attempt => resilienceStrategy.HandleRetryTimespan(attempt),
					//onRetry: (exception, timeSpan, retryCount, context) => this.HandleCommandRetry(sprocName, connectionName, retryCount, exception)
					onRetry: (exception, timeSpan, retryCount, context) => commandRetryHandler(sprocName, connectionName, retryCount, exception)
				);

			if (resilienceStrategy.CircuitBreakerFailureCount < 1)
			{
				result = retryPolicy;
			}
			else
			{
				var circuitBreakerPolicy = Policy.Handle<Exception>().CircuitBreakerAsync(
					exceptionsAllowedBeforeBreaking: resilienceStrategy.CircuitBreakerFailureCount,
					durationOfBreak: TimeSpan.FromMilliseconds(resilienceStrategy.CircuitBreakerTestInterval)
					);
				result = circuitBreakerPolicy.Wrap(retryPolicy);
			}
			return result;
		}
	}
}
