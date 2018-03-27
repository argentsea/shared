using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
	public class DataResilienceConfiguration
	{
		public string DataResilienceKey { get; set; }

		public int RetryCount { get; set; } = 6;

		/// <summary>
		/// This is the number of milliseconds to wait before retrying a “retry-able” connection or command error. Default is 500 ms.
		/// This interval may be extended with each retry, depending upon the RetryLengthening setting, up to RetryCount.
		/// </summary>
		public int RetryInterval { get; set; } = 250;

		public enum SequenceLengthening
		{
			Linear,
			Fibonacci,
			Cumulative,
			Doubling,
			Squaring
		}

		/// <summary>
		/// If a connection or command fails, this setting determines how much each subsequent retry should be further delayed.
		/// </summary>
		public SequenceLengthening RetryLengthening { get; set; } = SequenceLengthening.Fibonacci;

		/// <summary>
		/// If a connection or command consistantly fails, the circuit breaker will reject all further connections until one suceeds.
		/// This setting determines how many failures (after retries, if retry-able) before blocking all connections apart from a few periodic test attempts.
		/// </summary>
		public int CircuitBreakerFailureCount { get; set; } = 20;

		/// <summary>
		/// If a connection or command fails, the circuit breaker will reject all further connections until one suceeds.
		/// This setting determines how long (in milliseonds) the system should wait before allowing a test connection.
		/// </summary>
		public int CircuitBreakerTestInterval { get; set; } = 5000;

		public TimeSpan HandleRetryTimespan(int attempt)
		{
			long result;
			switch (this.RetryLengthening)
			{
				case SequenceLengthening.Cumulative:
					result = attempt * (attempt + 1) / 2;
					break;
				case SequenceLengthening.Doubling:
					result = ((attempt - 1) * 2) * this.RetryInterval;
					break;
				case SequenceLengthening.Linear:
					result = attempt * this.RetryInterval;
					break;
				case SequenceLengthening.Squaring:
					result = this.RetryInterval * (long)Math.Pow(2, attempt - 1);
					break;
				default: //Finonacci is default
					result = (attempt + (attempt - 1)) * this.RetryInterval;
					break;
			}
			return TimeSpan.FromMilliseconds(result);
		}
	}
}

