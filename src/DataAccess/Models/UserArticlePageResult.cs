using System;

namespace api.DataAccess.Models {
	public class UserArticlePageResult : UserArticle, IDbPageResult {
		public int TotalCount { get; set; }
	}
}