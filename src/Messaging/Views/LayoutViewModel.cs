// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using api.Configuration;

namespace api.Messaging.Views {
	public class LayoutViewModel {
		private readonly string imageBaseUrl;
		public LayoutViewModel(
			string title,
			string homeUrl,
			string imageBaseUrl,
			string openImageUrl
		) {
			Title = title;
			HomeUrl = homeUrl;
			this.imageBaseUrl = imageBaseUrl;
			OpenImageUrl = openImageUrl;
		}
		public LayoutViewModel(
			string title,
			string homeUrl,
			string imageBaseUrl,
			string openImageUrl,
			string subscription,
			string subscriptionsUrl
		) {
			Title = title;
			HomeUrl = homeUrl;
			this.imageBaseUrl = imageBaseUrl;
			OpenImageUrl = openImageUrl;
			Subscription = subscription;
			SubscriptionsUrl = subscriptionsUrl;
		}
		public string Title { get; }
		public string HomeUrl { get; }
		public string OpenImageUrl { get; }
		public string Subscription { get; }
		public string SubscriptionsUrl { get; }
		public string CreateImageUrl(string fileName) => imageBaseUrl + fileName;
	}
	public class LayoutViewModel<TContent> : LayoutViewModel {
		public LayoutViewModel(
			string title,
			string homeUrl,
			string imageBaseUrl,
			string openImageUrl,
			TContent content
		) : base(
			title,
			homeUrl,
			imageBaseUrl,
			openImageUrl
		) {
			Content = content;
		}
		public LayoutViewModel(
			string title,
			string homeUrl,
			string imageBaseUrl,
			string openImageUrl,
			string subscription,
			string subscriptionsUrl,
			TContent content
		) : base(
			title,
			homeUrl,
			imageBaseUrl,
			openImageUrl,
			subscription,
			subscriptionsUrl
		) {
			Content = content;
		}
		public TContent Content { get; }
	}
}