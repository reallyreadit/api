namespace api.DataAccess.Models {
	public class CommentPageResult : Comment, IDbPageResult {
		public int TotalCount { get; set; }
	}
}