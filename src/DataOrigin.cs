using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
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
