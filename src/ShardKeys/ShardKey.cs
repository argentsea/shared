// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ArgentSea
{
    /// <summary>
    /// Immutable class representing a sharded record with a “compound” key: the (virtual) shardId and the (database) recordId.
    /// </summary>
    [Serializable]
    public struct ShardKey<TRecord> : IEquatable<ShardKey<TRecord>>, IShardKey, ISerializable where TRecord : IComparable
	{
		private readonly short _shardId;
		private readonly TRecord _recordId;
		private readonly char _origin;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of sharded record identifier using constituent parts.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="shardId"></param>
        /// <param name="recordId"></param>
        /// <exception cref="ArgentSea.InvalidShardArgumentsException">Thrown when the DataOrigin is '0' (i.e. is empty), but the the shardId or recordId does not equal zero.</exception>
        public ShardKey(char origin, short shardId, TRecord recordId)
		{
			if (origin == '0' && (shardId != 0 || recordId.CompareTo(default(TRecord)) != 0))
			{
				throw new InvalidShardArgumentsException();
			}
			_origin = origin;
			_shardId = shardId;
			_recordId = recordId;
		}
        /// <summary>
        /// ISerializer constructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public ShardKey(SerializationInfo info, StreamingContext context)
        {
            if (info.MemberCount == 3)
            {
                _origin = info.GetChar("origin");
                _shardId = (short)info.GetValue("shardId", typeof(short));
                _recordId = (TRecord)info.GetValue("recordId", typeof(TRecord));
            }
            else
            {
                var tmp = FromExternalString(info.GetString("shardKey"));
                _origin = tmp.Origin;
                _shardId = tmp.ShardId;
                _recordId = tmp.RecordId;
            }
        }

		/// <summary>
		/// Initiaizes a new instance from a readonly data array.
		/// </summary>
		/// <param name="data">The readonly span containing the shardKey data. This can be generated using the ToArray() method.</param>
		public ShardKey(ReadOnlySpan<byte> data)
		{
            int orgnLen = data[0] & 3;
            _origin = System.Text.Encoding.UTF8.GetString(data.Slice(1, orgnLen))[0];
            var pos = orgnLen + 1;
            _shardId = BitConverter.ToInt16(data.Slice(pos));
			pos += 2;
			_recordId = ConvertFromBytes(data, ref pos, typeof(TRecord));
        }

        #endregion
        #region Public Properties and Method

        public short ShardId
		{
			get { return _shardId; }
		}
		public TRecord RecordId
		{
			get { return _recordId; }
		}
		public char Origin
		{
			get
			{
				return this._origin;
			}
		}
		public bool IsEmpty
		{
			get
			{
				return this._origin == '0' && this.RecordId.CompareTo(default(TRecord)) == 0 && this.ShardId == 0;
			}
		}
		public override string ToString()
		{
			return $"{{ \"origin\": \"{_origin}\", \"shard\": {_shardId.ToString()}, \"id\": \"{_recordId.ToString()}\"}}";
		}

		#endregion

		internal static byte[] GetValueBytes(IComparable value)
		{
			switch (value)
			{
				case int i:
					return BitConverter.GetBytes(i);
				case byte b:
					return new byte[1] { b };
				case short s:
					return BitConverter.GetBytes(s);
				case long l:
					return BitConverter.GetBytes(l);
				case char c:
					return BitConverter.GetBytes(c);
				case decimal d:
					var aid = Decimal.GetBits(d);
					var i0 = BitConverter.GetBytes(aid[0]);
					var i1 = BitConverter.GetBytes(aid[1]);
					var i2 = BitConverter.GetBytes(aid[2]);
					var i3 = BitConverter.GetBytes(aid[3]);
					return new byte[16] { i0[0], i0[1], i0[2], i0[3], i1[0], i1[1], i1[2], i1[3], i2[0], i2[1], i2[2], i2[3], i3[0], i3[1], i3[2], i3[3] };
				case double o:
					return BitConverter.GetBytes(o);
				case float f:
					return BitConverter.GetBytes(f);
				case uint ui:
					return BitConverter.GetBytes(ui);
				case ulong ul:
					return BitConverter.GetBytes(ul);
				case ushort us:
					return BitConverter.GetBytes(us);
				case sbyte sb:
					return new byte[1] { (byte)((int)sb + 128) };
				//case bool bln:
				//    return BitConverter.GetBytes(bln);
				case DateTime dt:
					return BitConverter.GetBytes(dt.Ticks);
				case string str:
					if (str is null)
					{
						return new byte[1] { 0 };
					}
					else
					{
						var aStr = System.Text.Encoding.UTF8.GetBytes(str);
						if (aStr.Length > 128)
						{
							throw new Exception("Shard values cannot serialize strings longer than 128 bytes.");
						}
						var aResult = new byte[aStr.Length + 1];
						aResult[0] = Convert.ToByte((aStr.Length << 1) + 1);
						aStr.CopyTo(aResult, 1);
						return aResult;
					}
				case Enum e:
					var type = Enum.GetUnderlyingType(value.GetType());
					var newValue = Convert.ChangeType(value, type);
					return ShardKey<TRecord>.GetValueBytes(newValue as IComparable);
				case DateTimeOffset dto:
					var adt = BitConverter.GetBytes(dto.Ticks);
					var tsp = BitConverter.GetBytes(dto.Offset.Ticks);
					return new byte[] { adt[0], adt[1], adt[2], adt[3], adt[4], adt[5], adt[6], adt[7], tsp[0], tsp[1], tsp[2], tsp[3], tsp[4], tsp[5], tsp[6], tsp[7] };
				case TimeSpan ts:
					return BitConverter.GetBytes(ts.Ticks);
				case Guid g:
					return g.ToByteArray();
				//case null:
				//    return new byte[0];
				default:
					if (value is Nullable)
					{
						if (value == null)
						{
							return BitConverter.GetBytes(true);
						}
						var shdType = Nullable.GetUnderlyingType(value.GetType());
						var nonNullValue = Convert.ChangeType(value, shdType);
						var aVal = GetValueBytes(nonNullValue as IComparable);
						var valResult = new byte[1 + aVal.Length];
						valResult[0] = BitConverter.GetBytes(false)[0];
						aVal.CopyTo(valResult, 1);
						return valResult;
					}
					else
					{
						throw new Exception("Cannot serialize this type.");
					}
			}
		}

		public byte[] ToArray()
		{
			var aOrigin = System.Text.Encoding.UTF8.GetBytes(new[] { this.Origin });
			var shardData = GetValueBytes(this._shardId);
			var recordData = GetValueBytes(this._recordId);
			var aResult = new byte[aOrigin.Length + shardData.Length + recordData.Length + 1]; //TODO: var aResult = stackalloc byte[aOrigin.Length + shardData.Length + recordData.Length + 1];
            aResult[0] = (byte)(aOrigin.Length | (1 << 2)); //origin length on bits 1 & 2, version (1) on bit 3.
			var resultIndex = 1;
			aOrigin.CopyTo(aResult, resultIndex);
			resultIndex += aOrigin.Length;
			shardData.CopyTo(aResult, resultIndex);
			resultIndex += shardData.Length;
			recordData.CopyTo(aResult, resultIndex);
			resultIndex += recordData.Length;
			return aResult;
		}

        
		internal static dynamic ConvertFromBytes(ReadOnlySpan<byte> data, ref int position, Type valueType)
		{
			if (valueType == typeof(int))
			{
				position += 4;
				return BitConverter.ToInt32(data.Slice(position - 4));
			}
			if (valueType == typeof(long))
			{
				position += 8;
				return BitConverter.ToInt64(data.Slice(position - 8));
			}
			if (valueType == typeof(byte))
			{
				position += 1;
				return data[position - 1];
			}
			if (valueType == typeof(short))
			{
				position += 2;
				return BitConverter.ToInt16(data.Slice(position - 2));
			}
			if (valueType == typeof(char))
			{
				position += 2;
				return BitConverter.ToChar(data.Slice(position - 2));
			}
			if (valueType == typeof(decimal))
			{
				var i0 = BitConverter.ToInt32(data.Slice(position));
				var i1 = BitConverter.ToInt32(data.Slice(position + 4));
				var i2 = BitConverter.ToInt32(data.Slice(position + 8));
				var i3 = BitConverter.ToInt32(data.Slice(position + 12));
				position += 16;
				return (dynamic)new Decimal(new int[] { i0, i1, i2, i3 });
			}
			if (valueType == typeof(double))
			{
				position += 8;
				return BitConverter.ToDouble(data.Slice(position - 8));
			}
			if (valueType == typeof(float))
			{
				position += 4;
				return BitConverter.ToSingle(data.Slice(position - 4));
			}
			if (valueType == typeof(uint))
			{
				position += 4;
				return BitConverter.ToUInt32(data.Slice(position - 4));
			}
			if (valueType == typeof(ulong))
			{
				position += 8;
				return BitConverter.ToUInt64(data.Slice(position - 8));
			}
			if (valueType == typeof(ushort))
			{
				position += 2;
				return BitConverter.ToUInt16(data.Slice(position - 2));
			}
			if (valueType == typeof(sbyte))
			{
				position += 1;
				return (sbyte)((int)data[position - 1] - 128);
			}
			if (valueType == typeof(bool))
			{
				position += 1;
				return BitConverter.ToBoolean(data.Slice(position - 1));
			}
			if (valueType == typeof(DateTime))
			{
				position += 8;
				return new DateTime(BitConverter.ToInt64(data.Slice(position - 8)));
			}
			if (valueType == typeof(string))
			{
				if (data[position] == 0)
				{
					position += 1;
					return (string)null;
				}
				var len = (int)data[position] >> 1;
				position += len + 1;
				return System.Text.Encoding.UTF8.GetString(data.Slice(position - len, len));
			}
			if (valueType == typeof(Enum))
			{
				var baseType = Enum.GetUnderlyingType(valueType);
				return ShardKey<TRecord>.ConvertFromBytes(data, ref position, baseType);
			}
			if (valueType == typeof(Nullable))
			{
				var isNull = BitConverter.ToBoolean(data.Slice(position));
				position += 1;
				if (isNull)
				{
					return null;
				}
				var baseType = Nullable.GetUnderlyingType(valueType);
				return ConvertFromBytes(data, ref position, baseType);
			}
            if (valueType == typeof(Guid))
            {
                //var result = new Guid(   new byte[] { data[position], data[position + 1], data[position + 2], data[position + 3], data[position + 4], data[position + 5], data[position + 6], data[position + 7], data[position + 8], data[position + 9], data[position + 10], data[position + 11], data[position + 12], data[position + 13], data[position + 14], data[position + 15] });
				var result = new Guid(data.Slice(position));
                position += 16;
                return result;
            }
            else
			{
				throw new Exception($"Could not recognized the type {valueType.ToString()}.");
			}
		}

        /// <summary>
        /// Serializes ShardKey data into a URL-safe string with a checksum
        /// </summary>
        /// <returns>A string which includes the concurrency stamp if defined and includeConcurrencyStamp is true, otherwise returns a smaller string .</returns>
        public string ToExternalString()
		{
            return StringExtensions.SerializeToExternalString(ToArray());
		}
		public static ShardKey<TRecord> FromExternalString(string value)
		{

            var aValues = StringExtensions.SerializeFromExternalString(value);
			return new ShardKey<TRecord>(new ReadOnlySpan<byte>(aValues));
			//short shardId = 0;
			//TRecord recordId = default(TRecord);

			//int orgnLen = aValues[0] & 3;
			//var orgn = System.Text.Encoding.UTF8.GetString(aValues, 1, orgnLen)[0];
			//var pos = orgnLen + 1;

			//shardId = ConvertFromBytes(aValues, ref pos, typeof(short));
			//recordId = ConvertFromBytes(aValues, ref pos, typeof(TRecord));
			//return new ShardKey<TRecord>(orgn, shardId, recordId);
		}

        /// <summary>
        /// Given a list of Models with ShardKey keys, return a distinct list of shard Ids.
        /// </summary>
        /// <param name="records">The list of models to evaluate.</param>
        /// <returns>A ShardsValues collection, with the shards listed and values not set.</returns>
        public static ShardsValues ToShardsValues<TModel>(IList<TModel> records) where TModel: IKeyedModel<TRecord>
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
            => ShardsValues.ShardListForeign<TRecord>(_shardId, (IList<ShardKey<TRecord>>) records);

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
            => ShardsValues.ShardListForeign<TRecord, TModel>(_shardId, (IList<TModel>) records);

        public bool Equals(ShardKey<TRecord> other)
		{
			return (other.Origin == _origin) && (other.ShardId.CompareTo(_shardId) == 0) && (other.RecordId.CompareTo(_recordId) == 0);
		}
		public override bool Equals(object obj)
		{
			if (obj is null || GetType() != obj.GetType())
			{
				return false;
			}

			var other = (ShardKey<TRecord>)obj;
			return (other.Origin == _origin) && (other.ShardId.CompareTo(_shardId) == 0) && (other.RecordId.CompareTo(_recordId) == 0);
		}
		public override int GetHashCode()
		{

			var aOgn = System.Text.Encoding.UTF8.GetBytes(new[] { this.Origin });
			var aShd = GetValueBytes(this._shardId);
			var aRec = GetValueBytes(this._recordId);
			byte[] aDtm = null;

			var result1 = 0;
			var result2 = 0;
			var result3 = 0;
			var result4 = 0;
			if (!(aOgn is null))
			{
				if (aOgn.Length > 0)
				{
					result1 |= aOgn[0];
				}
				if (aOgn.Length > 1)
				{
					result2 |= aOgn[1];
				}
				if (aOgn.Length > 2)
				{
					result3 |= aOgn[2];
				}
			}
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
		public static ShardKey<TRecord> Empty
		{
			get
			{
				return new ShardKey<TRecord>('0', 0, default(TRecord));
			}
		}
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("shardKey", ToExternalString());
            //info.AddValue("Ids", $"{_shardId.ToString()}, {_recordId.ToString()}");
        }
        public void ThrowIfInvalidOrigin(char expectedOrigin)
        {
            if (_origin != expectedOrigin)
            {
                throw new InvalidDataOriginException(expectedOrigin, _origin);
            }
        }
    }
}

