namespace api.DataAccess.Models {
	public class AuthorMetadata {
		public AuthorMetadata(
			string name,
			string url,
			string slug
		) {
			Name = name;
			Url = url;
			Slug = slug;
		}
		public string Name { get; }
		public string Url { get; }
		public string Slug { get; }
	}
}