using System;
using System.Collections.Generic;
using System.Text;

namespace ArgentSea
{
    public static class StringExtensions
    {
		[Flags]
		public enum InputCleaningOptions {
			/// <summary>
			/// Do not remove Unicode surrogate values, like Emoji.
			/// </summary>
			AllowEmojis,
			/// <summary>
			/// All “control” characters are removed by default; setting this flag preserve keep CrLf and/or Cr. It also ensures Windows line endings (Cr+Lf) when encountering Lf only (Unix line endings).
			/// </summary>
			AllowMultiline,
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
