using System;
using System.Data.Common;

namespace ArgentSea
{
    public static class DbParameterCollectionExtensions
    {
		#region Casting
		/// <summary>
		/// Returns a string, or null if the parameter value is DbNull.
		/// </summary>
		/// <returns>Parameter value as a string.</returns>
		public static string GetString(this DbParameter prm) => prm.Value as string;
		/// <summary>
		/// Returns a byte array, or null if the parameter value is DbNull.
		/// </summary>
		/// <returns>Parameter value as a byte[].</returns>
		public static byte[] GetBytes(this DbParameter prm) => prm.Value as byte[];
		/// <summary>
		/// Returns a Char value from the parameter, or NUL (char 0) if the value is DbNull.
		/// </summary>
		/// <returns>Parameter value as Char.</returns>
		//public static char GetChar(this DbParameter prm)
		//{
		//    if (System.DBNull.Value.Equals(prm.Value))
		//    {
		//        return (char)0;
		//    }
		//    else
		//    {
		//        return (char)prm.Value;
		//    }
		//}
		public static long GetLong(this DbParameter prm) => (long)prm.Value;
		public static long? GetNullableLong(this DbParameter prm) => prm.Value as long?;
		public static int GetInteger(this DbParameter prm) => (int)prm.Value;
		public static int? GetNullableInteger(this DbParameter prm) => prm.Value as int?;
		public static short GetShort(this DbParameter prm) => (short)prm.Value;
		public static short? GetNullableShort(this DbParameter prm) => prm.Value as short?;
		public static byte GetByte(this DbParameter prm) => (byte)prm.Value;
		public static byte? GetNullableByte(this DbParameter prm) => prm.Value as byte?;
		public static bool GetBoolean(this DbParameter prm) => (bool)prm.Value;
		public static bool? GetNullableBoolean(this DbParameter prm) => prm.Value as bool?;
		public static decimal GetDecimal(this DbParameter prm) => (decimal)prm.Value;
		public static decimal? GetNullableDecimal(this DbParameter prm) => prm.Value as decimal?;
		/// <summary>
		/// Returns a double (64-bit floating point) value from the parameter, or NaN (Not a Number) if the value is DbNull.
		/// </summary>
		/// <returns>Parameter value as double.</returns>
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
		public static double? GetNullableDouble(this DbParameter prm)
			=> prm.Value as double?;
		/// <summary>
		/// Returns a double (32-bit floating point) value from the parameter, or NaN (Not a Number) if the value is DbNull.
		/// </summary>
		/// <returns>Parameter value as float.</returns>
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
		public static float? GetNullableFloat(this DbParameter prm)
			=> prm.Value as float?;

		/// <summary>
		/// Returns a Guid value from the parameter, or Guid.Emtpy if the value is DbNull.
		/// </summary>
		/// <returns>Parameter value as Guid.</returns>
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
		public static DateTime GetDateTime(this DbParameter prm) => (DateTime)prm.Value;
		public static DateTime? GetNullableDateTime(this DbParameter prm) => prm.Value as DateTime?;
		public static DateTimeOffset GetDateTimeOffset(this DbParameter prm) => (DateTimeOffset)prm.Value;
		public static DateTimeOffset? GetNullableDateTimeOffset(this DbParameter prm) => prm.Value as DateTimeOffset?;
		public static TimeSpan GetTimeSpan(this DbParameter prm) => (TimeSpan)prm.Value;
		public static TimeSpan? GetNullableTimeSpan(this DbParameter prm) => prm.Value as TimeSpan?;
		#endregion
	}
}
