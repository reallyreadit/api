using api.DataAccess.Models;

namespace api.Controllers.Shared {
	public class DonationRecipientClientModel {
		public DonationRecipientClientModel(
			DonationRecipient recipient
		) {
			Name = recipient.Name;
			Website = recipient.Website;
		}
		public string Name { get; }
		public string Website { get; }
	}
}