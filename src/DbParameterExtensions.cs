// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Data.Common;
using System.Text.Json;

namespace ArgentSea
{
    /// <summary>
    /// This class adds extension methods which simplify getting typed .NET values from (output) parameters.
    /// Because these methods reference the abstract DbParameterCollection, these methods are available in the derived classes:
    /// <list type="bullet">
    /// <item><see cref="ArgentSea.ParameterCollection" /></item>
    /// <item>SqlParameterCollection</item>
    /// <item>NpgsqlParameterCollection</item>
    /// </list>
    /// </summary>
    public static class DbParameterExtensions
    {
        #region Casting
        /// <summary>
        /// Gets a string value from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a string.</returns>
        public static string GetString(this DbParameter prm) => prm.Value as string;

        /// <summary>
        /// Gets a byte array from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a byte[].</returns>
        public static byte[] GetBytes(this DbParameter prm) => prm.Value as byte[];

        /// <summary>
        /// Gets an Int64 value from the output parameter.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as an Int64.</returns>
        /// <exception cref="ArgentSea.UnexpectedNullException">Thrown when a database null value is encountered.</exception>
        public static long GetLong(this DbParameter prm) => DBNull.Value.Equals(prm.Value) ? throw new UnexpectedNullException(typeof(long), prm.ParameterName) : (long)prm.Value;

        /// <summary>
		/// Gets a Nullable&lt;Int64&gt; value from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Nullable&lt;Int64&gt;.</returns>
        public static long? GetNullableLong(this DbParameter prm) => prm.Value as long?;

        /// <summary>
        /// Gets an Int32 value from the output parameter.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as an Int32.</returns>
        /// <exception cref="ArgentSea.UnexpectedNullException">Thrown when a database null value is encountered.</exception>
        public static int GetInteger(this DbParameter prm) => DBNull.Value.Equals(prm.Value) ? throw new UnexpectedNullException(typeof(int), prm.ParameterName) : (int)prm.Value;

        /// <summary>
		/// Gets a Nullable&lt;Int32&gt; value from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Nullable&lt;Int32&gt;.</returns>
        public static int? GetNullableInteger(this DbParameter prm) => prm.Value as int?;

        /// <summary>
        /// Gets a short (Int16) value from the output parameter.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as an Int16.</returns>
        /// <exception cref="ArgentSea.UnexpectedNullException">Thrown when a database null value is encountered.</exception>
        public static short GetShort(this DbParameter prm) => DBNull.Value.Equals(prm.Value) ? throw new UnexpectedNullException(typeof(short), prm.ParameterName) : (short)prm.Value;

        /// <summary>
		/// Gets a Nullable&lt;Int16&gt; value from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Nullable&lt;Int16&gt;.</returns>
        public static short? GetNullableShort(this DbParameter prm) => prm.Value as short?;

        /// <summary>
        /// Gets a byte value from the output parameter.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a byte array.</returns>
        /// <exception cref="ArgentSea.UnexpectedNullException">Thrown when a database null value is encountered.</exception>
        public static byte GetByte(this DbParameter prm) => DBNull.Value.Equals(prm.Value) ? throw new UnexpectedNullException(typeof(byte), prm.ParameterName) : (byte)prm.Value;

        /// <summary>
		/// Gets a Nullable&lt;Byte&gt; value from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Nullable&lt;Byte&gt;.</returns>
        public static byte? GetNullableByte(this DbParameter prm) => prm.Value as byte?;

        /// <summary>
        /// Gets a Boolean value from the output parameter.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Boolean.</returns>
        /// <exception cref="ArgentSea.UnexpectedNullException">Thrown when a database null value is encountered.</exception>
        public static bool GetBoolean(this DbParameter prm) => DBNull.Value.Equals(prm.Value) ? throw new UnexpectedNullException(typeof(bool), prm.ParameterName) : (bool)prm.Value;

        /// <summary>
		/// Gets a Nullable&lt;Boolean&gt; value from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Nullable&lt;Boolean&gt;.</returns>
        public static bool? GetNullableBoolean(this DbParameter prm) => prm.Value as bool?;

        /// <summary>
        /// Gets a Decimal value from the output parameter.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Decimal.</returns>
        /// <exception cref="ArgentSea.UnexpectedNullException">Thrown when a database null value is encountered.</exception>
        public static decimal GetDecimal(this DbParameter prm) => DBNull.Value.Equals(prm.Value) ? throw new UnexpectedNullException(typeof(decimal), prm.ParameterName) : (decimal)prm.Value;

        /// <summary>
		/// Gets a Nullable&lt;Decimal&gt; value from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Nullable&lt;Decimal&gt;.</returns>
        public static decimal? GetNullableDecimal(this DbParameter prm) => prm.Value as decimal?;

        /// <summary>
        /// Gets a Double (64-bit floating point) value from the output parameter, or NaN (Not a Number) if the value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Double.</returns>
        public static double GetDouble(this DbParameter prm)
		{
			if (System.DBNull.Value.Equals(prm.Value))
			{
				return double.NaN;
			}
			else
			{
				return (double)prm.Value;
			}
		}

        /// <summary>
		/// Gets a Nullable&lt;Double&gt; value from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Nullable&lt;Double&gt;.</returns>
		public static double? GetNullableDouble(this DbParameter prm)
			=> prm.Value as double?;

        /// <summary>
        /// Gets a Float (32-bit floating point) value from the output parameter, or NaN (Not a Number) if the value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Float.</returns>
        public static float GetFloat(this DbParameter prm)
		{
			if (System.DBNull.Value.Equals(prm.Value))
			{
				return float.NaN;
			}
			else
			{
				return (float)prm.Value;
			}
		}

        /// <summary>
		/// Gets a Nullable&lt;Float&gt; value from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Nullable&lt;Float&gt;.</returns>
		public static float? GetNullableFloat(this DbParameter prm)
			=> prm.Value as float?;

        /// <summary>
        /// Gets a Guid value from the output parameter, or Guid.Emtpy if the value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Guid.</returns>
        public static Guid GetGuid(this DbParameter prm)
		{
			if (System.DBNull.Value.Equals(prm.Value))
			{
				return Guid.Empty;
			}
			else
			{
				return (Guid)prm.Value;
			}
		}

        /// <summary>
		/// Gets a Nullable&lt;Guid&gt; value from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Nullable&lt;Guid&gt;.</returns>
		public static Guid? GetNullableGuid(this DbParameter prm)
		{
			if (System.DBNull.Value.Equals(prm.Value))
			{
				return null;
			}
			else
			{
				return (Guid?)prm.Value;
			}
		}

        /// <summary>
        /// Gets a DateTime value from the output parameter.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a DateTime.</returns>
        /// <exception cref="ArgentSea.UnexpectedNullException">Thrown when a database null value is encountered.</exception>
		public static DateTime GetDateTime(this DbParameter prm) => DBNull.Value.Equals(prm.Value) ? throw new UnexpectedNullException(typeof(DateTime), prm.ParameterName) : (DateTime)prm.Value;

        /// <summary>
		/// Gets a Nullable&lt;DateTime&gt;) value from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Nullable&lt;DateTime&gt;.</returns>
        public static DateTime? GetNullableDateTime(this DbParameter prm) => prm.Value as DateTime?;

        /// <summary>
        /// Gets a DateTimeOffset value from the output parameter.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a DateTimeOffset.</returns>
        /// <exception cref="ArgentSea.UnexpectedNullException">Thrown when a database null value is encountered.</exception>
        public static DateTimeOffset GetDateTimeOffset(this DbParameter prm) => DBNull.Value.Equals(prm.Value) ? throw new UnexpectedNullException(typeof(DateTimeOffset), prm.ParameterName) : (DateTimeOffset)prm.Value;

        /// <summary>
		/// Gets a Nullable&lt;DateTimeOffset&gt; value from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Nullable&lt;DateTimeOffset&gt;.</returns>
        public static DateTimeOffset? GetNullableDateTimeOffset(this DbParameter prm) => prm.Value as DateTimeOffset?;

        /// <summary>
        /// Gets a TimeSpan value from the output parameter.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a TimeSpan.</returns>
        /// <exception cref="ArgentSea.UnexpectedNullException">Thrown when a database null value is encountered.</exception>
        public static TimeSpan GetTimeSpan(this DbParameter prm) => DBNull.Value.Equals(prm.Value) ? throw new UnexpectedNullException(typeof(TimeSpan), prm.ParameterName) : (TimeSpan)prm.Value;

        /// <summary>
        /// Gets a Nullable&lt;TimeSpan&gt; value from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Nullable&lt;TimeSpan&gt;.</returns>
        public static TimeSpan? GetNullableTimeSpan(this DbParameter prm) => prm.Value as TimeSpan?;

        /// <summary>
        /// Gets a DateOnly value from the output parameter.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a TimeSpan.</returns>
        /// <exception cref="ArgentSea.UnexpectedNullException">Thrown when a database null value is encountered.</exception>
        public static DateOnly GetDateOnly(this DbParameter prm) => DBNull.Value.Equals(prm.Value) ? throw new UnexpectedNullException(typeof(DateOnly), prm.ParameterName) : (DateOnly)prm.Value;

        /// <summary>
        /// Gets a Nullable&lt;DateOnly&gt; value from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Nullable&lt;TimeSpan&gt;.</returns>
        public static DateOnly? GetNullableDateOnly(this DbParameter prm) => prm.Value as DateOnly?;

        /// <summary>
        /// Gets a TimeSpan value from the output parameter.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a TimeSpan.</returns>
        /// <exception cref="ArgentSea.UnexpectedNullException">Thrown when a database null value is encountered.</exception>
        public static TimeOnly GetTimeOnly(this DbParameter prm) => DBNull.Value.Equals(prm.Value) ? throw new UnexpectedNullException(typeof(TimeOnly), prm.ParameterName) : (TimeOnly)prm.Value;

        /// <summary>
        /// Gets a Nullable&lt;TimeSpan&gt; value from the output parameter, or null if the parameter value is DbNull.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a Nullable&lt;TimeSpan&gt;.</returns>
        public static TimeOnly? GetNullableTimeOnly(this DbParameter prm) => prm.Value as TimeOnly?;
        #endregion

        /// <summary>
        /// Gets a JsonDocument value from the output parameter.
        /// </summary>
        /// <param name="prm">The output parameter, populated with a value (after Execute).</param>
        /// <returns>The parameter value as a JsonDocument.</returns>
        /// <exception cref="ArgentSea.UnexpectedNullException">Thrown when a database null value is encountered.</exception>
        public static JsonDocument GetJson(this DbParameter prm) =>  prm.Value as JsonDocument;

        internal static int GetParameterOrdinal(this DbParameterCollection parameters, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return -1;
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].ParameterName == name)
                {
                    return i;
                }
            }
            throw new Exception($"Could not find data parameter {name} in the parameters collection.");
        }
        internal static void SetShardId<TShard>(this DbParameterCollection parameters, int shardParameterOrdinal, TShard shardId) where TShard : IComparable
        {
            if (shardParameterOrdinal >= 0 && shardParameterOrdinal < parameters.Count)
            {
                parameters[shardParameterOrdinal].Value = shardId;
            }
        }
    }
}
