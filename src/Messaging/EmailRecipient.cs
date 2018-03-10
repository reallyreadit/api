namespace api.Messaging {
	public class EmailRecipient : IEmailRecipient {
		public EmailRecipient(string emailAddress, bool isEmailAddressConfirmed = false) {
			EmailAddress = emailAddress;
			IsEmailAddressConfirmed = isEmailAddressConfirmed;
		}
		public EmailRecipient(string name, string emailAddress, bool isEmailAddressConfirmed = false) {
			Name = name;
			EmailAddress = emailAddress;
			IsEmailAddressConfirmed = isEmailAddressConfirmed;
		}
		public string Name { get; }
		public string EmailAddress { get; }
		public bool IsEmailAddressConfirmed { get; }
	}
}