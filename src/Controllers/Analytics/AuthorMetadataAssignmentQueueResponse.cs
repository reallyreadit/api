using System.Collections.Generic;
using System.Linq;
using api.DataAccess.Models;

namespace api.Controllers.Analytics {
	public class AuthorMetadataAssignmentQueueResponse {
		public AuthorMetadataAssignmentQueueResponse(IEnumerable<Article> articles) {
			Articles = articles.ToArray();
		}
		public Article[] Articles { get; }
	}
}