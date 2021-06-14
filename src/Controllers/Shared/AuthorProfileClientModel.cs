namespace api.Controllers.Shared {
	public class AuthorProfileClientModel {
		public AuthorProfileClientModel(
			string name,
			string slug,
			int totalEarnings,
			string userName
		) {
			Name = name;
			Slug = slug;
			TotalEarnings = totalEarnings;
			TotalPayouts = 0;
			UserName = userName;
		}
		public string Name { get; }
		public string Slug { get; }
		public int TotalEarnings { get; }
		public int TotalPayouts { get; }
		public string UserName { get; }
	}
}