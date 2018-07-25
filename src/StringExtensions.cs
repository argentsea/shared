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
    }
}
