using System.Collections.Generic;

namespace api.DataAccess.Models {
	public class PostReferencePage : IDbPageResult<PostReference> {
		public PostReference[] PostReferences { get; set; }
		public int TotalCount { get; set; }

		IEnumerable<PostReference> IDbPageResult<PostReference>.Items => PostReferences;
	}
}