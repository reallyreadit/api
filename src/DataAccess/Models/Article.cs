using System;

namespace api.DataAccess.Models {
	public class Article {
		public Guid Id { get; set; }
		public string Title { get; set; }
		public string Slug { get; set; }
		public string Author { get; set; }
		public DateTime DatePublished { get; set; }
		public string Source { get; set; }
		public string Url { get; set; }
		public int CommentCount { get; set; }
	}
}