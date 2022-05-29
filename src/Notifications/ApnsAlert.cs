// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using api.Formatting;

namespace api.Notifications {
	// total payload size is 4KB
	// 1 character = 1 byte when converted to json for transmission (i think)
	// budget ~500B for baseline payload
	// title = 500
	// subtitle = 500
	// body = 2000
	public class ApnsAlert {
		public ApnsAlert(
			string title
		) {
			Title = title
				.RemoveControlCharacters()
				.Truncate(500, appendEllipsis: false);
		}
		public ApnsAlert(
			string title,
			string body
		) : this(
			title
		) {
			Body = body.Truncate(2000, appendEllipsis: false);
		}
		public ApnsAlert(
			string title,
			string subtitle,
			string body
		) : this(
			title,
			body
		) {
			Subtitle = subtitle
				.RemoveControlCharacters()
				.Truncate(500, appendEllipsis: false);
		}
		public string Title { get; }
		public string Subtitle { get; }
		public string Body { get; }
	}
}