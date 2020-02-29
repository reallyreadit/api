using System;
using System.Collections.Generic;
using System.Linq;

namespace api.Serialization {
	public static class QueryStringSerializer {
		public static string Serialize(IEnumerable<KeyValuePair<string, string>> query, bool includePrefix) {
			if (query == null || !query.Any()) {
				return String.Empty;
			}
			var queryString = String.Join(
				'&',
				query.Select(
					kvp => Uri.EscapeDataString(kvp.Key) + '=' + Uri.EscapeDataString(kvp.Value)
				)
			);
			if (includePrefix) {
				queryString = '?' + queryString;
			}
			return queryString;
		}
	}
}