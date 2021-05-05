using System;
using api.DataAccess.Models;

namespace api.Controllers.Shared {
	public enum SubscriptionStatusClientModelType {
		NeverSubscribed = 1,
		PaymentConfirmationRequired = 2,
		PaymentFailed = 3,
		Active = 4,
		Lapsed = 5
	}
	public abstract class SubscriptionStatusClientModel {
		public static SubscriptionStatusClientModel FromSubscriptionStatus(UserAccount user, SubscriptionStatus status) {
			var isUserFreeForLife = user.DateCreated < new DateTime(2021, 5, 6, 4, 0, 0);
			if (status == null) {
				return new SubscriptionStatusNeverSubscribedClientModel(
					isUserFreeForLife: isUserFreeForLife
				);
			}
			var provider = SubscriptionProviderClientValueExtensions.FromSubscriptionProvider(status.Provider);
			var price = new SubscriptionPriceClientModel(
				id: status.LatestPeriod.ProviderPriceId,
				name: status.LatestPeriod.PriceLevelName,
				amount: status.LatestPeriod.PriceAmount
			);
			var subscriptionState = status.GetCurrentState(DateTime.UtcNow);
			switch (subscriptionState) {
				case SubscriptionState.Active:
					var autoRenewEnabled = status.IsAutoRenewEnabled();
					SubscriptionPriceClientModel autoRenewPrice;
					if (autoRenewEnabled) {
						if (status.LatestRenewalStatusChange?.PriceAmount != null) {
							autoRenewPrice = new SubscriptionPriceClientModel(
								id: status.LatestRenewalStatusChange.ProviderPriceId,
								name: status.LatestRenewalStatusChange.PriceLevelName,
								amount: status.LatestRenewalStatusChange.PriceAmount.Value
							);
						} else {
							autoRenewPrice = price;
						}
					} else {
						autoRenewPrice = null;
					}
					return new SubscriptionStatusActiveClientModel(
						provider: provider,
						price: price,
						currentPeriodBeginDate: status.LatestPeriod.BeginDate,
						currentPeriodEndDate: status.LatestPeriod.EndDate,
						currentPeriodRenewalGracePeriodEndDate: status.LatestPeriod.RenewalGracePeriodEndDate,
						autoRenewEnabled: autoRenewEnabled,
						autoRenewPrice: autoRenewPrice,
						isUserFreeForLife: isUserFreeForLife
					);
				case SubscriptionState.Lapsed:
					return new SubscriptionStatusLapsedClientModel(
						provider: provider,
						price: price,
						lastPeriodEndDate: status.LatestPeriod.EndDate,
						lastPeriodRenewalGracePeriodEndDate: status.LatestPeriod.RenewalGracePeriodEndDate,
						dateRefunded: status.LatestPeriod.DateRefunded,
						isUserFreeForLife: isUserFreeForLife
					);
				case SubscriptionState.Incomplete:
				case SubscriptionState.IncompleteExpired:
				case SubscriptionState.RenewalRequiresConfirmation:
				case SubscriptionState.RenewalFailed:
					if (
						(
							subscriptionState == SubscriptionState.Incomplete &&
							status.LatestPeriod.PaymentStatus == SubscriptionPaymentStatus.RequiresConfirmation
						) ||
						subscriptionState == SubscriptionState.RenewalRequiresConfirmation
					) {
						return new SubscriptionStatusPaymentConfirmationRequiredClientModel(
							provider: provider,
							price: price,
							invoiceId: status.LatestPeriod.ProviderPeriodId,
							isUserFreeForLife: isUserFreeForLife
						);
					}
					return new SubscriptionStatusPaymentFailedClientModel(
						provider: provider,
						price: price,
						isUserFreeForLife: isUserFreeForLife
					);
				default:
					throw new ArgumentOutOfRangeException($"Unexpected subscription state: {subscriptionState}");
			}
		}
		public SubscriptionStatusClientModel(
			SubscriptionStatusClientModelType type,
			bool isUserFreeForLife
		) {
			Type = type;
			IsUserFreeForLife = isUserFreeForLife;
		}
		public SubscriptionStatusClientModelType Type { get; }
		public bool IsUserFreeForLife { get; }
	}
	public class SubscriptionStatusNeverSubscribedClientModel : SubscriptionStatusClientModel {
		public SubscriptionStatusNeverSubscribedClientModel(
			bool isUserFreeForLife
		) :
		base (
			type: SubscriptionStatusClientModelType.NeverSubscribed,
			isUserFreeForLife: isUserFreeForLife
		) {

		}
	}
	public class SubscriptionStatusPaymentConfirmationRequiredClientModel : SubscriptionStatusClientModel {
		public SubscriptionStatusPaymentConfirmationRequiredClientModel(
			SubscriptionProviderClientValue provider,
			SubscriptionPriceClientModel price,
			string invoiceId,
			bool isUserFreeForLife
		) :
		base(
			type: SubscriptionStatusClientModelType.PaymentConfirmationRequired,
			isUserFreeForLife: isUserFreeForLife
		) {
			Provider = provider;
			Price = price;
			InvoiceId = invoiceId;
		}
		public SubscriptionProviderClientValue Provider { get; }
		public SubscriptionPriceClientModel Price { get; }
		public string InvoiceId { get; }
	}
	public class SubscriptionStatusPaymentFailedClientModel : SubscriptionStatusClientModel {
		public SubscriptionStatusPaymentFailedClientModel(
			SubscriptionProviderClientValue provider,
			SubscriptionPriceClientModel price,
			bool isUserFreeForLife
		) :
		base (
			type: SubscriptionStatusClientModelType.PaymentFailed,
			isUserFreeForLife: isUserFreeForLife
		) {
			Provider = provider;
			Price = price;
		}
		public SubscriptionProviderClientValue Provider { get; }
		public SubscriptionPriceClientModel Price { get; }
	}
	public class SubscriptionStatusActiveClientModel : SubscriptionStatusClientModel {
		public SubscriptionStatusActiveClientModel(
			SubscriptionProviderClientValue provider,
			SubscriptionPriceClientModel price,
			DateTime currentPeriodBeginDate,
			DateTime currentPeriodEndDate,
			DateTime currentPeriodRenewalGracePeriodEndDate,
			bool autoRenewEnabled,
			SubscriptionPriceClientModel autoRenewPrice,
			bool isUserFreeForLife
		) :
		base(
			type: SubscriptionStatusClientModelType.Active,
			isUserFreeForLife: isUserFreeForLife
		) {
			Provider = provider;
			Price = price;
			CurrentPeriodBeginDate = currentPeriodBeginDate;
			CurrentPeriodEndDate = currentPeriodEndDate;
			CurrentPeriodRenewalGracePeriodEndDate = currentPeriodRenewalGracePeriodEndDate;
			AutoRenewEnabled = autoRenewEnabled;
			AutoRenewPrice = autoRenewPrice;
		}
		public SubscriptionProviderClientValue Provider { get; }
		public SubscriptionPriceClientModel Price { get; }
		public DateTime CurrentPeriodBeginDate { get; }
		public DateTime CurrentPeriodEndDate { get; }
		public DateTime CurrentPeriodRenewalGracePeriodEndDate { get; }
		public bool AutoRenewEnabled { get; }
		public SubscriptionPriceClientModel AutoRenewPrice { get; }
	}
	public class SubscriptionStatusLapsedClientModel : SubscriptionStatusClientModel {
		public SubscriptionStatusLapsedClientModel(
			SubscriptionProviderClientValue provider,
			SubscriptionPriceClientModel price,
			DateTime lastPeriodEndDate,
			DateTime lastPeriodRenewalGracePeriodEndDate,
			DateTime? dateRefunded,
			bool isUserFreeForLife
		) :
		base(
			type: SubscriptionStatusClientModelType.Lapsed,
			isUserFreeForLife: isUserFreeForLife
		) {
			Provider = provider;
			Price = price;
			LastPeriodEndDate = lastPeriodEndDate;
			LastPeriodRenewalGracePeriodEndDate = lastPeriodRenewalGracePeriodEndDate;
			LastPeriodDateRefunded = dateRefunded;
		}
		public SubscriptionProviderClientValue Provider { get; }
		public SubscriptionPriceClientModel Price { get; }
		public DateTime LastPeriodEndDate { get; }
		public DateTime LastPeriodRenewalGracePeriodEndDate { get; }
		public DateTime? LastPeriodDateRefunded { get; }
	}
}