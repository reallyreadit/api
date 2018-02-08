using System.Collections.Generic;
using api.DataAccess.Models;

namespace api.Controllers.Extension {
	public class CreateArticleAuthorEqualityComparer : IEqualityComparer<CreateArticleAuthor> {
		public bool Equals(CreateArticleAuthor x, CreateArticleAuthor y) => x?.Name == y?.Name && x?.Url == y?.Url;
		public int GetHashCode(CreateArticleAuthor obj) {
			unchecked {
				var hash = 17;
				hash = hash * 23 + (obj?.Name?.GetHashCode() ?? 0);
				hash = hash * 23 + (obj?.Url?.GetHashCode() ?? 0);
				return hash;
			}
		}
	}
}