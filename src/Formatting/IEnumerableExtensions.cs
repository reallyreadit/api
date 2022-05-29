// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

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