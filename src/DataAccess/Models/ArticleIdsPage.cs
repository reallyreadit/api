using System.Collections.Generic;

namespace api.DataAccess.Models {
	public class ArticleIdsPage : IDbPageResult<long> {
		public long[] ArticleIds { get; set; }
		public int TotalCount { get; set; }

		IEnumerable<long> IDbPageResult<long>.Items => ArticleIds;
	}
}