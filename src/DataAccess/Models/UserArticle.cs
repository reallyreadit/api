using System;

namespace api.DataAccess.Models {
	public class UserArticle {
		public Guid Id { get; set; }
		public string Title { get; set; }
		public string Slug { get; set; }
		public Guid SourceId { get; set; }
		public string Source { get; set; }
		public DateTime? DatePublished { get; set; }
		public DateTime? DateModified { get; set; }
		public string Section { get; set; }
		public string Description { get; set; }
		public string Url { get; set; }
		public string[] Authors { get; set; }
		public string[] Tags { get; set; }
		public int WordCount { get; set; }
		public int ReadableWordCount { get; set; }
		public int PageCount { get; set; }
		public int CommentCount { get; set; }
		public DateTime? LatestCommentDate { get; set; }
		public Guid UserAccountId { get; set; }
		public int WordsRead { get; set; }
		public DateTime? DateCreated { get; set; }
		public DateTime? DateStarred { get; set; }
		public decimal PercentComplete => ((decimal)WordsRead / ReadableWordCount) * 100;
	}
}