// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using ArgentSea.ShardKeys;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace ArgentSea
{
    /// <summary>
	/// Immutable class representing a sharded record with a database compound key. The ShardKey consist of the (virtual) shardId, the recordId, and the childId.
    /// </summary>
    /// <typeparam name="TRecord"></typeparam>
    /// <typeparam name="TChild"></typeparam>
    [Serializable]
    public struct ShardKey<TRecord, TChild, TGrandChild> : IEquatable<ShardKey<TRecord, TChild, TGrandChild>>, IShardKey
        where TRecord : IComparable
        where TChild : IComparable
        where TGrandChild : IComparable
    {
        internal readonly ShardKey<TRecord, TChild> _key;
        private readonly TGrandChild _grandChildId;

        #region Constructors
        public ShardKey(short shardId, TRecord recordId, TChild childRecordId, TGrandChild grandChildRecordId)
        {
            if (!ShardKeySerialization.TryEncodeTypeMetadata(typeof(TRecord), typeof(TChild), typeof(TGrandChild), out var metadata))
            {
                throw new InvalidShardKeyMetadataException();
            }
            _key = new ShardKey<TRecord, TChild>(metadata, shardId, recordId, childRecordId);
            _grandChildId = grandChildRecordId;
        }

        internal ShardKey(ReadOnlyMemory<byte> typeMetadata, short shardId, TRecord recordId, TChild childRecordId, TGrandChild grandChildRecordId)
        {
            _key = new ShardKey<TRecord, TChild>(typeMetadata, shardId, recordId, childRecordId);
            _grandChildId = grandChildRecordId;
        }

        /// <summary>
		/// Initiaizes a new instance from a readonly data array. This can but the raw data (from ToArray()) or UTF8 Span (ToUtf8()).
        /// </summary>
        /// <param name="data">The readonly span containing the shardKey data. This can be generated using the ToArray() method or ToUtf8() method.</param>
        public ShardKey(ReadOnlySpan<byte> data)
        {
            int metaLen = data[0] & 3;
            var isUtf8 = ((data[0] & 128) != 128);
            if (isUtf8) // utf8 encoding chars do not use high bits.
            {
                data = StringExtensions.Decode(data).Span;
                metaLen = data[0] & 3;
            }
            var typRecord = typeof(TRecord);
            var typChild = typeof(TChild);
            var typGrandChild = typeof(TGrandChild);
            if (!ShardKeySerialization.TryEncodeTypeMetadata(typRecord, typChild, typGrandChild, out var metadata))
            {
                throw new InvalidShardKeyMetadataException();
            }
            var metadataSpan = metadata.Span;
            var saved = data.Slice(1, metaLen);
            if (metadata.Length != 3 && saved.Length != 3)
            {
                throw new InvalidShardKeyMetadataException();
            }
            if (metadataSpan[0] != saved[0])
            {
                throw new InvalidShardKeyMetadataException(typRecord);
            }
            if (metadataSpan[1] != saved[1])
            {
                throw new InvalidShardKeyMetadataException(typChild);
            }
            if (metadataSpan[2] != saved[2])
            {
                throw new InvalidShardKeyMetadataException(typGrandChild);
            }
            var pos = metaLen + 1;
            var shardId = BitConverter.ToInt16(data.Slice(pos));
            pos += 2;
            if (!ShardKeySerialization.TryConvertFromBytes(ref data, ref pos, typRecord, out dynamic recordId))
            {
                throw new InvalidDataException("Could not parse binary record data to create a ShardKey.");
            }
            if (!ShardKeySerialization.TryConvertFromBytes(ref data, ref pos, typChild, out dynamic childId))
            {
                throw new InvalidDataException("Could not parse binary child data to create a ShardKey.");
            }
            this._key = new ShardKey<TRecord, TChild>(metadata, shardId, recordId, childId);

            if (!ShardKeySerialization.TryConvertFromBytes(ref data, ref pos, typGrandChild, out dynamic grandChildResult))
            {
                throw new InvalidDataException("Could not parse binary grandchild data to create a ShardKey.");
            }
            this._grandChildId = grandChildResult;
        }

        public static bool TryParse(ReadOnlySpan<byte> data, out ShardKey<TRecord, TChild, TGrandChild> result)
        {
            result = ShardKey<TRecord, TChild, TGrandChild>.Empty;
            if (data.Length < 4) // smallest possible type 1 + 2 + x (metadata + short + TRecord.Length)
            {
                return false;
            }
            int metaLen = data[0] & 3;
            var isUtf8 = ((data[0] & 128) != 128);
            if (isUtf8) // utf8 encoding chars do not use high bits.
            {
                data = StringExtensions.Decode(data).Span;
                metaLen = data[0] & 3;
            }
            if (data.Length < 3 + metaLen) // new smallest possible type orgn + 2 + x (metadata + short + TRecord.Length)
            {
                return false;
            }
            var typRecord = typeof(TRecord);
            var typChild = typeof(TChild);
            var typGrandChild = typeof(TGrandChild);
            if (!ShardKeySerialization.TryEncodeTypeMetadata(typRecord, typChild, typGrandChild, out var metadata))
            {
                return false;
            }
            var metadataSpan = metadata.Span;
            var saved = data.Slice(1, metaLen);
            if (metadataSpan.Length != 1 && saved.Length != 1)
            {
                return false;
            }
            if (metadataSpan[0] != saved[0])
            {
                return false;
            }

            var pos = metaLen + 1;
            short shardId = BitConverter.ToInt16(data.Slice(pos));
            pos += 2;
            if (!ShardKeySerialization.TryConvertFromBytes(ref data, ref pos, typRecord, out dynamic recordId))
            {
                return false;
            }
            if (!ShardKeySerialization.TryConvertFromBytes(ref data, ref pos, typChild, out dynamic childId))
            {
                return false;
            }
            if (!ShardKeySerialization.TryConvertFromBytes(ref data, ref pos, typGrandChild, out dynamic grandchildId))
            {
                return false;
            }
            result = new ShardKey<TRecord, TChild, TGrandChild>(metadata, shardId, (TRecord)recordId, (TChild)childId, (TGrandChild)grandchildId);
            return true;
        }
        #endregion


        public TChild ChildId { get => _key.ChildId; }

        public TGrandChild GrandChildId { get => _grandChildId; }

        public short ShardId { get => _key.ShardId; }

        public TRecord RecordId { get => _key.RecordId;  }

        public bool IsEmpty { get => _key.IsEmpty && _grandChildId.CompareTo(default(TGrandChild)) == 0; }

        /// <summary>
        /// Given a list of Models with ShardKChild keys, returns a distinct list of shard Ids, except for the shard Id specified.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="shardId">The shard id of the shard to exclude. This is typically the current shard and this function is used to determine if any records are foreign to it.</param>
        /// <param name="records">The list of models to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public static ShardsValues ToShardsValues<TModel>(IList<IKeyedModel<TRecord, TChild, TGrandChild>> records) where TModel : IKeyedModel<TRecord, TChild, TGrandChild>
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
        public static List<TModel> Merge<TModel>(List<TModel> master, List<TModel> replacements, bool appendUnmatchedReplacements = false) where TModel : IKeyedModel<TRecord, TChild, TGrandChild>
            => Merge<TModel>((IList<TModel>)master, (IList<TModel>)replacements, appendUnmatchedReplacements);

        /// <summary>
        /// Given a list of Models with ShardKChild keys, returns a distinct list of shard Ids, except for the shard Id specified.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="shardId">The shard id of the shard to exclude. This is typically the current shard and this function is used to determine if any records are foreign to it.</param>
        /// <param name="records">The list of models to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public static List<TModel> Merge<TModel>(List<TModel> master, IList<TModel> replacements, bool appendUnmatchedReplacements = false) where TModel : IKeyedModel<TRecord, TChild, TGrandChild>
            => Merge<TModel>((IList<TModel>)master, replacements, appendUnmatchedReplacements);

        /// <summary>
        /// Given a list of Models with ShardKChild keys, returns a distinct list of shard Ids, except for the shard Id specified.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="shardId">The shard id of the shard to exclude. This is typically the current shard and this function is used to determine if any records are foreign to it.</param>
        /// <param name="records">The list of models to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public static List<TModel> Merge<TModel>(IList<TModel> master, List<TModel> replacements, bool appendUnmatchedReplacements = false) where TModel : IKeyedModel<TRecord, TChild, TGrandChild>
            => (List<TModel>)Merge<TModel>(master, (IList<TModel>)replacements, appendUnmatchedReplacements);

        /// <summary>
        /// Given a list of ShardKey values, returns a distinct list of shard Ids, except for the shard Id of the current shard.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public ShardsValues ForeignShards(IList<ShardKey<TRecord, TChild, TGrandChild>> records)
            => ShardsValues.ShardListForeign<TRecord, TChild, TGrandChild>(_key.ShardId, records);

        /// <summary>
        /// Given a list of ShardKey values, returns a distinct list of shard Ids, except for the shard Id of the current shard.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public ShardsValues ForeignShards(List<ShardKey<TRecord, TChild, TGrandChild>> records)
            => ShardsValues.ShardListForeign<TRecord, TChild, TGrandChild>(_key.ShardId, (IList<ShardKey<TRecord, TChild, TGrandChild>>)records);

        /// <summary>
        /// Given a list of Models with ShardChld keys, returns a distinct list of shard Ids, except for the shard Id of the current shard.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public ShardsValues ForeignShards<TModel>(IList<TModel> records) where TModel : IKeyedModel<TRecord, TChild, TGrandChild>
            => ShardsValues.ShardListForeign<TRecord, TChild, TGrandChild, TModel>(_key.ShardId, records);

        /// <summary>
        /// Given a list of Models with ShardChld keys, returns a distinct list of shard Ids, except for the shard Id of the current shard.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public ShardsValues ForeignShards<TModel>(List<TModel> records) where TModel : IKeyedModel<TRecord, TChild, TGrandChild>
            => ShardsValues.ShardListForeign<TRecord, TChild, TGrandChild, TModel>(_key.ShardId, (IList<TModel>)records);
        
        /// <summary>
        /// Merge two lists by iterating master list and using replacement entry where keys match.
        /// </summary>
        /// <typeparam name="TModel">The of the list values.</typeparam>
        /// <param name="master">The list to be returned, possibly with some entries replaced.</param>
        /// <param name="replacements">A list of more complete records.</param>
        /// <returns>Merged list.</returns>
        public static List<TModel> Merge<TModel>(IList<TModel> master, IList<TModel> replacements, bool appendUnmatchedReplacements = false) where TModel : IKeyedModel<TRecord, TChild, TGrandChild>
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
        public static ShardsValues ShardListForeign<TModel>(short shardId, IList<TModel> records) where TModel : IKeyedModel<TRecord, TChild, TGrandChild>
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

        public static ShardsValues ShardListForeign(short shardId, IList<ShardKey<TRecord, TChild, TGrandChild>> records)
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

        public bool Equals(ShardKey<TRecord, TChild, TGrandChild> other)
        {
            return (other._key == this._key) && (other.GrandChildId.CompareTo(_grandChildId) == 0);
        }
        public override bool Equals(object obj)
        {
            if (obj is null || GetType() != obj.GetType())
            {
                return false;
            }
            var other = (ShardKey<TRecord, TChild, TGrandChild>)obj;
            return (other._key == this._key) && (other.GrandChildId.CompareTo(_grandChildId) == 0);
        }

        public override int GetHashCode()
        {
            var aSChd = ShardKeySerialization.GetValueBytes(this._grandChildId);
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

        public ReadOnlyMemory<byte> ToArray()
        {
            var shardData = ShardKeySerialization.GetValueBytes(this._key.ShardId);
            var recordData = ShardKeySerialization.GetValueBytes(this._key.RecordId);
            var childData = ShardKeySerialization.GetValueBytes(this._key.ChildId);
            var grandChildData = ShardKeySerialization.GetValueBytes(_grandChildId);
            var metaLen = _key._key._typeMetadata.Length;
            var aResult = new byte[metaLen + shardData.Length + recordData.Length + childData.Length + grandChildData.Length + 1];
            aResult[0] = (byte)(metaLen | 128); //metadata length on bits 1 & 2, No-utf8 flag on bit 8.
            var resultIndex = 1;
            _key._key._typeMetadata.ToArray().CopyTo(aResult, resultIndex);
            resultIndex += metaLen;
            shardData.CopyTo(aResult, resultIndex);
            resultIndex += shardData.Length;
            recordData.CopyTo(aResult, resultIndex);
            resultIndex += recordData.Length;
            childData.CopyTo(aResult, resultIndex);
            resultIndex += childData.Length;
            grandChildData.CopyTo(aResult, resultIndex);
            resultIndex += grandChildData.Length;
            return aResult;
        }

        /// <summary>
        /// Serializes ShardKey data into a URL-safe string with a checksum, optionally including a concurrency stamp.
        /// </summary>
        /// <param name="includeConcurrencyStamp">Indicates whether the string should include concurrancy stamp data, if defined.</param>
        /// <returns>A URL-safe string that can be re-serialized into a shard child.</returns>
        public string ToExternalString()
        {
            return StringExtensions.SerializeToExternalString(ToArray().Span);
        }
        public override string ToString()
        {
            return $"{{ \"shard\": {_key.ShardId.ToString()}, \"ids\": [\"{_key.RecordId.ToString()}\", \"{_key.ChildId.ToString()}\", \"{this._grandChildId.ToString()}\"]}}";
        }

        /// <summary>
        /// Create a new instance of ShardKey from a URL-safe string; this method is the inverse of ToExternalString().
        /// </summary>
        /// <param name="value">A string generated by the ToExternalString() method.</param>
        /// <returns>An instance of this type.</returns>
        public static ShardKey<TRecord, TChild, TGrandChild> FromExternalString(string value)
        {
            var aValues = StringExtensions.SerializeFromExternalString(value);
            return new ShardKey<TRecord, TChild, TGrandChild> (aValues.Span);
        }

        /// <summary>
        /// Create a new instance of ShardKey from binary data; this method is the inverse of ToArray().
        /// </summary>
        /// <param name="value">A binary value generaeted by the ToArraay() method.</param>
        /// <returns>An instance of this type.</returns>
        //public static ShardKey<TRecord, TChild, TGrandChild> FromSpan(ReadOnlySpan<byte> value)
        //{
        //    return new ShardKey<TRecord, TChild, TGrandChild>(value);
        //}

        /// <summary>
        /// Create a new instance of ShardKey from UTF8 encoded binary data; this method is the inverse of ToUtf8().
        /// </summary>
        /// <param name="value">A binary value generaeted by the ToUtf8() method.</param>
        /// <returns>An instance of this type.</returns>
        //public static ShardKey<TRecord, TChild, TGrandChild> FromUtf8(ReadOnlySpan<byte> encoded)
        //{
        //    return FromSpan(StringExtensions.Decode(encoded).Span);
        //}

        /// <summary>
        /// Serializes ShardKey data into a URL-safe string with a checksum
        /// </summary>
        /// <returns>A Uft8 encoded array.</returns>
        public ReadOnlyMemory<byte> ToUtf8()
        {
            var aValues = ToArray();
            return StringExtensions.EncodeToUtf8(aValues.Span);
        }

        public static bool operator ==(ShardKey<TRecord, TChild, TGrandChild> sc1, ShardKey<TRecord, TChild, TGrandChild> sc2)
        {
            return sc1.Equals(sc2);
        }
        public static bool operator !=(ShardKey<TRecord, TChild, TGrandChild> sc1, ShardKey<TRecord, TChild, TGrandChild> sc2)
        {
            return !sc1.Equals(sc2);
        }

        private static Lazy<ShardKey<TRecord, TChild, TGrandChild>> _lazyEmpty = new Lazy<ShardKey<TRecord, TChild, TGrandChild>>(() 
            => new ShardKey<TRecord, TChild, TGrandChild>(0, default(TRecord), default(TChild), default(TGrandChild)));

        public static ShardKey<TRecord, TChild, TGrandChild> Empty
        {
            get
            {
                return _lazyEmpty.Value;
            }
        }
    }
}
