// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Collections.Specialized;

namespace ArgentSea
{
    /// <summary>
    /// This is an implementation of the abstract DbParameterCollecion class. Unlike most provider-specific parameter collections, it can be created without a prior DbCommand object instance.
    /// </summary>
    public class ParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _lst = new List<DbParameter>();

        public override int Count => this._lst.Count;

        public override object SyncRoot => ((System.Collections.ICollection)this._lst).SyncRoot;

        public override int Add(object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (value is DbParameter)
            {
                var prm = (DbParameter)value;
                this._lst.Add(prm);
                return _lst.Count;
            }
            throw new ArgumentException(nameof(value));
        }

        public override void AddRange(Array values)
        {
            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }
            foreach (var value in values)
            {
                this.Add(value);
            }
        }

        public override void Clear() => this._lst.Clear();

        public override bool Contains(object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (value is DbParameter)
            {
                var prm = (DbParameter)value;
                return this._lst.Contains(prm);
            }
            else
            {
                throw new ArgumentException(nameof(value));
            }
        }

        public override bool Contains(string value)
        {
            foreach (var prm in this._lst)
            {
                if (prm.ParameterName == value)
                {
                    return true;
                }
            }
            return false;
        }

        public override void CopyTo(Array array, int index)
        {
            ((System.Collections.ICollection)this._lst).CopyTo(array, index);
        }

        public override System.Collections.IEnumerator GetEnumerator()
        {
            return ((System.Collections.ICollection)this._lst).GetEnumerator();
        }

        public override int IndexOf(object value)
        {
            if (value is DbParameter)
            {
                for (var i = 0; i < this._lst.Count; i++)
                {
                    if (this._lst[i] == (DbParameter)value)
                    {
                        return i;
                    }
                }
                return -1;
            }
            else
            {
                throw new ArgumentException(nameof(value));
            }
        }

        public override int IndexOf(string parameterName)
        {
            for (var i = 0; i < this._lst.Count; i++)
            {
                if (this._lst[i].ParameterName == parameterName)
                {
                    return i;
                }
            }
            return -1;
        }

        public override void Insert(int index, object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (value is DbParameter)
            {
                this._lst.Insert(index, (DbParameter)value);
            }
        }

        public override void Remove(object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (value is DbParameter)
            {
                this._lst.Remove((DbParameter)value);
            }
        }

        public override void RemoveAt(int index)
        {
            this._lst.RemoveAt(index);
        }

        public override void RemoveAt(string parameterName)
        {
            var index = this.IndexOf(parameterName);
            this._lst.RemoveAt(index);
        }

        protected override DbParameter GetParameter(int index)
        {
            return this._lst[index];
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            var index = this.IndexOf(parameterName);
			if (index >= 0)
			{
				return this._lst[index];
			}
			return null;
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (value is DbParameter)
            { 
                this._lst[index] = value;
            }
            else
            {
                throw new ArgumentException(nameof(value));
            }
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (value is DbParameter)
            {
                var index = this.IndexOf(parameterName);
                this._lst[index] = value;
            }
            else
            {
                throw new ArgumentException(nameof(value));
            }
        }
    }
}
