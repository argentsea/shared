using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
	//usersecrets.json:
	//"Credentials": [
	//	{
	//	    "SecurityKey": "0",
	//	    "UserName": "user",
	//	    "Password": "123456"
	//	},
	//	{
	//	    "SecurityKey": "1",
	//	    "WindowsAuth": true,
	//	},
	//	{
	//	    "SecurityKey": "2",
	//	    "UserName": "account",
	//	    "Password": "7890"
	//	}
	//]
	public class DataSecurityOptions
    {
        public SecurityConfiguration[] Credentials { get; set; }
    }
}
