// © John Hicks. All rights reserved. Licensed under the MIT license.
// See the LICENSE file in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
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
        internal static ReadOnlyMemory<byte> SerializeFromExternalString(string value)
        {
            if (value is null)
            {
                throw new ArgumentException(nameof(value));
            }
            string subValue = value.Substring(2);

            int origCheckSumHigh = CharToCheckSum(value[0]);
            int origCheckSumLow = CharToCheckSum(value[1]);
            int origCheckSum = ((origCheckSumHigh & 0x3f) << 6) | origCheckSumLow;
            //if ((origCheckSum & 0x400) == 0x400)
            //{
            //    subValue += "=";
            //    if ((origCheckSum & 0x800) == 0x800)
            //    {
            //        subValue += "=";
            //    }
            //}
            origCheckSum &= 0xFFF;
            //subValue = subValue.Replace('_', '+').Replace('~', '/');

            //var aValues = Convert.FromBase64String(subValue);
            var aValues = Decode(subValue).ToArray();

            int checkSum = 0;
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
                checkSum += aValues[i];
                checkSum &= 0xFFF;
            }

            if (origCheckSum != checkSum)
            {
                throw new Exception("External key string is not valid.");
            }
            return aValues;

        }
        internal static string SerializeToExternalString(ReadOnlySpan<byte> span)
        {
            Span<byte> value = stackalloc byte[span.Length];
            span.CopyTo(value);
            int checkSum = 0;

            for (int i = 0; i < value.Length; i++)
            {
                checkSum += value[i];
                checkSum &= 0xfff;
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
            var checkSumCharHigh = CheckSumToChar(checkSum >> 6).ToString();
            var checkSumCharLow = CheckSumToChar(checkSum & 0x3f).ToString();

            return checkSumCharHigh + checkSumCharLow  + EncodeToString(value);

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
                result = '-';
            }
            else
            {
                result = '_';
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
            else if ('-' == charCheckSum)
            {
                origCheckSum = 62;
            }
            else if ('_' == charCheckSum)
            {
                origCheckSum = 63;
            }
            else
            {
                throw new Exception("External key string has been corrupted.");
            }
            return origCheckSum;
        }

        private const string encodingChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_~";
        private static readonly char[] s_encode64Chars = encodingChars.ToCharArray();
        private static readonly byte[] s_encode64Utf8 = Encoding.UTF8.GetBytes(encodingChars);
        private static readonly byte[] s_decode64 = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 0, 0, 0, 0, 62, 0, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 0, 0, 0, 63, 0];


        /// <summary>
        /// Converts a byte array to a URL-safe encoded string.
        /// </summary>
        /// <param name="key">The binary value to encode.</param>
        /// <returns>A string representing the submitted value.</returns>
        public static string EncodeToString(ReadOnlySpan<byte> key)
        {
            if (key.IsEmpty)
            {
                throw new ArgumentNullException(nameof(key));
            }
            var keyLength = key.Length;
            var resultLength = keyLength + (keyLength / 3) + (keyLength % 3 > 0 ? 1 : 0);
            return string.Create(resultLength, key, (buffer, keyValue) =>
            {
                var encodeChars = s_encode64Chars;
                var keyLength = keyValue.Length;
                var b_index = 0;
                var quad = 0;
                var s_index = 0;
                for (b_index = 0; b_index < keyLength; b_index = b_index + 3)
                {
                    byte value = keyValue[b_index];
                    buffer[s_index] = encodeChars[value & 63];
                    s_index++;
                    quad = ((value & 192) >> 6);
                    if (b_index + 1 < keyLength)
                    {
                        value = keyValue[b_index + 1];
                        buffer[s_index] = encodeChars[value & 63];
                        s_index++;
                        quad = quad | ((value & 192) >> 4);
                    }
                    if (b_index + 2 < keyLength)
                    { 
                        value = keyValue[b_index + 2];
                        buffer[s_index] = encodeChars[value & 63];
                        s_index++;
                        quad = quad | ((value & 192) >> 2);
                    }
                    buffer[s_index] = encodeChars[quad];
                    s_index++;
                }
            });
        }

        /// <summary>
        /// Converts a byte array to a URL-safe encoded string.
        /// </summary>
        /// <param name="key">The binary value to encode.</param>
        /// <returns>A string representing the submitted value.</returns>
        public static ReadOnlyMemory<byte> EncodeToUtf8(ReadOnlySpan<byte> key)
        {
            if (key.IsEmpty)
            {
                throw new ArgumentNullException(nameof(key));
            }
            var keyLength = key.Length;
            var resultLength = keyLength + (keyLength / 3) + (keyLength % 3 > 0 ? 1 : 0);

            Span<byte> result = stackalloc byte[resultLength];
            var encodeChars = s_encode64Utf8;
            var b_index = 0;
            var quad = 0;
            var s_index = 0;
            var keyValue = key;
            for (b_index = 0; b_index < keyLength; b_index = b_index + 3)
            {
                byte value = keyValue[b_index];
                result[s_index] = encodeChars[value & 63];
                s_index++;
                quad = ((value & 192) >> 6);
                if (b_index + 1 < keyLength)
                {
                    value = keyValue[b_index + 1];
                    result[s_index] = encodeChars[value & 63];
                    s_index++;
                    quad = quad | ((value & 192) >> 4);
                }
                if (b_index + 2 < keyLength)
                {
                    value = keyValue[b_index + 2];
                    result[s_index] = encodeChars[value & 63];
                    s_index++;
                    quad = quad | ((value & 192) >> 2);
                }
                result[s_index] = encodeChars[quad];
                s_index++;
            }
            return result.ToArray();
        }

        /// <summary>
        /// Reverts an encoded string back to a byte array.
        /// </summary>
        /// <param name="encoded">The encoded string as UTF8 encoded.</param>
        /// <returns>The orginal bytes that were encoded.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ReadOnlyMemory<byte> Decode(ReadOnlySpan<byte> encoded)
        {
            if (encoded.IsEmpty)
            {
                throw new ArgumentNullException(nameof(encoded));
            }
            var keyLength = encoded.Length;
            var resultLength = keyLength - (keyLength / 4) - (keyLength % 4 > 0 ? 1 : 0);
            Span<byte> result = stackalloc byte[resultLength];
            var decodeChars = s_decode64;
            var b_index = 0;
            byte quad = 0;
            var s_index = 0;

            while (b_index < keyLength)
            {
                if (b_index + 3 < keyLength)
                {
                    quad = decodeChars[encoded[b_index + 3]];
                    result[s_index] = (byte)(decodeChars[encoded[b_index]] | ((quad & 3) << 6));
                    b_index++;
                    s_index++;
                    result[s_index] = (byte)(decodeChars[encoded[b_index]] | ((quad & 12) << 4));
                    b_index++;
                    s_index++;
                    result[s_index] = (byte)(decodeChars[encoded[b_index]] | ((quad & 48) << 2));
                    b_index = b_index + 2;
                    s_index++;
                }
                else
                {
                    quad = decodeChars[encoded[keyLength - 1]];
                    if (b_index < keyLength - 1)
                    {
                        result[s_index] = (byte)(decodeChars[encoded[b_index]] | ((quad & 3) << 6));
                        b_index++;
                        s_index++;
                    }
                    if (b_index < keyLength - 1)
                    {
                        result[s_index] = (byte)(decodeChars[encoded[b_index]] | ((quad & 12) << 4));
                        b_index++;
                        s_index++;
                    }
                    if (b_index < keyLength - 1)
                    {
                        result[s_index] = (byte)(decodeChars[encoded[b_index]] | ((quad & 48) << 2));
                        b_index++;
                        s_index++;
                    }
                    b_index++;
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Reverts an encoded string back to a byte array.
        /// </summary>
        /// <param name="encoded">The encoded string.</param>
        /// <returns>The orginal bytes that were encoded.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ReadOnlyMemory<byte> Decode(string encoded)
        {
            return Decode(Encoding.UTF8.GetBytes(encoded));
        }
    }
}
