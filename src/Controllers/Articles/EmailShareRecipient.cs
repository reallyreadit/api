namespace api.Controllers.Articles {
	public class EmailShareRecipient {
		public EmailShareRecipient(string emailAddres, long userAccountId, bool isSuccessful) {
			EmailAddress = emailAddres;
			UserAccountId = userAccountId;
			IsSuccessful = isSuccessful;
		}
		public string EmailAddress { get; }
		public long UserAccountId { get; }
		public bool IsSuccessful { get; }
	}
}