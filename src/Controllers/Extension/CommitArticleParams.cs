using System;

namespace api.Controllers.Extension {
	public class CommitArticleParams {
		public string Slug { get; set; }
		public string Title { get; set; }
		public int WordCount { get; set; }
		public int[] ReadState { get; set; }
		public double PercentComplete { get; set; }
		public string Url { get; set; }
		public DateTime? DatePublished { get; set; }
		public string Author { get; set; }
		public int PageNumber { get; set; }
		public PageLink[] PageLinks { get; set; }
		public Guid SourceId { get; set; }

		public class PageLink {
			public string Url { get; set; }
			public int PageNumber { get; set; }
		}
	}
}