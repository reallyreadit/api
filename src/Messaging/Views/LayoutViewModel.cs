using api.Configuration;

namespace api.Messaging.Views {
	public class LayoutViewModel {
		public LayoutViewModel(
			string title,
			string homeUrl,
			string logoUrl,
			string openImageUrl
		) {
			Title = title;
			HomeUrl = homeUrl;
			LogoUrl = logoUrl;
			OpenImageUrl = openImageUrl;
		}
		public LayoutViewModel(
			string title,
			string homeUrl,
			string logoUrl,
			string openImageUrl,
			string subscription,
			string subscriptionsUrl
		) {
			Title = title;
			HomeUrl = homeUrl;
			LogoUrl = logoUrl;
			OpenImageUrl = openImageUrl;
			Subscription = subscription;
			SubscriptionsUrl = subscriptionsUrl;
		}
		public string Title { get; }
		public string HomeUrl { get; }
		public string LogoUrl { get; }
		public string OpenImageUrl { get; }
		public string Subscription { get; }
		public string SubscriptionsUrl { get; }
	}
	public class LayoutViewModel<TContent> : LayoutViewModel {
		public LayoutViewModel(
			string title,
			string homeUrl,
			string logoUrl,
			string openImageUrl,
			TContent content
		) : base(
			title,
			homeUrl,
			logoUrl,
			openImageUrl
		) {
			Content = content;
		}
		public LayoutViewModel(
			string title,
			string homeUrl,
			string logoUrl,
			string openImageUrl,
			string subscription,
			string subscriptionsUrl,
			TContent content
		) : base(
			title,
			homeUrl,
			logoUrl,
			openImageUrl,
			subscription,
			subscriptionsUrl
		) {
			Content = content;
		}
		public TContent Content { get; }
	}
}