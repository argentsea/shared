// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace ArgentSea
{
    /// <summary>
	/// Immutable class representing a sharded record with a database compound key. The ShardChild consist of the (virtual) shardId, the recordId, and the childId.
    /// </summary>
    /// <typeparam name="TShard"></typeparam>
    /// <typeparam name="TRecord"></typeparam>
    /// <typeparam name="TChild"></typeparam>
    [Serializable]
    public struct ShardChild<TShard, TRecord, TChild> : IEquatable<ShardChild<TShard, TRecord, TChild>>, ISerializable
        where TShard : IComparable
        where TRecord : IComparable
        where TChild : IComparable
    {
        private readonly ShardKey<TShard, TRecord> _key;
        private readonly TChild _childId;

        public ShardKey<TShard, TRecord> Key {
            get { return _key;  }
        }

		public ShardChild(ShardKey<TShard, TRecord> key, TChild childRecordId)
        {
            _key = key;
            _childId = childRecordId;
        }
        public ShardChild(char origin, TShard shardId, TRecord recordId, TChild childRecordId)
        {
            _key = new ShardKey<TShard, TRecord>(origin, shardId, recordId);
            _childId = childRecordId;
        }
        /// <summary>
        /// ISerializer constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public ShardChild(SerializationInfo info, StreamingContext context)
        {
            if (info.MemberCount == 4)
            {
                char origin = info.GetChar("origin");
                TShard shardId = (TShard)info.GetValue("shardId", typeof(TShard));
                TRecord recordId = (TRecord)info.GetValue("recordId", typeof(TRecord));
                _key = new ShardKey<TShard, TRecord>(origin, shardId, recordId);
                _childId = (TChild)info.GetValue("childId", typeof(TChild));
            }
            else
            {
                var tmp = FromExternalString(info.GetString("shardChild"));
                _key = tmp.Key;
                _childId = tmp.ChildId;
            }
        }

        public TChild ChildId
		{
			get { return _childId; }
		}
		public char Origin
		{
			get
			{
				return _key.Origin;
			}
		}

		public TShard ShardId
		{
			get
			{
				return _key.ShardId;
			}
		}
		public TRecord RecordId
		{
			get
			{
				return _key.RecordId;
			}
		}

		public bool IsEmpty
        {
            get
            {
                return this.Key.IsEmpty && this.ChildId.CompareTo(default(TChild)) == 0;
            }
        }

        /// <summary>
        /// Given a list of Models with ShardKChild keys, returns a distinct list of shard Ids, except for the shard Id specified.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="shardId">The shard id of the shard to exclude. This is typically the current shard and this function is used to determine if any records are foreign to it.</param>
        /// <param name="records">The list of models to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public static ShardsValues<TShard> ShardList<TModel>(IList<IKeyedChildModel<TShard, TRecord, TChild>> records) where TModel : IKeyedChildModel<TShard, TRecord, TChild>
        {
            var result = new ShardsValues<TShard>();
            foreach (var record in records)
            {
                if (!result.Shards.ContainsKey(record.Key.ShardId))
                {
                    result.Add(record.Key.ShardId);
                }
            }
            return result;
        }

        /// <summary>
        /// Given a list of Models with ShardKChild keys, returns a distinct list of shard Ids, except for the shard Id specified.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="shardId">The shard id of the shard to exclude. This is typically the current shard and this function is used to determine if any records are foreign to it.</param>
        /// <param name="records">The list of models to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public static List<TModel> Merge<TModel>(List<TModel> master, List<TModel> replacements, bool appendUnmatchedReplacements = false) where TModel : IKeyedChildModel<TShard, TRecord, TChild>
            => (List<TModel>)Merge<TModel>((IList<TModel>)master, (IList<TModel>)replacements, appendUnmatchedReplacements);


        /// <summary>
        /// Given a list of ShardKey values, returns a distinct list of shard Ids, except for the shard Id specified.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="shardId">The shard id of the shard to exclude. This is typically the current shard and this function is used to determine if any records are foreign to it.</param>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public static ShardsValues<TShard> ShardListForeign(TShard shardId, List<ShardChild<TShard, TRecord, TChild>> records)
            => ShardListForeign(shardId, ((IList<ShardChild<TShard, TRecord, TChild>>)records));

        /// <summary>
        /// Given a list of ShardKey values, returns a distinct list of shard Ids, except for the shard Id specified.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="shardId">The shard id of the shard to exclude. This is typically the current shard and this function is used to determine if any records are foreign to it.</param>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public static ShardsValues<TShard> ShardListForeign(TShard shardId, IList<ShardChild<TShard, TRecord, TChild>> records)
        {
            var result = new ShardsValues<TShard>();
            foreach (var record in records)
            {
                if (!record.ShardId.Equals(shardId) && !result.Shards.ContainsKey(record.ShardId))
                {
                    result.Add(record.ShardId);
                }
            }
            return result;
        }

        /// <summary>
        /// Merge two lists by iterating master list and using replacement entry where keys match.
        /// </summary>
        /// <typeparam name="TModel">The of the list values.</typeparam>
        /// <param name="master">The list to be returned, possibly with some entries replaced.</param>
        /// <param name="replacements">A list of more complete records.</param>
        /// <returns>Merged list.</returns>
        public static IList<TModel> Merge<TModel>(IList<TModel> master, IList<TModel> replacements, bool appendUnmatchedReplacements = false) where TModel : IKeyedChildModel<TShard, TRecord, TChild>
        {
            if (master is null)
            {
                throw new ArgumentNullException(nameof(master));
            }
            if (replacements is null)
            {
                throw new ArgumentNullException(nameof(replacements));
            }
            var result = new List<TModel>(master);
            var track = new bool[replacements.Count];
            for (var i = 0; i < result.Count; i++)
            {
                for (var j = 0; j < replacements.Count; j++)
                {
                    if (result[i].Key.Equals(replacements[j].Key))
                    {
                        result[i] = replacements[j];
                        track[j] = true;
                        break;
                    }
                }
            }
            if (appendUnmatchedReplacements)
            {
                for (var i = 0; i < track.Length; i++)
                {
                    if (track[i])
                    {
                        result.Add(replacements[i]);
                    }
                }
            }
            return result;
        }


        public static ShardsValues<TShard> ShardListForeign<TModel>(TShard shardId, List<TModel> records) where TModel : IKeyedChildModel<TShard, TRecord, TChild>
            => ShardListForeign<TModel>(shardId, (IList<TModel>)records);

        /// <summary>
        /// Given a list of Models with ShardKey keys, returns a distinct list of shard Ids, except for the shard Id specified.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="shardId"></param>
        /// <param name="records">The list of models to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed and values not set.</returns>
        public static ShardsValues<TShard> ShardListForeign<TModel>(TShard shardId, IList<TModel> records) where TModel : IKeyedChildModel<TShard, TRecord, TChild>
        {
            var result = new ShardsValues<TShard>();
            foreach (var record in records)
            {
                if (!record.Key.ShardId.Equals(shardId) && !result.Shards.ContainsKey(record.Key.ShardId))
                {
                    result.Add(record.Key.ShardId);
                }
            }
            return result;
        }

        public bool Equals(ShardChild<TShard, TRecord, TChild> other)
        {
            return (other.Key == this.Key) && (other.ChildId.CompareTo(this.ChildId) == 0);
        }
        public override bool Equals(object obj)
        {
            if (obj is null || GetType() != obj.GetType())
            {
                return false;
            }
            var other = (ShardChild<TShard, TRecord, TChild>)obj;
            return (other.Key == this.Key) && (other.ChildId.CompareTo(this.ChildId) == 0);
        }
        public override int GetHashCode()
        {
            var aSChd = ShardKey<TShard, TRecord>.GetValueBytes(this._childId);
            var aResult = new byte[4];
            if (!(aSChd is null))
            {
                if (aSChd.Length > 0)
                {
                    aResult[0] = aSChd[0];
                }
                if (aSChd.Length > 1)
                {
                    aResult[1] = aSChd[1];
                }
                if (aSChd.Length > 2)
                {
                    aResult[2] = aSChd[2];
                }
                if (aSChd.Length > 3)
                {
                    aResult[3] = aSChd[3];
                }
            }

            return this.Key.GetHashCode() | BitConverter.ToInt32(aResult, 0);
        }

        internal byte[] ToArray()
        {
            var aOrigin = System.Text.Encoding.UTF8.GetBytes(new[] { this._key.Origin });
            var shardData = ShardKey<TShard, TRecord>.GetValueBytes(this._key.ShardId);
            var recordData = ShardKey<TShard, TRecord>.GetValueBytes(this._key.RecordId);
            var childData = ShardKey<TShard, TRecord>.GetValueBytes(this._childId);
            var aResult = new byte[aOrigin.Length + shardData.Length + recordData.Length + childData.Length + 1];
            aResult[0] = (byte)(aOrigin.Length | (1 << 2)); //origin length on bits 1 & 2, version (1) on bit 3.
            var resultIndex = 1;
            aOrigin.CopyTo(aResult, resultIndex);
            resultIndex += aOrigin.Length;
            shardData.CopyTo(aResult, resultIndex);
            resultIndex += shardData.Length;
            recordData.CopyTo(aResult, resultIndex);
            resultIndex += recordData.Length;
            childData.CopyTo(aResult, resultIndex);
            resultIndex = +childData.Length;
            return aResult;
        }

        /// <summary>
        /// Serializes ShardChild data into a URL-safe string with a checksum, optionally including a concurrency stamp.
        /// </summary>
        /// <param name="includeConcurrencyStamp">Indicates whether the string should include concurrancy stamp data, if defined.</param>
        /// <returns>A URL-safe string that can be re-serialized into a shard child.</returns>
        public string ToExternalString()
        {
            return StringExtensions.SerializeToExternalString(ToArray());
        }
        public override string ToString()
        {
            return $"{{ \"origin\": \"{_key.Origin}\", \"shardId\": \"{_key.ShardId.ToString()}\", \"recordId\": \"{_key.RecordId.ToString()}\", \"childRecordId\": \"{this._childId.ToString()}\"}}";
        }
        public static ShardChild<TShard, TRecord, TChild> FromExternalString(string value)
        {
            var aValues = StringExtensions.SerializeFromExternalString(value);
            TShard shardId = default(TShard);
            TRecord recordId = default(TRecord);
            TChild childId = default(TChild);

            int orgnLen = aValues[0] & 3;
            var orgn = System.Text.Encoding.UTF8.GetString(aValues, 1, orgnLen)[0];
            var pos = orgnLen + 1;

            var typeShard = typeof(TShard);
            shardId = ShardKey<TShard, TRecord>.ConvertFromBytes(aValues, ref pos, typeShard);
            var typeRecord = typeof(TRecord);
            recordId = ShardKey<TShard, TRecord>.ConvertFromBytes(aValues, ref pos, typeRecord);
            var typeChild = typeof(TChild);
            childId = ShardKey<TShard, TRecord>.ConvertFromBytes(aValues, ref pos, typeChild);
           
            return new ShardChild<TShard, TRecord, TChild>(orgn, shardId, recordId, childId);

        }
        public static bool operator ==(ShardChild<TShard, TRecord, TChild> sc1, ShardChild<TShard, TRecord, TChild> sc2)
        {
            return sc1.Equals(sc2);
        }
        public static bool operator !=(ShardChild<TShard, TRecord, TChild> sc1, ShardChild<TShard, TRecord, TChild> sc2)
        {
            return !sc1.Equals(sc2);
        }
        public static ShardChild<TShard, TRecord, TChild> Empty
        {
            get
            {
                return new ShardChild<TShard, TRecord, TChild>(new ShardKey<TShard, TRecord>('0', default(TShard), default(TRecord)), default(TChild));
            }
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("shardChild", ToExternalString());
            info.AddValue("origin", _key.Origin);
            info.AddValue("shardId", _key.ShardId);
            info.AddValue("recordId", _key.RecordId);
            info.AddValue("childId", _childId);
        }
        public void ThrowIfInvalidOrigin(char expectedOrigin)
        {
            if (_key.Origin != expectedOrigin)
            {
                throw new InvalidDataOriginException(expectedOrigin, _key.Origin);
            }
        }
    }
}
