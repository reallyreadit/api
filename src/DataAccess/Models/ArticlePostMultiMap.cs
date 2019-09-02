namespace api.DataAccess.Models {
	public class ArticlePostMultiMap {
		public ArticlePostMultiMap(Article article, Post post) {
			Article = article;
			Post = post;
		}
		public Article Article { get; }
		public Post Post { get; }
	}
}