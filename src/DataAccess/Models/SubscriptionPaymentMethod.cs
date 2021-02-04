using System;
using api.Subscriptions;

namespace api.DataAccess.Models {
	public class SubscriptionPaymentMethod {
		public SubscriptionProvider Provider { get; set; }
		public string ProviderPaymentMethodId { get; set; }
		public string ProviderAccountId { get; set; }
		public DateTime DateCreated { get; set; }
		public SubscriptionPaymentMethodWallet Wallet { get; set; }
		public SubscriptionPaymentMethodBrand Brand { get; set; }
		public string LastFourDigits { get; set; }
		public string Country { get; set; }
		public int ExpirationMonth { get; set; }
		public int ExpirationYear { get; set; }
	}
}