using api.DataAccess.Models;

namespace api.Controllers.AuthorsController {
	public class ContactStatusAssignment {
		public string Slug { get; set; }
		public AuthorContactStatus Status { get; set; }
	}
}