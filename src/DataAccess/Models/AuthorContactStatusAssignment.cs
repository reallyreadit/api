using api.DataAccess.Serialization;

namespace api.DataAccess.Models {
	public class AuthorContactStatusAssignment {
		public AuthorContactStatusAssignment(
			string slug,
			AuthorContactStatus contactStatus
		) {
			Slug = slug;
			ContactStatus = PostgresSerialization.SerializeEnum(contactStatus);
		}
		public string Slug { get; }
		public string ContactStatus { get; }
	}
}