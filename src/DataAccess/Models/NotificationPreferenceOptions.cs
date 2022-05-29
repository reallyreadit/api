// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

namespace api.DataAccess.Models {
	public class NotificationPreferenceOptions {
		public bool CompanyUpdateViaEmail { get; set; }
		public bool AotdViaEmail { get; set; }
		public bool AotdViaExtension { get; set; }
		public bool AotdViaPush { get; set; }
		public NotificationEventFrequency AotdDigestViaEmail { get; set; }
		public bool ReplyViaEmail { get; set; }
		public bool ReplyViaExtension { get; set; }
		public bool ReplyViaPush { get; set; }
		public NotificationEventFrequency ReplyDigestViaEmail { get; set; }
		public bool LoopbackViaEmail { get; set; }
		public bool LoopbackViaExtension { get; set; }
		public bool LoopbackViaPush { get; set; }
		public NotificationEventFrequency LoopbackDigestViaEmail { get; set; }
		public bool PostViaEmail { get; set; }
		public bool PostViaExtension { get; set; }
		public bool PostViaPush { get; set; }
		public NotificationEventFrequency PostDigestViaEmail { get; set; }
		public bool FollowerViaEmail { get; set; }
		public bool FollowerViaExtension { get; set; }
		public bool FollowerViaPush { get; set; }
		public NotificationEventFrequency FollowerDigestViaEmail { get; set; }
	}
}