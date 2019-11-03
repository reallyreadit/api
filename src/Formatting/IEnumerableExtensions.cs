using System;
using System.Collections.Generic;
using System.Linq;

namespace api.Formatting {
	public static class IEnumerableExtensions {
		public static string ToListString(
			this IEnumerable<string> items,
			int maxItemCount = 3
		) {
			items = items
				.Where(item => !String.IsNullOrWhiteSpace(item))
				.Select(item => item.Trim());
			if (items.Any()) {
				int remainder;
				if (maxItemCount > 0 && maxItemCount < items.Count()) {
					remainder = items.Count() - maxItemCount;
					items = items.Take(maxItemCount);
				} else {
					remainder = 0;
				}
				string list;
				if (items.Count() > 2) {
					list = String.Join(", ", items.Take(items.Count() - 2)) + " & " + items.Last();
				} else {
					list = String.Join(" & ", items);
				}
				if (remainder > 0) {
					list += $" (+ {remainder} more)";
				}
				return list;
			}
			return String.Empty;
		}
	}
}