namespace api.Controllers.Articles {
	public class SearchQuery {
		public string[] Authors { get; set; } = new string[0];
		public string[] Sources { get; set; } = new string[0];
		public string[] Tags { get; set; } = new string[0];
		public int? MinLength { get; set; }
		public int? MaxLength { get; set; }
	}
}