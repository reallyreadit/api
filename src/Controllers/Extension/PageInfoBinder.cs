using api.DataAccess.Models;

namespace api.Controllers.Extension {
	public class PageInfoBinder {
		public string Url { get; set; }
		public int? Number { get; set; }
		public int WordCount { get; set; }
		public int ReadableWordCount { get; set; }
		public ArticleBinder Article { get; set; } = new ArticleBinder();
		public class ArticleBinder {
			public string Title { get; set; }
			public SourceBinder Source { get; set; } = new SourceBinder();
			public class SourceBinder {
				public string Name { get; set; }
				public string Url { get; set; }
			}
			public string DatePublished { get; set; }
			public string DateModified { get; set; }
			public AuthorBinder[] Authors { get; set; } = new AuthorBinder[0];
			public class AuthorBinder {
				public string Name { get; set; }
				public string Url { get; set; }
				public override bool Equals(object obj) {
					var author = obj as AuthorBinder;
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
			public string Section { get; set; }
			public string Description { get; set; }
			public string[] Tags { get; set; } = new string[0];
			public PageLinkBinder[] PageLinks { get; set; } = new PageLinkBinder[0];
			public class PageLinkBinder {
				public int Number { get; set; }
				public string Url { get; set; }
			}
		}
	}
}