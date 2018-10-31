// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    class SimpleDbConnection : IConnectionConfiguration
    {
        public string ConnectionDescription { get; set; }

        public string ConnectionString { get; set; }

        public string ResilienceKey { get { return null; } }

        public string GetConnectionString()
        {
            return ConnectionString;
        }

        public void SetConfigurationOptions(DataSecurityOptions securityOptions, DataResilienceOptions resilienceStrategiesOptions)
        {
            //
        }
    }
}
