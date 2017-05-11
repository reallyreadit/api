namespace api.Messaging {
	public class EmailAddress {
		public EmailAddress(string name, string address) {
			Name = name;
			Address = address;
		}
		public string Name { get; }
		public string Address { get; }
	}
}