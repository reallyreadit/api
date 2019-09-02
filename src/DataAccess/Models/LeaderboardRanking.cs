namespace api.DataAccess.Models {
	public class LeaderboardRanking : IRanking {
		public string UserName { get; set; }
		public int Score { get; set; }
		public int Rank { get; set; }
	}
}