using System;
using System.Data.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Polly;
using Microsoft.Extensions.Logging;

namespace ArgentSea
{
    public interface IConnectionConfiguration
    {

        string GetConnectionString();

        string ConnectionDescription
        {
            get;
        }

        void SetSecurity(SecurityConfiguration security);
    }
}
