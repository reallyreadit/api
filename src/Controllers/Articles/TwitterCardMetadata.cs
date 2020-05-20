namespace api.Controllers.Articles {
	public class TwitterCardMetadata {
		public TwitterCardMetadata(
			string title,
			string description,
			string imageUrl
		) {
			Title = title;
			Description = description;
			ImageUrl = imageUrl;
		}
		public string Title { get; }
		public string Description { get; }
		public string ImageUrl { get; }
	}
}