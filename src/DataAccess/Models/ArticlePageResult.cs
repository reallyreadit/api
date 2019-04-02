using System;

namespace api.DataAccess.Models {
	public class ArticlePageResult : Article, IDbPageResult {
		public int TotalCount { get; set; }
	}
}