﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Collections.Immutable;

namespace ArgentSea
{
    /// <summary>
    /// This interface is used by provider specific implementations. It is unlikely that you would implement this in consumer code.
    /// </summary>
    public interface IDatabaseConfigurationOptions
	{
		IDatabaseConnectionConfiguration[] DbConnectionsInternal { get; }
	}
}