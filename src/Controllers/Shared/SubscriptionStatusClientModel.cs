using System;
using api.DataAccess.Models;

namespace api.Controllers.Shared {
	public enum SubscriptionStatusClientModelType {
		NeverSubscribed = 1,
		Incomplete = 2,
		Active = 3,
		Lapsed = 4
	}
	public abstract class SubscriptionStatusClientModel {
		public static SubscriptionStatusClientModel FromSubscriptionStatus(UserAccount user, SubscriptionStatus status) {
			var isUserFreeForLife = user.DateCreated < new DateTime(2021, 1, 1);
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
					return new SubscriptionStatusActiveClientModel(
						provider: provider,
						price: price,
						currentPeriodBeginDate: status.LatestPeriod.BeginDate,
						currentPeriodEndDate: status.LatestPeriod.EndDate,
						isUserFreeForLife: isUserFreeForLife
					);
				case SubscriptionState.Lapsed:
					return new SubscriptionStatusLapsedClientModel(
						provider: provider,
						price: price,
						lastPeriodEndDate: status.LatestPeriod.EndDate,
						isUserFreeForLife: isUserFreeForLife
					);
				case SubscriptionState.Incomplete:
				case SubscriptionState.IncompleteExpired:
					return new SubscriptionStatusIncompleteClientModel(
						provider: provider,
						price: price,
						requiresConfirmation:
							subscriptionState == SubscriptionState.Incomplete &&
							status.LatestPeriod.PaymentStatus == SubscriptionPaymentStatus.RequiresConfirmation,
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
	public class SubscriptionStatusIncompleteClientModel : SubscriptionStatusClientModel {
		public SubscriptionStatusIncompleteClientModel(
			SubscriptionProviderClientValue provider,
			SubscriptionPriceClientModel price,
			bool requiresConfirmation,
			bool isUserFreeForLife
		) :
		base(
			type: SubscriptionStatusClientModelType.Incomplete,
			isUserFreeForLife: isUserFreeForLife
		) {
			Provider = provider;
			Price = price;
			RequiresConfirmation = requiresConfirmation;
		}
		public SubscriptionProviderClientValue Provider { get; }
		public SubscriptionPriceClientModel Price { get; }
		public bool RequiresConfirmation { get; }
	}
	public class SubscriptionStatusActiveClientModel : SubscriptionStatusClientModel {
		public SubscriptionStatusActiveClientModel(
			SubscriptionProviderClientValue provider,
			SubscriptionPriceClientModel price,
			DateTime currentPeriodBeginDate,
			DateTime currentPeriodEndDate,
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
		}
		public SubscriptionProviderClientValue Provider { get; }
		public SubscriptionPriceClientModel Price { get; }
		public DateTime CurrentPeriodBeginDate { get; }
		public DateTime CurrentPeriodEndDate { get; }
	}
	public class SubscriptionStatusLapsedClientModel : SubscriptionStatusClientModel {
		public SubscriptionStatusLapsedClientModel(
			SubscriptionProviderClientValue provider,
			SubscriptionPriceClientModel price,
			DateTime lastPeriodEndDate,
			bool isUserFreeForLife
		) :
		base(
			type: SubscriptionStatusClientModelType.Lapsed,
			isUserFreeForLife: isUserFreeForLife
		) {
			Provider = provider;
			Price = price;
			LastPeriodEndDate = lastPeriodEndDate;
		}
		public SubscriptionProviderClientValue Provider { get; }
		public SubscriptionPriceClientModel Price { get; }
		public DateTime LastPeriodEndDate { get; }
	}
}