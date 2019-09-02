namespace api.DataAccess.Models {
	public class StreakRanking : IRanking {
		public int DayCount { get; set; }
		public bool IncludesToday { get; set; }
		public int Rank { get; set; }
	}
}