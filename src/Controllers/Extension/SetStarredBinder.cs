using System;

namespace api.Controllers.Extension {
	public class SetStarredBinder {
		public Guid ArticleId { get; set; }
		public bool IsStarred { get; set; }
	}
}