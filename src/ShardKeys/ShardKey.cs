// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace ArgentSea
{
    /// <summary>
    /// Immutable class representing a sharded record with a “compound” key: the (virtual) shardId and the (database) recordId.
    /// </summary>
    [Serializable]
    public struct ShardKey<TRecord> : IEquatable<ShardKey<TRecord>>, IShardKey where TRecord : IComparable
    {
        private readonly short _shardId;
        private readonly TRecord _recordId;
        internal readonly ReadOnlyMemory<byte> _typeMetadata;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of sharded record identifier using constituent parts.
        /// </summary>
        /// <param name="shardId"></param>
        /// <param name="recordId"></param>
        /// <exception cref="ArgentSea.InvalidShardArgumentsException">Thrown when the generic types are invalid..</exception>
        public ShardKey(short shardId, TRecord recordId)
        {
            if (!ShardKeySerialization.TryEncodeTypeMetadata(typeof(TRecord), out var metadata))
            {
                throw new InvalidShardKeyMetadataException();
            }
            _typeMetadata = metadata;
            _shardId = shardId;
            _recordId = recordId;
        }
        internal ShardKey(ReadOnlyMemory<byte> typeMetadata, short shardId, TRecord recordId)
        {
            _typeMetadata = typeMetadata;
            _shardId = shardId;
            _recordId = recordId;
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
            if (isUtf8) // utf8 encoding chars do not use high bits, so it's safe to put.
            {
                data = StringExtensions.Decode(data).Span;
                metaLen = data[0] & 3;
            }
            var typRecord = typeof(TRecord);
            if (!ShardKeySerialization.TryEncodeTypeMetadata(typRecord, out var metadata))
            {
                throw new InvalidShardKeyMetadataException(typRecord);
            }
            _typeMetadata = metadata;
            var metadataSpan = metadata.Span;
            var saved = data.Slice(1, metaLen);
            if (metadataSpan.Length != 1 && saved.Length != 1)
            {
                throw new InvalidShardKeyMetadataException();
            }
            if (metadataSpan[0] != saved[0])
            {
                throw new InvalidShardKeyMetadataException(typRecord);
            }

            var pos = metaLen + 1;
            _shardId = BitConverter.ToInt16(data.Slice(pos));
            pos += 2;
            if (!ShardKeySerialization.TryConvertFromBytes(ref data, ref pos, typRecord, out dynamic result))
            {
                throw new InvalidDataException("Could not parse binary data to create a ShardKey.");
            }
            _recordId = result;
        }

        public static bool TryParse(ReadOnlySpan<byte> data, out ShardKey<TRecord> result)
        {
            result = ShardKey<TRecord>.Empty;
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
            if (!ShardKeySerialization.TryEncodeTypeMetadata(typRecord, out var metadata))
            {
                return false;
            }
            var metadataSpan = metadata.Span;
            var saved = data.Slice(1, metaLen);
            if (metadataSpan.Length != 1 && saved.Length != 1 && metadataSpan[0] != saved[0])
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
            result = new ShardKey<TRecord>(metadata, shardId, (TRecord)recordId);
            return true;
        }

        #endregion
        #region Public Properties and Method

        public readonly short ShardId { get => _shardId; }

        public readonly TRecord RecordId { get => _recordId; }

        public readonly bool IsEmpty { get => _recordId.CompareTo(_lazyEmptyRecordId.Value) == 0 && _shardId == 0; }

        public override string ToString() => $"{{ \"shard\": {_shardId.ToString()}, \"id\": \"{_recordId.ToString()}\"}}";

        #endregion

        public ReadOnlyMemory<byte> ToArray()
        {
            var shardData = ShardKeySerialization.GetValueBytes(this._shardId);
            var recordData = ShardKeySerialization.GetValueBytes(this._recordId);
            var metaLen = _typeMetadata.Length;
            var aResult = new byte[metaLen + shardData.Length + recordData.Length + 1];
            aResult[0] = (byte)(metaLen | 128); //metadata length on bits 1 & 2, No-utf8 flag on bit 8.
            var resultIndex = 1;
            _typeMetadata.ToArray().CopyTo(aResult, resultIndex);
            resultIndex += metaLen;
            shardData.CopyTo(aResult, resultIndex);
            resultIndex += shardData.Length;
            recordData.CopyTo(aResult, resultIndex);
            resultIndex += recordData.Length;
            return aResult;
        }

        /// <summary>
        /// Serializes ShardKey data into a URL-safe string with a checksum
        /// </summary>
        /// <returns>A string which includes the concurrency stamp if defined and includeConcurrencyStamp is true, otherwise returns a smaller string .</returns>
        public string ToExternalString()
        {
            return StringExtensions.SerializeToExternalString(ToArray().Span);
        }


        /// <summary>
        /// Create a new instance of ShardKey from a URL-safe string; this method is the inverse of ToExternalString().
        /// </summary>
        /// <param name="value">A string generated by the ToExternalString() method.</param>
        /// <returns>An instance of this type.</returns>
        public static ShardKey<TRecord> FromExternalString(string value)
        {

            var aValues = StringExtensions.SerializeFromExternalString(value);
            return new ShardKey<TRecord>(aValues.Span);
        }


        /// <summary>
        /// Create a new instance of ShardKey from binary data; this method is the inverse of ToArray().
        /// </summary>
        /// <param name="value">A binary value generaeted by the ToArraay() method.</param>
        /// <returns>An instance of this type.</returns>
        //public static ShardKey<TRecord> FromSpan(ReadOnlySpan<byte> value)
        //{
        //	return new ShardKey<TRecord>(value);
        //}

        /// <summary>
        /// Create a new instance of ShardKey from UTF8 encoded binary data; this method is the inverse of ToUtf8().
        /// </summary>
        /// <param name="value">A binary value generaeted by the ToUtf8() method.</param>
        /// <returns>An instance of this type.</returns>
        //public static ShardKey<TRecord> FromUtf8(ReadOnlySpan<byte> encoded)
        //{
        //	return FromSpan(StringExtensions.Decode(encoded).Span);
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

        /// <summary>
        /// Given a list of Models with ShardKey keys, return a distinct list of shard Ids.
        /// </summary>
        /// <param name="records">The list of models to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed and values not set.</returns>
        public static ShardsValues ToShardsValues<TModel>(IList<TModel> records) where TModel : IKeyedModel<TRecord>
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
        /// Merge two lists by iterating master list and using replacement entry where keys match.
        /// </summary>
        /// <typeparam name="TModel">The of the list values.</typeparam>
        /// <param name="master">The list to be returned, possibly with some entries replaced.</param>
        /// <param name="replacements">A list of more complete records.</param>
        /// <param name="appendUnmatchedReplacements">If true, any records in the replacement list that are were not found in the master list are appended to the collection result.</param>
        /// <returns>Merged list.</returns>
        public static List<TModel> Merge<TModel>(List<TModel> master, List<TModel> replacements, bool appendUnmatchedReplacements = false) where TModel : IKeyedModel<TRecord>
            => Merge<TModel>((IList<TModel>)master, (IList<TModel>)replacements, appendUnmatchedReplacements);

        /// <summary>
        /// Merge two lists by iterating master list and using replacement entry where keys match.
        /// </summary>
        /// <typeparam name="TModel">The of the list values.</typeparam>
        /// <param name="master">The list to be returned, possibly with some entries replaced.</param>
        /// <param name="replacements">A list of more complete records.</param>
        /// <param name="appendUnmatchedReplacements">If true, any records in the replacement list that are were not found in the master list are appended to the collection result.</param>
        /// <returns>Merged list.</returns>
        public static List<TModel> Merge<TModel>(IList<TModel> master, List<TModel> replacements, bool appendUnmatchedReplacements = false) where TModel : IKeyedModel<TRecord>
            => Merge<TModel>(master, (IList<TModel>)replacements, appendUnmatchedReplacements);

        /// <summary>
        /// Merge two lists by iterating master list and using replacement entry where keys match.
        /// </summary>
        /// <typeparam name="TModel">The of the list values.</typeparam>
        /// <param name="master">The list to be returned, possibly with some entries replaced.</param>
        /// <param name="replacements">A list of more complete records.</param>
        /// <param name="appendUnmatchedReplacements">If true, any records in the replacement list that are were not found in the master list are appended to the collection result.</param>
        /// <returns>Merged list.</returns>
        public static List<TModel> Merge<TModel>(List<TModel> master, IList<TModel> replacements, bool appendUnmatchedReplacements = false) where TModel : IKeyedModel<TRecord>
            => Merge<TModel>((IList<TModel>)master, replacements, appendUnmatchedReplacements);


        /// <summary>
        /// Merge two lists by iterating master list and using replacement entry where keys match.
        /// </summary>
        /// <typeparam name="TModel">The of the list values.</typeparam>
        /// <param name="master">The list to be returned, possibly with some entries replaced.</param>
        /// <param name="replacements">A list of more complete records.</param>
        /// <param name="appendUnmatchedReplacements">If true, any records in the replacement list that are were not found in the master list are appended to the collection result.</param>
        /// <returns>Merged list.</returns>
        public static List<TModel> Merge<TModel>(IList<TModel> master, IList<TModel> replacements, bool appendUnmatchedReplacements = false) where TModel : IKeyedModel<TRecord>
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
        /// Given a list of ShardKey values, returns a distinct list of shard Ids, except for the shard Id of the current shard.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public ShardsValues ForeignShards(IList<ShardKey<TRecord>> records)
            => ShardsValues.ShardListForeign<TRecord>(_shardId, records);

        /// <summary>
        /// Given a list of ShardKey values, returns a distinct list of shard Ids, except for the shard Id of the current shard.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public ShardsValues ForeignShards(List<ShardKey<TRecord>> records)
            => ShardsValues.ShardListForeign<TRecord>(_shardId, (IList<ShardKey<TRecord>>)records);

        /// <summary>
        /// Given a list of Models with ShardKey keys, returns a distinct list of shard Ids, except for the shard Id of the current shard.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public ShardsValues ForeignShards<TModel>(IList<TModel> records) where TModel : IKeyedModel<TRecord>
            => ShardsValues.ShardListForeign<TRecord, TModel>(_shardId, records);

        /// <summary>
        /// Given a list of Models with ShardKey keys, returns a distinct list of shard Ids, except for the shard Id of the current shard.
        /// Useful for querying foreign shards after the primary shard has returned results.
        /// </summary>
        /// <param name="records">The list of ShardKeys to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed. The values dictionary will be null.</returns>
        public ShardsValues ForeignShards<TModel>(List<TModel> records) where TModel : IKeyedModel<TRecord>
            => ShardsValues.ShardListForeign<TRecord, TModel>(_shardId, (IList<TModel>)records);

        public bool Equals(ShardKey<TRecord> other)
        {
            return ((other.ShardId.CompareTo(_shardId) == 0) && (other.RecordId.CompareTo(_recordId) == 0));
        }
        public override bool Equals(object obj)
        {
            if (obj is null || GetType() != obj.GetType())
            {
                return false;
            }

            var other = (ShardKey<TRecord>)obj;
            return (other.ShardId.CompareTo(_shardId) == 0) && (other.RecordId.CompareTo(_recordId) == 0);
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

            return BitConverter.ToInt32(new byte[] { (byte)result1, (byte)result2, (byte)result3, (byte)result4 }, 0);
        }


        public static bool operator ==(ShardKey<TRecord> sk1, ShardKey<TRecord> sk2)
        {
            return sk1.Equals(sk2);
        }
        public static bool operator !=(ShardKey<TRecord> sk1, ShardKey<TRecord> sk2)
        {
            return !sk1.Equals(sk2);
        }

        private static readonly Lazy<ShardKey<TRecord>> _lazyEmpty = new Lazy<ShardKey<TRecord>>(() =>
        {
            return new ShardKey<TRecord>(0, _lazyEmptyRecordId.Value);
        });

        public static ShardKey<TRecord> Empty
        {
            get
            {
                return _lazyEmpty.Value;
            }
        }

        private static readonly Lazy<TRecord> _lazyEmptyRecordId = new Lazy<TRecord>(() =>
            ShardKeySerialization.GetKeyDataType(typeof(TRecord)) switch
            {
                KeyDataType.String => (TRecord)(object)string.Empty,
                KeyDataType.Blob => (TRecord)(object)Array.Empty<byte>(),
                _ => default,
            }
        );
    }
}

