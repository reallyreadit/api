namespace api.Messaging {
	public class EmailMailbox {
		public EmailMailbox(string name, string address) {
			Name = name;
			Address = address;
		}
		public string Name { get; }
		public string Address { get; }
	}
}