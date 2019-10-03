using System.Text.RegularExpressions;

namespace api.Messaging {
	public static class EmailFormatting {
		public static string ExtractEmailAddress(string toString) {
			var match = Regex.Match(
				input: toString,
				pattern: "<([^>]+)>"
			);
			var address = (
				match.Success ?
					match.Groups[1].Value :
					toString	
			);
			if (
				Regex.IsMatch(
					input: address,
					pattern: "^[^@]+@[^@]+$"
				)
			) {
				return address.Trim();
			}
			return null;
		}
	}
}