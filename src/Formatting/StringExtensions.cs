using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace api.Formatting {
	public static class StringExtensions {
		public static string MergeContiguousWhitespace(this string instance) {
			if (instance == null) {
				return null;
			}
			return Regex.Replace(
				input: instance,
				pattern: @"\s+",
				replacement: " "
			);
		}
		public static string RemoveControlCharacters(this string instance) {
			if (instance == null) {
				return null;
			}
			return new String(
				instance
					.Where(
						character => !Char.IsControl(character)
					)
					.ToArray()
			);
		}
	}
}