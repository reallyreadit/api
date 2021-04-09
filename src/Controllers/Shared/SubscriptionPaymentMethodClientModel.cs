using api.DataAccess.Models;

namespace api.Controllers.Shared {
	public class SubscriptionPaymentMethodClientModel {
		public SubscriptionPaymentMethodClientModel(
			SubscriptionPaymentMethod paymentMethod
		) {
			Id = paymentMethod.ProviderPaymentMethodId;
			Wallet = SubscriptionPaymentMethodWalletClientValueExtensions.FromSubscriptionPaymentMethodWallet(
				paymentMethod.Wallet
			);
			Brand = SubscriptionPaymentMethodBrandClientValueExtensions.FromSubscriptionPaymentMethodBrand(
				paymentMethod.Brand
			);
			LastFourDigits = paymentMethod.LastFourDigits;
			ExpirationMonth = paymentMethod.ExpirationMonth;
			ExpirationYear	= paymentMethod.ExpirationYear;
		}
		public string Id { get; }
		public SubscriptionPaymentMethodWalletClientValue Wallet { get; }
		public SubscriptionPaymentMethodBrandClientValue Brand { get; }
		public string LastFourDigits { get; }
		public int ExpirationMonth { get; }
		public int ExpirationYear { get; }
	}
}