using System;

namespace api.DataAccess.Models {
	public class ArticleAuthorAssignment {
		public long ArticleId { get; set; }
		public long AuthorId { get; set; }
		public DateTime DateAssigned { get; set; }
		public DateTime? DateUnassigned { get; set; }
		public AuthorAssignmentMethod AssignmentMethod { get; set; }
		public long? AssignedByUserAccountId { get; set; }
		public long? UnassignedByUserAccountId { get; set; }
	}
}