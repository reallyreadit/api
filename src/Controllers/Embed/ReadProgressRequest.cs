namespace api.Controllers.Embed {
	public class ReadProgressRequest {
		public long ArticleId { get; set; }
		public int[] ReadState { get; set; }
	}
}