namespace api.Controllers.AuthorsController {
	public class ContactStatusResponse {
		public ContactStatusResponse(int updatedRecordCount) {
			UpdatedRecordCount = updatedRecordCount;
		}
		public int UpdatedRecordCount { get; }
	}
}