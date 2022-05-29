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

namespace api.Analytics {
	public class SemanticVersion {
		public SemanticVersion(
			int major,
			int minor,
			int patch
		) {
			Major = major;
			Minor = minor;
			Patch = patch;
		}
		public SemanticVersion(string versionString) {
			var match = Regex.Match(versionString, @"^(\d+)\.(\d+)\.(\d+)$");
			if (!match.Success) {
				throw new ArgumentException("Invalid version string");
			}
			Major = Int32.Parse(match.Groups[1].Value);
			Minor = Int32.Parse(match.Groups[2].Value);
			Patch = Int32.Parse(match.Groups[3].Value);
		}
		public int CompareTo(SemanticVersion version) {
			if (Major != version.Major) {
				return Major.CompareTo(version.Major);
			}
			if (Minor != version.Minor) {
				return Minor.CompareTo(version.Minor);
			}
			return Patch.CompareTo(version.Patch);
		}
		public override string ToString() {
			return Major + "." + Minor + "." + Patch;
		}
		public int Major { get; }
		public int Minor { get; }
		public int Patch { get; }
	}
}