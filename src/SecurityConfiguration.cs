using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    /// <summary>
    /// Data security information separated into separate configuration class so that it can be secured separately, as in Azure Key Value for example.
    /// </summary>
    public class SecurityConfiguration
    {
        /// <summary>
        /// Used by data connection classes to identify the corresponding security information.
        /// </summary>
        public string SecurityKey { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        //public bool RequireSSL { get; set; }
        public bool WindowsAuth { get; set; }
    }
}
