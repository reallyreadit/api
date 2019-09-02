namespace api.DataAccess.Models {
	public class ArticlePostMultiMapPageResult : ArticlePostMultiMap, IDbPageResult {
		public ArticlePostMultiMapPageResult(Article article, Post post, long totalCount)
			:base(article, post)
		{
			TotalCount = (int)totalCount;
		}
		public int TotalCount { get; }
	}
}