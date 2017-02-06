using System;

namespace api.Controllers.Extension {
	public class ArticleInfoBinder {
		public string Title { get; set; }
		public DateTime? DatePublished { get; set; }
		public string Author { get; set; }
		public PageLinkInfoBinder[] PageLinks { get; set; }
	}
}