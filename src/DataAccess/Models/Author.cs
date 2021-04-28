namespace api.DataAccess.Models {
	public class Author {
		public long Id { get; set; }
		public string Name { get; set; }
		public string Url { get; set; }
		public string TwitterHandle { get; set; }
		public TwitterHandleAssignment TwitterHandleAssignment { get; set; }
		public string Slug { get; set; }
		public string EmailAddress { get; set; }
	}
}