namespace api.Messaging {
	public interface IEmailRecipient {
		string Name { get; }
		string EmailAddress { get; }
		bool IsEmailAddressConfirmed { get; }
	}
}