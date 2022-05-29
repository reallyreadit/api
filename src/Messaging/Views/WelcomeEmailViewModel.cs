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
using api.Messaging.Views.Shared;

namespace api.Messaging.Views {
	public class WelcomeEmailViewModel {
		public WelcomeEmailViewModel(
			Uri downloadUrl,
			Uri profileUrl,
			string userName,
			ArticleViewModel shortRead,
			ArticleViewModel mediumRead,
			ArticleViewModel longRead
		) {
			DownloadUrl = downloadUrl.ToString();
			ProfileUrl = profileUrl.ToString();
			UserName = userName;
			ShortRead = shortRead;
			MediumRead = mediumRead;
			LongRead = longRead;
		}
		public string DownloadUrl { get; }
		public string ProfileUrl { get; }
		public string UserName { get; }
		public ArticleViewModel ShortRead { get; }
		public ArticleViewModel MediumRead { get; }
		public ArticleViewModel LongRead { get; }
	}
}