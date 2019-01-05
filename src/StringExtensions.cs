// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    /// <summary>
    /// This static class contains extension methods for cleaning input strings of invalid characters.
    /// It does NOT sanitize input strings for protection against injection attacks (your parmemeterized queries should protect against that).
    /// </summary>
    public static class StringExtensions
    {
		[Flags]
		public enum InputCleaningOptions {
			/// <summary>
			/// Remove starting and trailing whitespace, “control” characters including Lf and Cr, and extended characters like Emoji icons.
			/// </summary>
			CleanAll = 0,
			/// <summary>
			/// Do not remove Unicode surrogate values, like Emoji.
			/// </summary>
			AllowEmojis = 1,
			/// <summary>
			/// All “control” characters are removed by default; setting this flag preserve keep CrLf and/or Cr. It also ensures Windows line endings (Cr+Lf) when encountering Lf only (Unix line endings).
			/// </summary>
			AllowMultiline = 2,
		}

		/// <summary>
		/// Removes leading/trailing whitespace, control charactors (cr, lf, tab, etc.), and emojis from input string.
		/// </summary>
		/// <param name="value">Input string, resumably provided by user.</param>
		/// <returns>Input string with undesired characters removed.</returns>
		public static string CleanInput(this string value) => CleanInput(value, 0);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static string CleanInput(this string value, InputCleaningOptions flags)
		{
			if (value is null)
			{
				return null;
			}
			var sb = new StringBuilder(value.Length * 2);
			var foundFirstNonWhitespace = false;
			int lastNonSeperator = value.Length;
			bool allowEmojis = (flags & InputCleaningOptions.AllowEmojis) == InputCleaningOptions.AllowEmojis;
			bool allowMultiline = (flags & InputCleaningOptions.AllowMultiline) == InputCleaningOptions.AllowMultiline;

			for (int i = 0; i < value.Length; i++)
			{
				var ch = value[i];

				if (char.IsHighSurrogate(ch) && !allowEmojis)
				{
					i++;
				}
				else if (ch == '\n' && allowMultiline)
				{
					sb.Append("\r\n");
				}
				else if (char.IsControl(ch))
				{
					//

				}
				else if (char.IsSeparator(ch))
				{
					if (foundFirstNonWhitespace)
					{
						sb.Append(ch);
					}
				}
				else
				{
					foundFirstNonWhitespace = true;
					sb.Append(ch);
					lastNonSeperator = sb.Length;
				}
			}
			if (!foundFirstNonWhitespace)
			{
				return string.Empty;
			}
			return sb.ToString(0, lastNonSeperator);
		}
        internal static byte[] SerializeFromExternalString(string value)
        {
            if (value is null)
            {
                throw new ArgumentException(nameof(value));
            }
            string subValue = value.Substring(2);

            int origCheckSumHigh = CharToCheckSum(value[0]);
            int origCheckSumLow = CharToCheckSum(value[1]);
            int origCheckSum = ((origCheckSumHigh & 0x3f) << 6) | origCheckSumLow;
            if ((origCheckSum & 0x400) == 0x400)
            {
                subValue += "=";
                if ((origCheckSum & 0x800) == 0x800)
                {
                    subValue += "=";
                }
            }
            origCheckSum &= 0x3FF;
            subValue = subValue.Replace('_', '+').Replace('~', '/');

            var aValues = Convert.FromBase64String(subValue);

            for (int i = 0; i < aValues.Length; i++)
            {
                if (i % 6 == 0)
                {
                    aValues[i] ^= 119;
                }
                else if (i % 6 == 1)
                {
                    aValues[i] ^= 78;
                }
                else if (i % 6 == 2)
                {
                    aValues[i] ^= 180;
                }
                else if (i % 6 == 3)
                {
                    aValues[i] ^= 92;
                }
                else if (i % 6 == 4)
                {
                    aValues[i] ^= 83;
                }
                else if (i % 6 == 5)
                {
                    aValues[i] ^= 77;
                }
            }

            int checkSum = 0;
            foreach (var chr in aValues)
            {
                checkSum += chr;
                checkSum &= 0x3ff;
            }
            if (origCheckSum != checkSum)
            {
                throw new Exception("External key string is not valid.");
            }

            if ((aValues[0] & 12) != (1 << 2))
            {
                throw new Exception("The serialization version is invalid. Cannot deserialize this external string.");
            }
            return aValues;

        }
        internal static string SerializeToExternalString(byte[] value)
        {
            int checkSum = 0;
            foreach (var chr in value)
            {
                checkSum += chr;
                checkSum &= 0x3ff;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (i % 6 == 0)
                {
                    value[i] ^= 119;
                }
                else if (i % 6 == 1)
                {
                    value[i] ^= 78;
                }
                else if (i % 6 == 2)
                {
                    value[i] ^= 180;
                }
                else if (i % 6 == 3)
                {
                    value[i] ^= 92;
                }
                else if (i % 6 == 4)
                {
                    value[i] ^= 83;
                }
                else if (i % 6 == 5)
                {
                    value[i] ^= 77;
                }
            }
            var base64Value = Convert.ToBase64String(value).Replace('+', '_').Replace('/', '~');
            if (base64Value[base64Value.Length - 1] == '=')
            {
                checkSum |= 0x400;
                if (base64Value[base64Value.Length - 2] == '=')
                {
                    checkSum |= 0x800;
                }
            }
            var checkSumCharHigh = CheckSumToChar(checkSum >> 6).ToString();
            var checkSumCharLow = CheckSumToChar(checkSum & 0x3f).ToString();

            return checkSumCharHigh + checkSumCharLow + base64Value.TrimEnd('=');
        }

        private static char CheckSumToChar(int checkSum)
        {
            char result;
            if (checkSum < 26)
            {
                result = (char)(0x41 + checkSum);
            }
            else if (checkSum < 52)
            {
                result = (char)(0x61 + (checkSum - 26));
            }
            else if (checkSum < 62)
            {
                result = (char)(0x30 + (checkSum - 52));
            }
            else if (checkSum == 62)
            {
                result = '+';
            }
            else
            {
                result = '/';
            }
            return result;
        }
        private static int CharToCheckSum(char charCheckSum)
        {
            int origCheckSum;
            if ('A' <= charCheckSum && charCheckSum <= 'Z')
            {
                origCheckSum = (int)charCheckSum - 0x41;
            }
            else if ('a' <= charCheckSum && charCheckSum <= 'z')
            {
                origCheckSum = ((int)charCheckSum - 0x61) + 26;
            }
            else if ('0' <= charCheckSum && charCheckSum <= '9')
            {
                origCheckSum = ((int)charCheckSum - 0x30) + 52;
            }
            else if ('+' == charCheckSum)
            {
                origCheckSum = 62;
            }
            else if ('/' == charCheckSum)
            {
                origCheckSum = 63;
            }
            else
            {
                throw new Exception("External key string has been corrupted.");
            }
            return origCheckSum;
        }
    }
}
