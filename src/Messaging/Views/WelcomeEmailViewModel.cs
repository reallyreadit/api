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