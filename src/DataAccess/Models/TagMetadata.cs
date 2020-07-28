namespace api.DataAccess.Models {
	public class TagMetadata {
		public TagMetadata(
			string name,
			string slug
		) {
			Name = name;
			Slug = slug;
		}
		public string Name { get; }
		public string Slug { get; set; }
	}
}