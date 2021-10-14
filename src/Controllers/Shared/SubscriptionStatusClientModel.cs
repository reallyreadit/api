using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using Npgsql;

namespace api.Controllers.Shared {
	public enum SubscriptionStatusClientModelType {
		NeverSubscribed = 1,
		PaymentConfirmationRequired = 2,
		PaymentFailed = 3,
		Active = 4,
		Lapsed = 5
	}
	public abstract class SubscriptionStatusClientModel {
		public static async Task<SubscriptionStatusClientModel> FromQuery(DatabaseOptions databaseOptions, long userAccountId) {
			UserAccount user;
			SubscriptionStatus status;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				user = await db.GetUserAccountById(userAccountId);
				status = await db.GetCurrentSubscriptionStatusForUserAccountAsync(user.Id);
			}
			return await SubscriptionStatusClientModel.FromQuery(databaseOptions, user, status);
		}
		public static async Task<SubscriptionStatusClientModel> FromQuery(DatabaseOptions databaseOptions, UserAccount user) {
			SubscriptionStatus status;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				status = await db.GetCurrentSubscriptionStatusForUserAccountAsync(user.Id);
			}
			return await SubscriptionStatusClientModel.FromQuery(databaseOptions, user, status);
		}
		public static async Task<SubscriptionStatusClientModel> FromQuery(DatabaseOptions databaseOptions, long userAccountId, SubscriptionStatus status) {
			UserAccount user;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				user = await db.GetUserAccountById(userAccountId);
			}
			return await SubscriptionStatusClientModel.FromQuery(databaseOptions, user, status);
		}
		public static async Task<SubscriptionStatusClientModel> FromQuery(DatabaseOptions databaseOptions, UserAccount user, SubscriptionStatus status) {
			var isUserFreeForLife = user.DateCreated < new DateTime(2021, 5, 6, 4, 0, 0);
			var subscriptionState = status?.GetCurrentState(DateTime.UtcNow);
			FreeTrialClientModel freeTrial;
			if (!isUserFreeForLife && subscriptionState != SubscriptionState.Active) {
				using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
					var freeTrialCredits = await db.GetFreeTrialCreditsForUserAccountAsync(user.Id);
					IEnumerable<FreeTrialArticleView> freeTrialViews;
					if (
						freeTrialCredits.Any(
							credit => credit.CreditType == FreeTrialCreditType.ArticleView && credit.AmountRemaining < credit.AmountCredited
						)
					) {
						freeTrialViews = await db.GetFreeArticleViewsForUserAccountAsync(user.Id);
					} else {
						freeTrialViews = new FreeTrialArticleView[0];
					}
					freeTrial = new FreeTrialClientModel(freeTrialCredits, freeTrialViews);
				}
			} else {
				freeTrial = null;
			}
			if (status == null) {
				return new SubscriptionStatusNeverSubscribedClientModel(
					freeTrial: freeTrial
				);
			}
			var provider = SubscriptionProviderClientValueExtensions.FromSubscriptionProvider(status.Provider);
			var price = new SubscriptionPriceClientModel(
				id: status.LatestPeriod.ProviderPriceId,
				name: status.LatestPeriod.PriceLevelName,
				amount: status.LatestPeriod.PriceAmount
			);
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
						freeTrial: freeTrial
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
							freeTrial: freeTrial
						);
					}
					return new SubscriptionStatusPaymentFailedClientModel(
						provider: provider,
						price: price,
						freeTrial: freeTrial
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
	public class SubscriptionStatusInactiveClientModel : SubscriptionStatusClientModel {
		public SubscriptionStatusInactiveClientModel(
			SubscriptionStatusClientModelType type,
			FreeTrialClientModel freeTrial
		) :
		base (
			type: type,
			isUserFreeForLife: freeTrial == null
		) {
			FreeTrial = freeTrial;
		}
		public FreeTrialClientModel FreeTrial { get; }
	}
	public class SubscriptionStatusNeverSubscribedClientModel : SubscriptionStatusInactiveClientModel {
		public SubscriptionStatusNeverSubscribedClientModel(
			FreeTrialClientModel freeTrial
		) :
		base (
			type: SubscriptionStatusClientModelType.NeverSubscribed,
			freeTrial: freeTrial
		) {

		}
	}
	public class SubscriptionStatusPaymentConfirmationRequiredClientModel : SubscriptionStatusInactiveClientModel {
		public SubscriptionStatusPaymentConfirmationRequiredClientModel(
			SubscriptionProviderClientValue provider,
			SubscriptionPriceClientModel price,
			string invoiceId,
			FreeTrialClientModel freeTrial
		) :
		base(
			type: SubscriptionStatusClientModelType.PaymentConfirmationRequired,
			freeTrial: freeTrial
		) {
			Provider = provider;
			Price = price;
			InvoiceId = invoiceId;
		}
		public SubscriptionProviderClientValue Provider { get; }
		public SubscriptionPriceClientModel Price { get; }
		public string InvoiceId { get; }
	}
	public class SubscriptionStatusPaymentFailedClientModel : SubscriptionStatusInactiveClientModel {
		public SubscriptionStatusPaymentFailedClientModel(
			SubscriptionProviderClientValue provider,
			SubscriptionPriceClientModel price,
			FreeTrialClientModel freeTrial
		) :
		base (
			type: SubscriptionStatusClientModelType.PaymentFailed,
			freeTrial: freeTrial
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
	public class SubscriptionStatusLapsedClientModel : SubscriptionStatusInactiveClientModel {
		public SubscriptionStatusLapsedClientModel(
			SubscriptionProviderClientValue provider,
			SubscriptionPriceClientModel price,
			DateTime lastPeriodEndDate,
			DateTime lastPeriodRenewalGracePeriodEndDate,
			DateTime? dateRefunded,
			FreeTrialClientModel freeTrial
		) :
		base(
			type: SubscriptionStatusClientModelType.Lapsed,
			freeTrial: freeTrial
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