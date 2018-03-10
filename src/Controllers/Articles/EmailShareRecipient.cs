using System;

namespace api.Controllers.Articles {
	public class EmailShareRecipient {
		public EmailShareRecipient(string emailAddres, Guid userAccountId, bool isSuccessful) {
			EmailAddress = emailAddres;
			UserAccountId = userAccountId;
			IsSuccessful = isSuccessful;
		}
		public string EmailAddress { get; }
		public Guid UserAccountId { get; }
		public bool IsSuccessful { get; }
	}
}