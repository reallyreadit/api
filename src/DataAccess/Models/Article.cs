using System;

namespace api.DataAccess.Models {
	public class Article {
		public Guid Id { get; set; }
		public string Title { get; set; }
		public string Slug { get; set; }
		public Guid SourceId { get; set; }
		public DateTime? DatePublished { get; set; }
		public DateTime? DateModified { get; set; }
		public string Section { get; set; }
		public string Description { get; set; }
	}
}