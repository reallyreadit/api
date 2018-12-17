using System;

namespace api.DataAccess.Models {
	public class UserArticle {
		public long Id { get; set; }
		public string Title { get; set; }
		public string Slug { get; set; }
		public string Source { get; set; }
		public DateTime? DatePublished { get; set; }
		public string Section { get; set; }
		public string Description { get; set; }
		public DateTime? AotdTimestamp { get; set; }
		public string Url { get; set; }
		public string[] Authors { get; set; }
		public string[] Tags { get; set; }
		public int WordCount { get; set; }
		public int CommentCount { get; set; }
		public int ReadCount { get; set; }
		public DateTime? DateCreated { get; set; }
		public decimal PercentComplete { get; set; }
		public bool IsRead { get; set; }
		public DateTime? DateStarred { get; set; }
		public string ProofToken { get; set; }
	}
}