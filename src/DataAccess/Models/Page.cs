using System;

namespace api.DataAccess.Models {
	public class Page {
		public Guid Id { get; set; }
		public Guid ArticleId { get; set; }
		public int Number { get; set; }
		public int WordCount { get; set; }
		public string Url { get; set; }
	}
}