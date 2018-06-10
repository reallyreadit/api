using System;

namespace api.DataAccess.Models {
	public class UserArticle {
		public long Id { get; set; }
		public string Title { get; set; }
		public string Slug { get; set; }
		public long SourceId { get; set; }
		public string Source { get; set; }
		public DateTime? DatePublished { get; set; }
		public DateTime? DateModified { get; set; }
		public string Section { get; set; }
		public string Description { get; set; }
		public DateTime? AotdTimestamp { get; set; }
		public int Score { get; set; }
		public string Url { get; set; }
		public string[] Authors { get; set; }
		public string[] Tags { get; set; }
		public int WordCount { get; set; }
		public int ReadableWordCount { get; set; }
		public int PageCount { get; set; }
		public int CommentCount { get; set; }
		public DateTime? LatestCommentDate { get; set; }
		public int ReadCount { get; set; }
		public DateTime? LatestReadDate { get; set; }
		public long UserAccountId { get; set; }
		public int WordsRead { get; set; }
		public DateTime? DateCreated { get; set; }
		public DateTime? LastModified { get; set; }
		public decimal PercentComplete { get; set; }
		public bool IsRead { get; set; }
		public DateTime? DateCompleted { get; set; }
		public DateTime? DateStarred { get; set; }
	}
}