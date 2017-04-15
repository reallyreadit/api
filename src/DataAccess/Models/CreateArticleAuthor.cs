namespace api.DataAccess.Models {
	public class CreateArticleAuthor {
		public string Name { get; set; }
		public string Url { get; set; }
		public override bool Equals(object obj) {
			var author = obj as CreateArticleAuthor;
			return author?.Name == this.Name && author?.Url == this.Url;
		}
		public override int GetHashCode() {
			unchecked {
				var hash = 17;
				hash = hash * 23 + (this.Name?.GetHashCode() ?? 0);
				hash = hash * 23 + (this.Url?.GetHashCode() ?? 0);
				return hash;
			}
		}
	}
}