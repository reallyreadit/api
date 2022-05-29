// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

namespace api.Configuration {
	public class TwitterAuthOptions {
		public string BrowserAuthCallback { get; set; }
		public string BrowserLinkCallback { get; set; }
		public string BrowserPopupCallback { get; set; }
		public string ConsumerKey { get; set; }
		public string ConsumerSecret { get; set; }
		public TwitterAccountOptions SearchAccount { get; set; }
		public string TwitterApiServerUrl { get; set; }
		public string TwitterUploadServerUrl { get; set; }
		public string WebViewCallback { get; set; }
	}
}