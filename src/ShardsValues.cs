using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    public class ShardsValues<TShard> : ICollection, IEnumerable<ShardParameterValue<TShard>> where TShard : IComparable
    {
        private readonly object syncRoot = new object();
        private int _parameterCount = 0;

        public ShardsValues()
        {
            this.Shards = new Dictionary<TShard, IDictionary<string, object>>();
        }
        public ShardsValues(Dictionary<TShard, IDictionary<string, object>> shards)
        {
            if (shards is null)
            {
                throw new ArgumentNullException(nameof(shards));
            }
            this.Shards = shards;
            foreach(var shd in shards)
            {
                if (shd.Value is null)
                {
                    _parameterCount++;
                }
                else
                {
                    _parameterCount = _parameterCount + shd.Value.Count;
                }
            }
        }


        public ShardsValues<TShard> Add(TShard shardId)
        {
            if (!Shards.ContainsKey(shardId))
            {
                Shards.Add(shardId, new Dictionary<string, object>());
                _parameterCount++;
            }
            return this;
        }
        public ShardsValues<TShard> Add(TShard shardId, string parameterName, object parameterValue)
        {
            if (Shards.TryGetValue(shardId, out var prms))
            {
                if (prms.Count != 0)
                {
                    _parameterCount++;
                }
                prms.Add(parameterName, parameterValue);
            }
            else
            {
                Shards.Add(shardId, new Dictionary<string, object>() { { parameterName, parameterValue } });
                _parameterCount++;

            }
            return this;
        }

        public int Count => _parameterCount;

        public bool IsSynchronized => throw new NotImplementedException();

        public object SyncRoot => syncRoot;

        public void CopyTo(Array array, int index)
        {
            var result = new ShardParameterValue<TShard>[_parameterCount];
            var i = 0;
            foreach (var itm in ((IEnumerable<ShardParameterValue<TShard>>)this))
            {
                result[i] = itm;
                i++;
            }
        }

        public IEnumerator GetEnumerator() => ((IEnumerable<ShardParameterValue<TShard>>)this).GetEnumerator();

        IEnumerator<ShardParameterValue<TShard>> IEnumerable<ShardParameterValue<TShard>>.GetEnumerator()
        {
            foreach (var shd in Shards)
            {
                if (shd.Value.Count == 0)
                {
                    yield return new ShardParameterValue<TShard>(shd.Key, null, null);
                }
                else
                {
                    foreach (var prm in shd.Value)
                    {
                        yield return new ShardParameterValue<TShard>(shd.Key, prm.Key, prm.Value);
                    }
                }
            }
        }
        public IDictionary<TShard, IDictionary<string, object>> Shards { get; private set; }
    }
}
