using System;

namespace api.DataAccess.Models {
	public class AuthServicePost {
		public long Id { get; set; }
		public long IdentityId { get; set; }
		public DateTime DatePosted { get; set; }
		public long? CommentId { get; set; }
		public long? SilentPostId { get; set; }
		public string Content { get; set; }
	}
}