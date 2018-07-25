using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    class SimpleDbConnection : IConnectionConfiguration
    {
        public string ConnectionDescription { get; set; }

        public string ConnectionString { get; set; }

        public string GetConnectionString()
        {
            return ConnectionString;
        }

        public void SetSecurity(SecurityConfiguration security)
        {
            //
        }
    }
}
