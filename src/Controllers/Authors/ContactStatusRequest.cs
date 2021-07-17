namespace api.Controllers.AuthorsController {
	public class ContactStatusRequest {
		public string ApiKey { get; set; }
		public ContactStatusAssignment[] Assignments { get; set; }
	}
}