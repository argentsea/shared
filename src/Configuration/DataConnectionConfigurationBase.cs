// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ArgentSea
{
    public enum SequenceLengthening
    {
        Linear,
        Fibonacci,
        HalfSquare,
        Squaring
    }

    /// <summary>
    /// Abstract class implementing the data connection functionality shared by PostgreSQL and SQL Server implementations.
    /// </summary>
    public abstract class DataConnectionConfigurationBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Event indicating that a connection property has changed, which should cause any future connection strings to change. This event is not raise on resiliance (non-connection) properties.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private const int DefaultRetryInterval = 250;
        private string _userName = null;
        private string _password = null;
        private bool? _windowsAuth = null;

        /// <summary>
        /// The database login account, if windows auth is not used.
        /// </summary>
        public string UserName
        {
            get { return _userName; }
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// The database login password, if windows auth is not used.
        /// </summary>
        public string Password
        { 
            get { return _password; }
            set
            {
                if (_password != value)
                {
                    _password = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// Windows (kerberos) auth should be used, rather than username and password.
        /// </summary>
        public bool? WindowsAuth
        {
            get { return _windowsAuth; }
            set
            {
                if (_windowsAuth != value)
                {
                    _windowsAuth = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// The number of times to automatically retry when a transient error is encountered. The default is 6. 
        /// Does not raise PropertyChanged event.
        /// </summary>
        public int? RetryCount { get; set; }

        /// <summary>
        /// This is the number of milliseconds to wait before retrying a “retry-able” connection or command error. Default is 250 ms.
        /// This interval may be extended with each retry, depending upon the RetryLengthening setting, up to RetryCount.
        /// Does not raise PropertyChanged event.
        /// </summary>
        public int? RetryInterval { get; set; }

        /// <summary>
        /// If a connection or command fails, this setting determines how much each subsequent retry should be further delayed.
        /// Does not raise PropertyChanged event.
        /// </summary>
        public SequenceLengthening? RetryLengthening { get; set; }

        /// <summary>
        /// If a connection or command consistantly fails, the circuit breaker will reject all further connections until one suceeds.
        /// This setting determines how many failures (after retries, if retry-able) before blocking all connections apart from a few periodic test attempts.
        /// Does not raise PropertyChanged event.
        /// </summary>
        public int? CircuitBreakerFailureCount { get; set; }

        /// <summary>
        /// If a connection or command fails, the circuit breaker will reject all further connections until one suceeds.
        /// This setting determines how long (in milliseonds) the system should wait before allowing a test connection.
        /// Does not raise PropertyChanged event.
        /// </summary>
        public int? CircuitBreakerTestInterval { get; set; }


        public TimeSpan GetRetryTimespan(int attempt)
        {
            long result;
            var retryLengthening = SequenceLengthening.Fibonacci;
            int retryInterval = DefaultRetryInterval;
            if (this.RetryLengthening.HasValue)
            {
                retryLengthening = this.RetryLengthening.Value;
            }
            if (this.RetryInterval.HasValue)
            {
                retryInterval = this.RetryInterval.Value;
            }
            switch (retryLengthening)
            {
                case SequenceLengthening.HalfSquare:
                    result = ((attempt * attempt) / 2) * retryInterval;
                    break;
                case SequenceLengthening.Linear:
                    result = attempt * retryInterval;
                    break;
                case SequenceLengthening.Squaring:
                    result = retryInterval * (long)Math.Pow(2, attempt - 1);
                    break;
                default: //Finonacci is default
                    result = (attempt + (attempt - 1)) * retryInterval;
                    break;
            }
            return TimeSpan.FromMilliseconds(result);
        }

        protected void RaisePropertyChanged([CallerMemberName] string caller = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }

    }
}
