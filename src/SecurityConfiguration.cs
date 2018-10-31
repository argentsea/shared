// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    /// <summary>
    /// This class represents data security information, with a key that can be reference by any connection(s) that share the security information.
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
