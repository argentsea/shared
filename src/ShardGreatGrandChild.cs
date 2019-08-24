// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace ArgentSea
{
    /// <summary>
	/// Immutable class representing a sharded record with a database compound key. The ShardKey consist of the (virtual) shardId, the recordId, and the childId.
    /// </summary>
    /// <typeparam name="TRecord"></typeparam>
    /// <typeparam name="TChild"></typeparam>
    [Serializable]
    public struct ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> : IEquatable<ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>>, ISerializable
        where TRecord : IComparable
        where TChild : IComparable
        where TGrandChild : IComparable
        where TGreatGrandChild : IComparable
    {
        private readonly ShardKey<TRecord, TChild, TGrandChild> _key;
        private readonly TGreatGrandChild _greatGrandChildId;

        public ShardKey<TRecord> Parent {
            get { return _key.Parent;  }
        }

        public ShardKey<TRecord, TChild> Child
        {
            get { return _key.Child; }
        }

        public ShardKey<TRecord, TChild, TGrandChild> GrandChild
        {
            get { return _key; }
        }

        public ShardKey(ShardKey<TRecord, TChild, TGrandChild> key, TGreatGrandChild grandChildRecordId)
        {
            _key = key;
            _greatGrandChildId = grandChildRecordId;
        }
        public ShardKey(char origin, short shardId, TRecord recordId, TChild childRecordId, TGrandChild grandChildRecordId, TGreatGrandChild greatGrandChildRecordId)
        {
            _key = new ShardKey<TRecord, TChild, TGrandChild>(origin, shardId, recordId, childRecordId, grandChildRecordId);
            _greatGrandChildId = greatGrandChildRecordId;
        }
        /// <summary>
        /// ISerializer constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public ShardKey(SerializationInfo info, StreamingContext context)
        {
            if (info.MemberCount == 5)
            {
                char origin = info.GetChar("origin");
                var shardId = (short)info.GetValue("shardId", typeof(short));
                TRecord recordId = (TRecord)info.GetValue("recordId", typeof(TRecord));
                TChild childId = (TChild)info.GetValue("childId", typeof(TChild));
                TGrandChild grandChildId = (TGrandChild)info.GetValue("grandChildId", typeof(TGrandChild));
                _key = new ShardKey<TRecord, TChild, TGrandChild>(origin, shardId, recordId, childId, grandChildId);
                _greatGrandChildId = (TGreatGrandChild)info.GetValue("greatGrandChildId", typeof(TGreatGrandChild));
            }
            else
            {
                var tmp = FromExternalString(info.GetString("ShardKey"));
                _key = tmp.GrandChild;
                _greatGrandChildId = tmp.GreatGrandChildId;
            }
        }

        public short ShardId
        {
            get { return _key.ShardId; }
        }

        public TRecord RecordId
        {
            get { return _key.RecordId; }
        }

        public TChild ChildId
        {
            get { return _key.ChildId; }
        }

        public TGrandChild GrandChildId
        {
            get { return _key.GrandChildId; }
        }

        public TGreatGrandChild GreatGrandChildId
        {
            get { return _greatGrandChildId; }
        }

        public char Origin
		{
			get
			{
				return _key.Origin;
			}
		}

		public bool IsEmpty
        {
            get
            {
                return _key.IsEmpty && _greatGrandChildId.CompareTo(default(TGreatGrandChild)) == 0;
            }
        }

        /// <summary>
        /// Given a list of Models with ShardKChild keys, returns a distinct list of shard Ids, except for the shard Id specified.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="shardId">The shard id of the shard to exclude. This is typically the current shard and this function is used to determine if any records are foreign to it.</param>
        /// <param name="records">The list of models to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public static ShardsValues ToShardsValues<TModel>(IList<IKeyedModel<TRecord, TChild, TGrandChild, TGreatGrandChild>> records) where TModel : IKeyedModel<TRecord, TChild, TGrandChild, TGreatGrandChild>
        {
            var result = new ShardsValues();
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
        public static List<TModel> Merge<TModel>(List<TModel> master, List<TModel> replacements, bool appendUnmatchedReplacements = false) where TModel : IKeyedModel<TRecord, TChild, TGrandChild, TGreatGrandChild>
            => Merge<TModel>((IList<TModel>)master, (IList<TModel>)replacements, appendUnmatchedReplacements);

        /// <summary>
        /// Given a list of Models with ShardKChild keys, returns a distinct list of shard Ids, except for the shard Id specified.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="shardId">The shard id of the shard to exclude. This is typically the current shard and this function is used to determine if any records are foreign to it.</param>
        /// <param name="records">The list of models to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public static List<TModel> Merge<TModel>(List<TModel> master, IList<TModel> replacements, bool appendUnmatchedReplacements = false) where TModel : IKeyedModel<TRecord, TChild, TGrandChild, TGreatGrandChild>
            => Merge<TModel>((IList<TModel>)master, replacements, appendUnmatchedReplacements);

        /// <summary>
        /// Given a list of Models with ShardKChild keys, returns a distinct list of shard Ids, except for the shard Id specified.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="shardId">The shard id of the shard to exclude. This is typically the current shard and this function is used to determine if any records are foreign to it.</param>
        /// <param name="records">The list of models to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public static List<TModel> Merge<TModel>(IList<TModel> master, List<TModel> replacements, bool appendUnmatchedReplacements = false) where TModel : IKeyedModel<TRecord, TChild, TGrandChild, TGreatGrandChild>
            => (List<TModel>)Merge<TModel>(master, (IList<TModel>)replacements, appendUnmatchedReplacements);

        /// <summary>
        /// Given a list of ShardKey values, returns a distinct list of shard Ids, except for the shard Id of the current shard.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public ShardsValues ForeignShards(IList<ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>> records)
            => ShardsValues.ShardListForeign<TRecord, TChild, TGrandChild, TGreatGrandChild>(_key.ShardId, records);

        /// <summary>
        /// Given a list of ShardKey values, returns a distinct list of shard Ids, except for the shard Id of the current shard.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public ShardsValues ForeignShards(List<ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>> records)
            => ShardsValues.ShardListForeign<TRecord, TChild, TGrandChild, TGreatGrandChild>(_key.ShardId, (IList<ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>>)records);

        /// <summary>
        /// Given a list of Models with ShardChld keys, returns a distinct list of shard Ids, except for the shard Id of the current shard.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public ShardsValues ForeignShards<TModel>(IList<TModel> records) where TModel : IKeyedModel<TRecord, TChild, TGrandChild, TGreatGrandChild>
            => ShardsValues.ShardListForeign<TRecord, TChild, TGrandChild, TGreatGrandChild, TModel>(_key.ShardId, records);

        /// <summary>
        /// Given a list of Models with ShardChld keys, returns a distinct list of shard Ids, except for the shard Id of the current shard.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public ShardsValues ForeignShards<TModel>(List<TModel> records) where TModel : IKeyedModel<TRecord, TChild, TGrandChild, TGreatGrandChild>
            => ShardsValues.ShardListForeign<TRecord, TChild, TGrandChild, TGreatGrandChild, TModel>(_key.ShardId, (IList<TModel>)records);

        /// <summary>
        /// Merge two lists by iterating master list and using replacement entry where keys match.
        /// </summary>
        /// <typeparam name="TModel">The of the list values.</typeparam>
        /// <param name="master">The list to be returned, possibly with some entries replaced.</param>
        /// <param name="replacements">A list of more complete records.</param>
        /// <returns>Merged list.</returns>
        public static List<TModel> Merge<TModel>(IList<TModel> master, IList<TModel> replacements, bool appendUnmatchedReplacements = false) where TModel : IKeyedModel<TRecord, TChild, TGrandChild, TGreatGrandChild>
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

        /// <summary>
        /// Given a list of Models with ShardKey keys, returns a distinct list of shard Ids, except for the shard Id specified.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="shardId"></param>
        /// <param name="records">The list of models to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed and values not set.</returns>
        public static ShardsValues ShardListForeign<TModel>(short shardId, IList<TModel> records) where TModel : IKeyedModel<TRecord, TChild, TGrandChild, TGreatGrandChild>
        {
            var result = new ShardsValues();
            foreach (var record in records)
            {
                if (!record.Key.ShardId.Equals(shardId) && !result.Shards.ContainsKey(record.Key.ShardId))
                {
                    result.Add(record.Key.ShardId);
                }
            }
            return result;
        }

        public static ShardsValues ShardListForeign(short shardId, IList<ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>> records)
        {
            var result = new ShardsValues();
            foreach (var record in records)
            {
                if (!record.ShardId.Equals(shardId) && !result.Shards.ContainsKey(record.ShardId))
                {
                    result.Add(record.ShardId);
                }
            }
            return result;
        }

        public bool Equals(ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> other)
        {
            return (other.GrandChild == _key) && (other.GreatGrandChildId.CompareTo(_greatGrandChildId) == 0);
        }
        public override bool Equals(object obj)
        {
            if (obj is null || GetType() != obj.GetType())
            {
                return false;
            }
            var other = (ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>)obj;
            return (other.GrandChild == _key) && (other.GreatGrandChildId.CompareTo(_greatGrandChildId) == 0);
        }

        public override int GetHashCode()
        {
            var aSChd = ShardKey<TRecord>.GetValueBytes(this._greatGrandChildId);
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

            return _key.GetHashCode() | BitConverter.ToInt32(aResult, 0);
        }

        internal byte[] ToArray()
        {
            var aOrigin = System.Text.Encoding.UTF8.GetBytes(new[] { this._key.Origin });
            var shardData = ShardKey<TRecord>.GetValueBytes(_key.ShardId);
            var recordData = ShardKey<TRecord>.GetValueBytes(_key.RecordId);
            var childData = ShardKey<TRecord>.GetValueBytes(_key.ChildId);
            var grandChildData = ShardKey<TRecord>.GetValueBytes(_key.GrandChildId);
            var greatGrandChildData = ShardKey<TRecord>.GetValueBytes(_greatGrandChildId);

            var aResult = new byte[aOrigin.Length + shardData.Length + recordData.Length + childData.Length + grandChildData.Length + 1];
            aResult[0] = (byte)(aOrigin.Length | (1 << 2)); //origin length on bits 1 & 2, version (1) on bit 3.
            var resultIndex = 1;
            aOrigin.CopyTo(aResult, resultIndex);
            resultIndex += aOrigin.Length;
            shardData.CopyTo(aResult, resultIndex);
            resultIndex += shardData.Length;
            recordData.CopyTo(aResult, resultIndex);
            resultIndex += recordData.Length;
            childData.CopyTo(aResult, resultIndex);
            resultIndex += childData.Length;
            grandChildData.CopyTo(aResult, resultIndex);
            resultIndex += grandChildData.Length;
            greatGrandChildData.CopyTo(aResult, resultIndex);
            resultIndex += greatGrandChildData.Length;
            return aResult;
        }

        /// <summary>
        /// Serializes ShardKey data into a URL-safe string with a checksum, optionally including a concurrency stamp.
        /// </summary>
        /// <param name="includeConcurrencyStamp">Indicates whether the string should include concurrancy stamp data, if defined.</param>
        /// <returns>A URL-safe string that can be re-serialized into a shard child.</returns>
        public string ToExternalString()
        {
            return StringExtensions.SerializeToExternalString(ToArray());
        }
        public override string ToString()
        {
            return $"{{ \"origin\": \"{_key.Origin}\", \"shardId\": \"{_key.ShardId.ToString()}\", \"recordId\": \"{_key.RecordId.ToString()}\", \"childId\": \"{_key.ChildId.ToString()}\", \", \"grandChildId\": \"{_key.GrandChildId.ToString()}\", \"greatGrandChildId\": \"{_greatGrandChildId.ToString()}\"}}";
        }
        public static ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> FromExternalString(string value)
        {
            var aValues = StringExtensions.SerializeFromExternalString(value);

            int orgnLen = aValues[0] & 3;
            var orgn = System.Text.Encoding.UTF8.GetString(aValues, 1, orgnLen)[0];
            var pos = orgnLen + 1;

            short shardId = ShardKey<TRecord>.ConvertFromBytes(aValues, ref pos, typeof(short));
            TRecord recordId = ShardKey<TRecord>.ConvertFromBytes(aValues, ref pos, typeof(TRecord));
            TChild childId = ShardKey<TRecord>.ConvertFromBytes(aValues, ref pos, typeof(TChild));
            TGrandChild grandChildId = ShardKey<TRecord>.ConvertFromBytes(aValues, ref pos, typeof(TGrandChild));
            TGreatGrandChild greatGrandChildId = ShardKey<TRecord>.ConvertFromBytes(aValues, ref pos, typeof(TGreatGrandChild));

            return new ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>(orgn, shardId, recordId, childId, grandChildId, greatGrandChildId);

        }
        public static bool operator ==(ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> sc1, ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> sc2)
        {
            return sc1.Equals(sc2);
        }
        public static bool operator !=(ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> sc1, ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> sc2)
        {
            return !sc1.Equals(sc2);
        }
        public static ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> Empty
        {
            get
            {
                return new ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>(new ShardKey<TRecord, TChild, TGrandChild>('0', 0, default(TRecord), default(TChild), default(TGrandChild)), default(TGreatGrandChild));
            }
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("shardChild", ToExternalString());
            info.AddValue("origin", _key.Origin);
            info.AddValue("shardId", _key.ShardId);
            info.AddValue("recordId", _key.RecordId);
            info.AddValue("childId", _key.ChildId);
            info.AddValue("grandChildId", _key.GrandChildId);
            info.AddValue("greateGrandChildId", _greatGrandChildId);
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
