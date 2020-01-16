using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace api.Serialization {
	public class PemParser {
		public static PemSection[] Parse(string fileContents) {
			return Regex
				.Matches(
					fileContents,
					@"^\s*\-+\s*begin\s*(?<type>(?:[^-]|\-(?!\-))+)\-+(?<body>[^-]+)\-+\s*end\s*\k<type>\-+\s*$",
					RegexOptions.IgnoreCase | RegexOptions.Multiline
				)
				.Select(
					match => new PemSection(
						match.Groups["type"].Value.Trim(),
						Regex.Replace(match.Groups["body"].Value, @"\s", "")
					)
				)
				.ToArray();
		}
	}
}