using System;

namespace api.DataAccess.Models {
	public class UserArticle {
		public Guid Id { get; set; }
		public string Title { get; set; }
		public int PercentComplete { get; set; }
	}
}