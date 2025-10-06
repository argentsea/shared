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
	/// Immutable class representing a sharded record with a database compound key. The ShardKey consist of the (virtual) shardId, the recordId, the childId, and the grandChildId.
    /// </summary>
    /// <typeparam name="TRecord"></typeparam>
    /// <typeparam name="TChild"></typeparam>
    [Serializable]
    public struct ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> : IEquatable<ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>>, IShardKey
        where TRecord : IComparable
        where TChild : IComparable
        where TGrandChild : IComparable
        where TGreatGrandChild : IComparable
    {
        private readonly short _shardId;
        private readonly TRecord _recordId;
        private readonly TChild _childId;
        private readonly TGrandChild _grandChildId;
        private readonly TGreatGrandChild _greatGrandChildId;
        internal readonly ReadOnlyMemory<byte> _typeMetadata;

        #region Constructors
        public ShardKey(short shardId, TRecord recordId, TChild childRecordId, TGrandChild grandChildRecordId, TGreatGrandChild greatGrandChildRecordId)
        {
            if (!ShardKeySerialization.TryEncodeTypeMetadata(typeof(TRecord), typeof(TChild), typeof(TGrandChild), typeof(TGreatGrandChild), out var metadata))
            {
                throw new InvalidShardKeyMetadataException();
            }
            _typeMetadata = metadata;
            _shardId = shardId;
            _recordId = recordId;
            _childId = childRecordId;
            _grandChildId = grandChildRecordId;
            _greatGrandChildId = greatGrandChildRecordId;
        }

        internal ShardKey(ReadOnlyMemory<byte> typeMetadata, short shardId, TRecord recordId, TChild childRecordId, TGrandChild grandChildRecordId, TGreatGrandChild greatGrandChildRecordId)
        {
            _typeMetadata = typeMetadata;
            _shardId = shardId;
            _recordId = recordId;
            _childId = childRecordId;
            _grandChildId = grandChildRecordId;
            _greatGrandChildId = greatGrandChildRecordId;
        }

        /// <summary>
		/// Initiaizes a new instance from a readonly data array. This can be the raw data (from ToArray()) or UTF8 Span (ToUtf8()).
        /// </summary>
        /// <param name="data">The readonly span containing the shardKey data. This can be generated using the ToArray() method or ToUtf8() method.</param>
        public ShardKey(ReadOnlyMemory<byte> data) : this(new ReadOnlySpan<byte>(data.ToArray()))
        {

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
            var typGreatGrandChild = typeof(TGreatGrandChild);
            if (!ShardKeySerialization.TryEncodeTypeMetadata(typRecord, typChild, typGrandChild, typGreatGrandChild, out var metadata))
            {
                throw new InvalidShardKeyMetadataException();
            }
            var metadataSpan = metadata.Span;
            var saved = data.Slice(1, metaLen);
            if (metadata.Length != 3 || saved.Length != 3)
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
            if (!ShardKeySerialization.TryConvertFromBytes(ref data, ref pos, typGrandChild, out dynamic grandChildId))
            {
                throw new InvalidDataException("Could not parse binary grandchild data to create a ShardKey.");
            }

            _typeMetadata = metadata;
            _shardId = shardId;
            _recordId = recordId;
            _childId = childId;
            _grandChildId = grandChildId;

            if (!ShardKeySerialization.TryConvertFromBytes(ref data, ref pos, typGreatGrandChild, out dynamic greatGrandChildResult))
            {
                throw new InvalidDataException("Could not parse binary great-grandchild data to create a ShardKey.");
            }
            this._greatGrandChildId = greatGrandChildResult;
        }

        public static bool TryParse(ReadOnlySpan<byte> data, out ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> result)
        {
            result = ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>.Empty;
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
            if (data.Length < 3 + metaLen) // new smallest possible type metadata + 2 + x (metadata + short + TRecord.Length)
            {
                return false;
            }
            var typRecord = typeof(TRecord);
            var typChild = typeof(TChild);
            var typGrandChild = typeof(TGrandChild);
            var typGreatGrandChild = typeof(TGreatGrandChild);
            if (!ShardKeySerialization.TryEncodeTypeMetadata(typRecord, typChild, typGrandChild, typGreatGrandChild, out var metadata))
            {
                return false;
            }
            var metadataSpan = metadata.Span;
            var saved = data.Slice(1, metaLen);
            if (metadataSpan.Length != 3 && saved.Length != 3 && metadataSpan[0] != saved[0] && metadataSpan[1] != saved[1] && metadataSpan[2] != saved[2])
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
            if (!ShardKeySerialization.TryConvertFromBytes(ref data, ref pos, typGreatGrandChild, out dynamic greatgrandchildId))
            {
                return false;
            }
            result = new ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>(metadata, shardId, (TRecord)recordId, (TChild)childId, (TGrandChild)grandchildId, (TGreatGrandChild)greatgrandchildId);
            return true;
        }
        #endregion

        public TChild ChildId { get => _childId; }

        public TGrandChild GrandChildId { get => _grandChildId; }

        public TGreatGrandChild GreatGrandChildId { get => _greatGrandChildId; }

        public short ShardId { get => _shardId; }

        public TRecord RecordId { get => _recordId; }

        public bool IsEmpty { get => _recordId.CompareTo(default(TRecord)) == 0 && _shardId == 0 && _childId.CompareTo(default(TChild)) == 0 && _grandChildId.CompareTo(default(TGrandChild)) == 0 && _greatGrandChildId.CompareTo(default(TGreatGrandChild)) == 0; }

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
            => ShardsValues.ShardListForeign<TRecord, TChild, TGrandChild, TGreatGrandChild>(_shardId, records);

        /// <summary>
        /// Given a list of ShardKey values, returns a distinct list of shard Ids, except for the shard Id of the current shard.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public ShardsValues ForeignShards(List<ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>> records)
            => ShardsValues.ShardListForeign<TRecord, TChild, TGrandChild, TGreatGrandChild>(_shardId, (IList<ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>>)records);

        /// <summary>
        /// Given a list of Models with ShardChld keys, returns a distinct list of shard Ids, except for the shard Id of the current shard.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public ShardsValues ForeignShards<TModel>(IList<TModel> records) where TModel : IKeyedModel<TRecord, TChild, TGrandChild, TGreatGrandChild>
            => ShardsValues.ShardListForeign<TRecord, TChild, TGrandChild, TGreatGrandChild, TModel>(_shardId, records);

        /// <summary>
        /// Given a list of Models with ShardChld keys, returns a distinct list of shard Ids, except for the shard Id of the current shard.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public ShardsValues ForeignShards<TModel>(List<TModel> records) where TModel : IKeyedModel<TRecord, TChild, TGrandChild, TGreatGrandChild>
            => ShardsValues.ShardListForeign<TRecord, TChild, TGrandChild, TGreatGrandChild, TModel>(_shardId, (IList<TModel>)records);

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
            var matched = new bool[replacements.Count];
            for (var i = 0; i < result.Count; i++)
            {
                for (var j = 0; j < replacements.Count; j++)
                {
                    if (result[i].Key.Equals(replacements[j].Key))
                    {
                        result[i] = replacements[j];
                        matched[j] = true;
                        break;
                    }
                }
            }
            if (appendUnmatchedReplacements)
            {
                for (var i = 0; i < matched.Length; i++)
                {
                    if (!matched[i])
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
            return ((((other.ShardId.CompareTo(_shardId) == 0) && (other.RecordId.CompareTo(_recordId) == 0)) && (other.ChildId.CompareTo(this.ChildId) == 0)) && (other.GrandChildId.CompareTo(_grandChildId) == 0)) && (other.GreatGrandChildId.CompareTo(_greatGrandChildId) == 0);
        }
        public override bool Equals(object obj)
        {
            if (obj is null || GetType() != obj.GetType())
            {
                return false;
            }
            var other = (ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>)obj;
            return ((((other.ShardId.CompareTo(_shardId) == 0) && (other.RecordId.CompareTo(_recordId) == 0)) && (other.ChildId.CompareTo(this.ChildId) == 0)) && (other.GrandChildId.CompareTo(_grandChildId) == 0)) && (other.GreatGrandChildId.CompareTo(_greatGrandChildId) == 0);
        }

        public override int GetHashCode()
        {
            var aShd = ShardKeySerialization.GetValueBytes(this._shardId);
            var aRec = ShardKeySerialization.GetValueBytes(this._recordId);
            byte[] aDtm = null;

            var result1 = 0;
            var result2 = 0;
            var result3 = 0;
            var result4 = 0;
            if (!(aShd is null))
            {
                if (aShd.Length > 0)
                {
                    result1 |= aShd[0];
                }
                if (aShd.Length > 1)
                {
                    result2 |= aShd[1];
                }
                if (aShd.Length > 2)
                {
                    result3 |= aShd[2];
                }
                if (aShd.Length > 3)
                {
                    result4 |= aShd[3];
                }
            }
            if (!(aRec is null))
            {
                if (aRec.Length > 0)
                {
                    result1 |= aRec[0];
                }
                if (aRec.Length > 1)
                {
                    result2 |= aRec[1];
                }
                if (aRec.Length > 2)
                {
                    result3 |= aRec[2];
                }
                if (aRec.Length > 3)
                {
                    result4 |= aRec[3];
                }
            }
            if (!(aDtm is null))
            {
                result1 |= aRec[0];
                result2 |= aRec[1];
                result3 |= aRec[2];
                result4 |= aRec[3];
            }

            var aSChd = ShardKeySerialization.GetValueBytes(this._childId);
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


            var aSGchd = ShardKeySerialization.GetValueBytes(this._grandChildId);
            if (!(aSGchd is null))
            {
                if (aSGchd.Length > 0)
                {
                    aResult[0] = aSGchd[0];
                }
                if (aSChd.Length > 1)
                {
                    aResult[1] = aSGchd[1];
                }
                if (aSChd.Length > 2)
                {
                    aResult[2] = aSGchd[2];
                }
                if (aSChd.Length > 3)
                {
                    aResult[3] = aSGchd[3];
                }
            }


            var aSgGgChd = ShardKeySerialization.GetValueBytes(this._greatGrandChildId);
            if (!(aSgGgChd is null))
            {
                if (aSgGgChd.Length > 0)
                {
                    aResult[0] = aSgGgChd[0];
                }
                if (aSChd.Length > 1)
                {
                    aResult[1] = aSgGgChd[1];
                }
                if (aSChd.Length > 2)
                {
                    aResult[2] = aSgGgChd[2];
                }
                if (aSChd.Length > 3)
                {
                    aResult[3] = aSgGgChd[3];
                }
            }

            return BitConverter.ToInt32(new byte[] { (byte)result1, (byte)result2, (byte)result3, (byte)result4 }, 0);
        }

        public ReadOnlyMemory<byte> ToArray()
        {
            var shardData = ShardKeySerialization.GetValueBytes(_shardId);
            var recordData = ShardKeySerialization.GetValueBytes(_recordId);
            var childData = ShardKeySerialization.GetValueBytes(_childId);
            var grandChildData = ShardKeySerialization.GetValueBytes(_grandChildId);
            var greatGrandChildData = ShardKeySerialization.GetValueBytes(_greatGrandChildId);
            var metaLen = _typeMetadata.Length;

            var aResult = new byte[metaLen+ shardData.Length + recordData.Length + childData.Length + grandChildData.Length + greatGrandChildData.Length + 1];
            aResult[0] = (byte)(metaLen | 128); //metadata length on bits 1 & 2, No-utf8 flag on bit 8.
            var resultIndex = 1;
            _typeMetadata.ToArray().CopyTo(aResult, resultIndex);
            resultIndex += metaLen;
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
            return StringExtensions.SerializeToExternalString(ToArray().Span);
        }
        public override string ToString()
        {
            return $"\"shard\": {_shardId.ToString()}, \"ids\": [\"{_recordId.ToString()}\", \"{_childId.ToString()}\", \"{_grandChildId.ToString()}\", \"{_greatGrandChildId.ToString()}\"]";
        }

        /// <summary>
        /// Create a new instance of ShardKey from a URL-safe string; this method is the inverse of ToExternalString().
        /// </summary>
        /// <param name="value">A string generated by the ToExternalString() method.</param>
        /// <returns>An instance of this type.</returns>
        public static ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> FromExternalString(string value)
        {
            var aValues = StringExtensions.SerializeFromExternalString(value);
            return new ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>(aValues.Span);
        }

        /// <summary>
        /// Create a new instance of ShardKey from binary data; this method is the inverse of ToArray().
        /// </summary>
        /// <param name="value">A binary value generaeted by the ToArraay() method.</param>
        /// <returns>An instance of this type.</returns>
        //public static ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> FromSpan(ReadOnlySpan<byte> value)
        //{
        //    return new ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>(value);
        //}

        /// <summary>
        /// Create a new instance of ShardKey from UTF8 encoded binary data; this method is the inverse of ToUtf8().
        /// </summary>
        /// <param name="value">A binary value generaeted by the ToUtf8() method.</param>
        /// <returns>An instance of this type.</returns>
        //public static ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> FromUtf8(ReadOnlySpan<byte> encoded)
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

        public static bool operator ==(ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> sc1, ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> sc2)
        {
            return sc1.Equals(sc2);
        }
        public static bool operator !=(ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> sc1, ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> sc2)
        {
            return !sc1.Equals(sc2);
        }

        private static Lazy<ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>> _lazyEmpty = new Lazy<ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>>(() 
            => new ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild>(0, default(TRecord), default(TChild), default(TGrandChild), default(TGreatGrandChild)));

        public static ShardKey<TRecord, TChild, TGrandChild, TGreatGrandChild> Empty
        {
            get
            {
                return _lazyEmpty.Value;
            }
        }
    }
}
