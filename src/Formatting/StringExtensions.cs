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