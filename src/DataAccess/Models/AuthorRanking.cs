namespace api.DataAccess.Models {
	public class AuthorRanking : IRanking {
		public string Name { get; set; }
		public string Slug { get; set; }
		public int Score { get; set; }
		public int Rank { get; set; }
	}
}