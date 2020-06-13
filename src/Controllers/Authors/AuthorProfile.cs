namespace api.Controllers.AuthorsController {
	public class AuthorProfile {
		public AuthorProfile(
			string name
		) {
			Name = name;
		}
		public string Name { get; }
	}
}