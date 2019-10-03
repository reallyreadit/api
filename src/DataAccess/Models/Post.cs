using System;

namespace api.DataAccess.Models {
	public class Post {
		public DateTime PostDateCreated { get; set; }
		public string UserName { get; set; }
		public long? CommentId { get; set; }
		public string CommentText { get; set; }
		public long? SilentPostId { get; set; }
		public bool HasAlert { get; set; }
	}
}