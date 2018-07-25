using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    /// <summary>
    /// This options class contains an array of <see cref="SecurityConfiguration">security credentials</see> (login information). 
    /// A connection can specify as particular login account by referencing a SecurityKey.
    /// </summary>
    /// <example>
    /// For example, you might configure your usersecrets.json like this:
    /// <code>
    ///"Credentials": [
    ///	{
    ///	    "SecurityKey": "0",
    ///	    "UserName": "user",
    ///	    "Password": "123456"
    ///	},
    ///	{
    ///	    "SecurityKey": "1",
    ///	    "WindowsAuth": true,
    ///	},
    ///	{
    ///	    "SecurityKey": "2",
    ///	    "UserName": "account",
    ///	    "Password": "7890"
    ///	}
    ///</code>
    ///</example>
    public class DataSecurityOptions
    {
        public SecurityConfiguration[] Credentials { get; set; }
    }
}
