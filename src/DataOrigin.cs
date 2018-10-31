// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    /// <summary>
    /// This object help descript the “type” of data. For example, you could assign Customer data a data origin value of 'c'.
    /// When comparing data values, the ShardKey/ShardChild will not match if the data origin is not the same value — even if IDs are the same.
    /// This helps prevent accidentaly comparisions that are not valid and also prevents collisions if you choose allows values of different types to be stored in the same cache.
    /// </summary>
    public struct DataOrigin :  IEquatable<object>
    {
        private char _value;
        public DataOrigin(char sourceIndicator)
        {
            _value = sourceIndicator;
        }
        public char SourceIndicator { get { return _value; } }

        public override bool Equals(object other)
        {
            if (other is DataOrigin)
            {
                return ((DataOrigin)other).SourceIndicator == _value;
            }
            if (other is char)
            {
                return ((char)other) == _value;
            }
            if (other is string && !string.IsNullOrEmpty((string)other))
            {
                return (((string)other)[0]) == _value;
            }
            return false;
        }
        public static bool operator ==(DataOrigin do1, DataOrigin do2)
        {
            return do1.SourceIndicator == do2.SourceIndicator;
        }
        public static bool operator !=(DataOrigin do1, DataOrigin do2)
        {
            return do1.SourceIndicator != do2.SourceIndicator;
        }
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }
    }
}
