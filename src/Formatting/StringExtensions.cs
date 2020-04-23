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
		public static string Truncate(this string instance, int limit, bool appendEllipsis = true) {
			if (instance == null || instance.Length <= limit) {
				return instance;
			}
			var ellipsis = "...";
			if (appendEllipsis && limit > ellipsis.Length) {
				return instance.Substring(0, limit - ellipsis.Length) + ellipsis;
			}
			return instance.Substring(0, limit);
		}
	}
}