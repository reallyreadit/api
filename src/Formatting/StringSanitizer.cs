using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace api.Formatting {
	public class StringSanitizer {
		private readonly char spaceChar;
		private readonly string spaceString;
		private readonly char hyphenChar;
		private readonly string hyphenString;
		private readonly char[] generalJoinerChars;
		private readonly char[] punctuationConnectorChars;
		private readonly string nonWordGreedyRegexPattern;
		public StringSanitizer() {
			spaceChar = (char)0x0020;
			spaceString = spaceChar.ToString();
			hyphenChar = '-';
			hyphenString = hyphenChar.ToString();
			generalJoinerChars = Enumerable
				// ZWSP, ZWNJ, ZWJ, WJ, BOM
				.Range(0x200B, 3)
				.Concat(
					new[] {
						0x2060,
						0xFEFF
					}
				)
				.Select(
					n => (char)n
				)
				.ToArray();
			punctuationConnectorChars = new[] {
				(char)0x005F,	// LOW LINE
				(char)0x203F,	// UNDERTIE
				(char)0x2040,	// CHARACTER TIE
				(char)0x2054,	// INVERTED UNDERTIE
				(char)0xFE33,	// PRESENTATION FORM FOR VERTICAL LOW LINE
				(char)0xFE34,	// PRESENTATION FORM FOR VERTICAL WAVY LOW LINE,
				(char)0xFE4D,	// DASHED LOW LINE
				(char)0xFE4E,	// CENTERLINE LOW LINE
				(char)0xFE4F,	// WAVY LOW LINE
				(char)0xFF3F	// FULLWIDTH LOW LINE
			};
			nonWordGreedyRegexPattern = $@"[\W{new String(punctuationConnectorChars)}]+";
		}
		public string GenerateSlug(string text) {
			return Regex
				.Replace(
					text,
					nonWordGreedyRegexPattern,
					hyphenString
				)
				.Trim(hyphenChar)
				.ToLowerInvariant();
		}
		public string SanitizeSingleLine(string text) {
			// replace whitespace chars with space
			text = Regex.Replace(text, @"\s", spaceString);
			// remove control chars and joiner chars
			text = new String(
				text
					.Where(
						c => !Char.IsControl(c) && !generalJoinerChars.Contains(c)
					)
					.ToArray()
			);
			// replace punctuation connector chars with space
			foreach (var connectorChar in punctuationConnectorChars) {
				text = text.Replace(connectorChar, spaceChar);
			}
			// merge contiguous whitespace
			text = Regex.Replace(text, @"\s{2,}", spaceString);
			// trim whitespace
			return text.Trim();
		}
	}
}