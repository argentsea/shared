using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    public class ShardsValues<TShard> : ICollection, IEnumerable<ShardParameterValue<TShard>> where TShard : IComparable
    {
        private readonly object syncRoot = new object();

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
        }
        public ShardsValues(IList<TShard> list)
        {
            foreach (var shd in list)
            {
                this.Shards.Add(shd, new Dictionary<string, object>());
            }
        }

        public ShardsValues<TShard> Add(TShard shardId)
        {
            if (!Shards.ContainsKey(shardId))
            {
                Shards.Add(shardId, new Dictionary<string, object>());
            }
            return this;
        }
        public ShardsValues<TShard> Add(TShard shardId, string parameterName, object parameterValue)
        {
            if (Shards.TryGetValue(shardId, out var prms))
            {
                if (prms is null)
                {
                    Shards[shardId] = new Dictionary<string, object>() { { parameterName, parameterValue } };
                }
                else
                {
                    prms.Add(parameterName, parameterValue);
                }
            }
            else
            {
                Shards.Add(shardId, new Dictionary<string, object>() { { parameterName, parameterValue } });

            }
            return this;
        }

        public int Count
        {
            get {
                var parameterCount = 0;
                foreach (var shd in Shards)
                {
                    if (shd.Value is null || shd.Value.Count == 0)
                    {
                        parameterCount++;
                    }
                    else
                    {
                        parameterCount += shd.Value.Count;
                    }
                }
                return parameterCount;
            }
        }

        public bool IsSynchronized => throw new NotImplementedException();

        public object SyncRoot => syncRoot;

        public void CopyTo(Array array, int index)
        {
            var result = new ShardParameterValue<TShard>[this.Count];
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
                if (shd.Value is null || shd.Value.Count == 0)
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

        public void Merge(IList<TShard> shardIds)
        {
            foreach (var shardId in shardIds)
            {
                if (!Shards.Keys.Contains(shardId))
                {
                    Shards.Add(shardId, new Dictionary<string, object>());
                }
            }
        }
        public void Merge(ShardsValues<TShard> values)
        {
            foreach (var shd in values.Shards)
            {
                if (!Shards.Keys.Contains(shd.Key))
                {
                    var dtn = shd.Value;
                    if (!(dtn is null))
                    {
                        foreach (var kv in dtn)
                        {
                            Shards[shd.Key].Add(kv);
                        }
                    }
                }
                else
                {
                    Shards.Add(shd.Key, new Dictionary<string, object>());
                }
            }
        }
        public void Remove(TShard shardId)
        {
            if (Shards.ContainsKey(shardId))
            {
                Shards.Remove(shardId);
            }
        }
    }
}
