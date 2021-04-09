using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using api.Authentication;
using api.BackgroundProcessing;
using api.Configuration;
using api.Controllers.Shared;
using api.DataAccess;
using api.DataAccess.Models;
using api.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Controllers.Subscriptions {
	public class SubscriptionsController : Controller {
		private readonly DatabaseOptions databaseOptions;
		private readonly IHttpClientFactory httpClientFactory;
		private readonly ILogger<SubscriptionsController> logger;
		private readonly SubscriptionsOptions subscriptionsOptions;
		private readonly IBackgroundTaskQueue taskQueue;

		public SubscriptionsController(
			IOptions<DatabaseOptions> databaseOptions,
			IHttpClientFactory httpClientFactory,
			ILogger<SubscriptionsController> logger,
			IOptions<SubscriptionsOptions> subscriptionsOptions,
			IBackgroundTaskQueue taskQueue
		) {
			this.databaseOptions = databaseOptions.Value;
			this.httpClientFactory = httpClientFactory;
			this.logger = logger;
			this.subscriptionsOptions = subscriptionsOptions.Value;
			this.taskQueue = taskQueue;
		}

		/// <summary>Attempts to cancel the <c>Subscription</c>.</summary>
		/// <remarks>If an error occurrs it will be logged and the return value will be null.</remarks>
		/// <returns>A <c>Subscription</c> or <c>null</c>.</returns>
		private async Task<Stripe.Subscription> CancelSubscriptionAsync(string providerSubscriptionId) {
			// CancelAsync throws with resource_missing if the subscription has already been cancelled.
			try {
				return await new Stripe.SubscriptionService()
					.CancelAsync(
						id: providerSubscriptionId
					);
			} catch (Exception ex) {
				if ((ex as Stripe.StripeException)?.StripeError.Code != StripeErrorCode.ResourceMissing) {
					logger.LogError("Error cancelling Stripe subscription with id: {SubscriptionId}.", providerSubscriptionId);
				}
			}
			return null;
		}

		/// <summary>Attempts to create a StripePaymentResponse from the PaymentIntent.</summary>
		/// <remarks>If an error occurrs it will be logged and a ProblemDetails ObjectResult will be returned.</remarks>
		/// <returns>A StripePaymentResponse or a ProblemDetails ObjectResult.</returns>
		/// <param name="paymentIntent">A Stripe PaymentIntent object.</param>
		/// <param name="userAccount">An optional UserAccount. If null the UserAccount will be fetched from the database using the current user's id.</param>
		private async Task<ActionResult<StripePaymentResponse>> CreateStripePaymentResponseActionResultAsync(Stripe.PaymentIntent paymentIntent, UserAccount userAccount = null) {
			var userAccountId = User.GetUserAccountId();
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				try {
					return StripePaymentResponse.FromPaymentIntent(
						paymentIntent,
						SubscriptionStatusClientModel.FromSubscriptionStatus(
							userAccount ??
							await db.GetUserAccountById(
								userAccountId: userAccountId
							),
							await db.GetCurrentSubscriptionStatusForUserAccountAsync(
								userAccountId: userAccountId
							)
						)
					);
				} catch (Exception ex) {
					logger.LogError(ex, "Failed to create StripePaymentResponse for payment intent with id: {PaymentIntentId} and user with id: {UserId}", paymentIntent.Id, userAccountId);
					return Problem($"Failed to create response object.", statusCode: 500);
				}
			}
		}

		/// <summary>Attempts to get the Invoice and associated PaymentIntent.</summary>
		/// <remarks>If an error occurrs it will be logged and the return value will be null.</remarks>
		/// <returns>An Invoice with an expanded PaymentIntent property or null.</returns>
		private async Task<Stripe.Invoice> GetInvoiceWithPaymentIntentAsync(string invoiceId) {
			try {
				return await new Stripe.InvoiceService()
					.GetAsync(
						id: invoiceId,
						options: new Stripe.InvoiceGetOptions {
							Expand = new List<string> {
								StripeInvoiceExpandProperties.PaymentIntent
							}
						}
					);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to get Stripe invoice with id: {InvoiceId} for user with id: {UserId}.", invoiceId, User.GetUserAccountIdOrDefault());
				return null;
			}
		}

		/// <summary>Will return a <c>SubscriptionPriceLevel</c> from the given price selection.</summary>
		/// <remarks>
		/// <para>A new custom price will be created if one does not already exist.</para>
		/// <para>If an error occurrs it will be logged and the return value will be null.</para>
		/// </remarks>
		/// <returns>A <c>SubscriptionPriceLevel</c> or null.</returns>
		public async Task<SubscriptionPriceLevel> GetOrCreatePriceLevelFromPriceSelectionAsync(ISubscriptionPriceSelection selection) {
			// check for a price level or custom amount
			if (
				!String.IsNullOrWhiteSpace(selection.PriceLevelId)
			) {
				// check the price level id against the standard price levels
				return await GetStandardPriceLevel(SubscriptionProvider.Stripe, selection.PriceLevelId);
			} else {
				// verify the custom amount
				if (selection.CustomPriceAmount < 2500 || selection.CustomPriceAmount > 100000) {
					logger.LogError("Subscription custom price out of range: {Price}.", selection.CustomPriceAmount);
					return null;
				}
				// check for an existing price that matches the custom amount
				SubscriptionPriceLevel customPrice;
				using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
					customPrice = await db.GetCustomSubscriptionPriceLevelForProviderAsync(
						provider: SubscriptionProvider.Stripe,
						amount: selection.CustomPriceAmount
					);
				}
				if (customPrice != null) {
					// use the existing price
					return customPrice;
				} else {
					// create a new stripe price
					Stripe.Price stripeCustomPrice;
					try {
						stripeCustomPrice = await new Stripe.PriceService()
							.CreateAsync(
								new Stripe.PriceCreateOptions {
									Currency = "usd",
									UnitAmount = selection.CustomPriceAmount,
									Product = subscriptionsOptions.StripeSubscriptionProductId,
									Recurring = new Stripe.PriceRecurringOptions {
										Interval = "month"
									},
									Nickname = $"Custom Level ({(selection.CustomPriceAmount / 100).ToString("c2")})"
								}
							);
					} catch (Exception ex) {
						logger.LogError(ex, "Failed to create new Stripe price with amount: {Amount}.", selection.CustomPriceAmount);
						return null;
					}
					// create a new custom price (returns existing price if one already exists)
					using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
						try {
							customPrice = await db.CreateCustomSubscriptionPriceLevelAsync(
								provider: SubscriptionProvider.Stripe,
								providerPriceId: stripeCustomPrice.Id,
								dateCreated: stripeCustomPrice.Created,
								amount: (int)stripeCustomPrice.UnitAmount.Value
							);
						} catch (Exception ex) {
							logger.LogError(ex, "Failed to create price with id: {PriceId}. Stripe request id: {RequestId}.", stripeCustomPrice.Id, stripeCustomPrice.StripeResponse.RequestId);
							return null;
						}
					}
					// check for a duplicate stripe price
					if (customPrice.ProviderPriceId != stripeCustomPrice.Id) {
						logger.LogError("Created duplicate custom price with id: {PriceId}.", stripeCustomPrice.Id);
						try {
							await new Stripe.PriceService()
								.UpdateAsync(
									id: stripeCustomPrice.Id,
									options: new Stripe.PriceUpdateOptions {
										Active = false
									}
								);
						} catch (Exception ex) {
							logger.LogError(ex, "Failed to deactivate duplicate custom price with id: {PriceId}.", stripeCustomPrice.Id);
						}
					}
					// return the custom price
					return customPrice;
				}
			}
		}

		/// <summary>Attempts to get the price amount from the selection.</summary>
		/// <remarks>If an error occurrs it will be logged and the return value will be null.</remarks>
		/// <returns>The price as an integer or null.</returns>
		private async Task<int?> GetPriceAmountAsync(ISubscriptionPriceSelection selection) {
			if (
				!String.IsNullOrWhiteSpace(selection.PriceLevelId)
			) {
				return (await GetStandardPriceLevel(SubscriptionProvider.Stripe, selection.PriceLevelId))?.Amount;
			} else {
				return selection.CustomPriceAmount;
			}
		}

		private async Task<SubscriptionPriceLevel> GetStandardPriceLevel(SubscriptionProvider provider, string providerPriceId) {
			SubscriptionPriceLevel[] standardPriceLevels;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				standardPriceLevels = (
						await db.GetStandardSubscriptionPriceLevelsForProviderAsync(provider)
					)
					.ToArray();
			}
			var result = standardPriceLevels?.SingleOrDefault(
				priceLevel => priceLevel.ProviderPriceId == providerPriceId
			);
			if (result == null) {
				logger.LogError("Invalid standard price level id: {PriceLevelId}.", providerPriceId);
			}
			return result;
		}

		/// <summary>Attempts to get the <c>Subscription</c> along with the latest <c>Invoice</c> and its <c>PaymentIntent</c>.</summary>
		/// <remarks>If an error occurrs it will be logged and the return value will be null.</remarks>
		/// <returns>A <c>Subscription</c> with expanded <c>LatestInvoice</c> and <c>PaymentIntent</c> properties or null.</returns>
		private async Task<Stripe.Subscription> GetStripeSubscriptionWithLatestInvoiceAsync(string providerSubscriptionId) {
			try {
				return await new Stripe.SubscriptionService()
					.GetAsync(
						id: providerSubscriptionId,
						options: new Stripe.SubscriptionGetOptions {
							Expand = new List<string> {
								StripeSubscriptionExpandProperties.LatestInvoicePaymentIntent
							}
						}
					);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to get Stripe subscription with id: {SubscriptionId}.", providerSubscriptionId);
				return null;
			}
		}

		/// <summary>Attempts to retrieve the SubscriptionItem Id for the Subscription in order to generate a price change options item.</summary>
		/// <remarks>If any errors occur they will be logged and the return value will be null.</remarks>
		/// <returns>A SubscriptionItemOptions list or null.</returns>
		private async Task<List<Stripe.SubscriptionItemOptions>> GetSubscriptionItemOptionsForPriceChangeAsync(string providerSubscriptionId, string providerPriceId) {
			// retrieve the stripe subscription in order to get the subscription item id
			Stripe.Subscription stripeSubscription;
			try {
				stripeSubscription = await new Stripe.SubscriptionService()
					.GetAsync(
						id: providerSubscriptionId
					);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to get Stripe subscription with id: {SubscriptionId}.", providerSubscriptionId);
				return null;
			}
			// assign the new price to the existing subscription item id
			return new List<Stripe.SubscriptionItemOptions> {
				new Stripe.SubscriptionItemOptions {
					Id = stripeSubscription.Items.Data[0].Id,
					Price = providerPriceId
				}
			};
		}

		/// <summary>Attempts to pay the Invoice.</summary>
		/// <remarks>
		/// <para>If a CardError occurrs an attempt will be made to get an updated Invoice.</para>
		/// <para>All other types of errors will be logged and the return value will be null.</para>
		/// </remarks>
		/// <returns>An Invoice with an expanded PaymentIntent property or null.</returns>
		private async Task<Stripe.Invoice> PayInvoiceHandlingCardErrorsAsync(string invoiceId) {
			try {
				return await new Stripe.InvoiceService()
					.PayAsync(
						id: invoiceId,
						options: new Stripe.InvoicePayOptions {
							Expand = new List<string> {
								StripeInvoiceExpandProperties.PaymentIntent
							}
						}
					);
			} catch (Exception ex) {
				if ((ex as Stripe.StripeException)?.StripeError.Type == StripeErrorType.CardError) {
					return await GetInvoiceWithPaymentIntentAsync(invoiceId: invoiceId);
				} else {
					logger.LogError(ex, "Failed to pay Stripe invoice with id: {InvoiceId}.", invoiceId);
					return null;
				}
			}
		}

		/// <summary>Handles all invoice payment events and creates or updates the associated subscription period when appropriate.</summary>
		/// <remarks>If an error occurrs it will be logged and <c>success</c> will be <c>false</c>.</remarks>
		/// <returns>A success indicator and the associated period. The period will be null if success is false and may be null if success is true.</returns>
		/// <param name="invoice">An Invoice with an expanded PaymentIntent property.</param>
		private async Task<( bool IsSuccessful, SubscriptionPeriod Period )> ProcessInvoicePaymentAttemptAsync(Stripe.Invoice invoice) {
			// There should only be a single regular line item.
			Stripe.InvoiceLineItem
				subscriptionLineItem = invoice.Lines.SingleOrDefault(
					line => !line.Proration
				),
				prorationLineItem = invoice.Lines.SingleOrDefault(
					line => line.Proration
				);
			if (subscriptionLineItem == null) {
				logger.LogError("Unexpected number of line items on Stripe invoice with id: {InvoiceId}.", invoice.Id);
				return (
					IsSuccessful: false,
					Period: null
				);
			}

			// Check the billing reason and handle accordingly.
			switch (invoice.BillingReason) {
				case StripeInvoiceBillingReason.SubscriptionCycle:
					// Cancel the subscription if a renewal payment fails. Otherwise invoices will continue to be generated.
					// Cancelling automatically via Stripe payment settings also effects pending update payments so we can't use it
					// and must handle it manually here instead.
					if (invoice.PaymentIntent.Status == StripePaymentIntentStatus.RequiresPaymentMethod) {
						await CancelSubscriptionAsync(
							providerSubscriptionId: invoice.SubscriptionId
						);
					}
					break;
				case StripeInvoiceBillingReason.SubscriptionUpdate:
					// Ignore unpaid update invoices.
					if (!invoice.Paid) {
						return (
							IsSuccessful: true,
							Period: null
						);
					}
					// Make sure the subscription is not set to cancel at period end. The subscription could have been in an active/pending cancellation
					// state when the upgrade was attempted and Stripe does not clear the state on upgrade completion so we have to do it manually.
					await UpdateStripeSubscriptionAsync(
						providerSubscriptionId: invoice.SubscriptionId,
						options: new Stripe.SubscriptionUpdateOptions {
							CancelAtPeriodEnd = false
						}
					);
					break;
			}

			// Create or update the subscription period.
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				try {
					return (
						IsSuccessful: true,
						Period: await db.CreateOrUpdateSubscriptionPeriodAsync(
							provider: SubscriptionProvider.Stripe,
							providerSubscriptionId: invoice.SubscriptionId,
							providerPeriodId: invoice.Id,
							providerPriceId: subscriptionLineItem.Price.Id,
							providerPaymentMethodId: invoice.PaymentIntent.PaymentMethodId,
							beginDate: DateTimeOffset
								.FromUnixTimeSeconds(subscriptionLineItem.Period.Start)
								.UtcDateTime,
							endDate: DateTimeOffset
								.FromUnixTimeSeconds(subscriptionLineItem.Period.End)
								.UtcDateTime,
							dateCreated: invoice.Created,
							paymentStatus: SubscriptionPaymentStatusExtensions.FromStripePaymentIntentStatusString(invoice.PaymentIntent.Status),
							datePaid: invoice.StatusTransitions.PaidAt,
							dateRefunded: null,
							refundReason: null,
							prorationDiscount: prorationLineItem != null ?
								new Nullable<int>((int)prorationLineItem.Amount * -1) :
								null
						)
					);
				} catch (Exception ex) {
					logger.LogError(ex, "Failed to create or update subscription period associated with Stripe invoice with id: {InvoiceId}.", invoice.Id);
					return (
						IsSuccessful: false,
						Period: null
					);
				}
			}
		}

		private async Task<SubscriptionPaymentMethod> SetDefaultPaymentMethodAsync(string providerPaymentMethodId, string providerAccountId) {
			// attach stripe payment method to stripe customer (successful no-op if already attached and we need the payment method object either way)
			Stripe.PaymentMethod stripePaymentMethod;
			try {
				stripePaymentMethod = await new Stripe.PaymentMethodService()
					.AttachAsync(
						providerPaymentMethodId,
						new Stripe.PaymentMethodAttachOptions {
							Customer = providerAccountId
						}
					);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to attach Stripe payment method with id: {PaymentMethodId} to Stripe customer with id: {CustomerId}.", providerPaymentMethodId, providerAccountId);
				return null;
			}

			// check for an existing readup payment method
			SubscriptionPaymentMethod paymentMethod;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				paymentMethod = await db.GetSubscriptionPaymentMethodAsync(
					provider: SubscriptionProvider.Stripe,
					providerPaymentMethodId: stripePaymentMethod.Id
				);
			}

			// create if it doesn't exist or update if it's changed
			if (paymentMethod == null) {
				// create readup payment method
				var cardWallet = SubscriptionPaymentMethodWalletExtensions.FromStripePaymentMethodWalletString(stripePaymentMethod.Card.Wallet?.Type);
				if (cardWallet == SubscriptionPaymentMethodWallet.Unknown) {
					// non-critical error. log but keep going
					logger.LogError("Unexpected value for card wallet: {Wallet} on Stripe payment method with id: {PaymentMethodId}. Stripe request id: {RequestId}.", stripePaymentMethod.Card.Wallet?.Type, stripePaymentMethod.Id, stripePaymentMethod.StripeResponse.RequestId);
				}
				var cardBrand = SubscriptionPaymentMethodBrandExtensions.FromStripePaymentMethodBrandString(stripePaymentMethod.Card.Brand);
				if (cardBrand == SubscriptionPaymentMethodBrand.None) {
					// non-critical error. log but keep going
					logger.LogError("Unexpected value for card brand: {Brand} on Stripe payment method with id: {PaymentMethodId}. Stripe request id: {RequestId}.", stripePaymentMethod.Card.Brand, stripePaymentMethod.Id, stripePaymentMethod.StripeResponse.RequestId);
				}
				using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
					try {
						await db.CreateSubscriptionPaymentMethodAsync(
							provider: SubscriptionProvider.Stripe,
							providerPaymentMethodId: stripePaymentMethod.Id,
							providerAccountId: providerAccountId,
							dateCreated: stripePaymentMethod.Created,
							wallet: cardWallet,
							brand: cardBrand,
							lastFourDigits: stripePaymentMethod.Card.Last4,
							country: stripePaymentMethod.Card.Country,
							expirationMonth: (int)stripePaymentMethod.Card.ExpMonth,
							expirationYear: (int)stripePaymentMethod.Card.ExpYear
						);
					} catch (Exception ex) {
						logger.LogError(ex, "Failed to create payment method for Stripe payment method with id: {PaymentMethodId}. Stripe request id: {RequestId}.", stripePaymentMethod.Id, stripePaymentMethod.StripeResponse.RequestId);
						return null;
					}
				}
			} else if (
				paymentMethod.ExpirationMonth != stripePaymentMethod.Card.ExpMonth ||
				paymentMethod.ExpirationYear != stripePaymentMethod.Card.ExpYear
			) {
				// update the payment method
				paymentMethod = await UpdatePaymentMethodFromStripeAsync(stripePaymentMethod, SubscriptionEventSource.UserAction);
			}

			// update stripe customer's default invoice payment method (successful no-op if already default)
			try {
				await new Stripe.CustomerService()
					.UpdateAsync(
						providerAccountId,
						new Stripe.CustomerUpdateOptions() {
							InvoiceSettings = new Stripe.CustomerInvoiceSettingsOptions {
								DefaultPaymentMethod = stripePaymentMethod.Id
							}
						}
					);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to set Stripe payment method with id: {PaymentMethodId} as default for Stripe customer with id: {CustomerId}.", stripePaymentMethod.Id, providerAccountId);
				return null;
			}

			// set the readup default payment method (successful no-op if already default)
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				try {
					return await db.AssignDefaultSubscriptionPaymentMethod(
						provider: SubscriptionProvider.Stripe,
						providerAccountId: providerAccountId,
						providerPaymentMethodId: stripePaymentMethod.Id
					);
				} catch (Exception ex) {
					logger.LogError(ex, "Failed to set payment method with id: {PaymentMethodId} as default for subscription account with id: {CustomerId}.", stripePaymentMethod.Id, providerAccountId);
					return null;
				}
			}
		}

		/// <summary>Creates or updates accounts, subscriptions and periods from records supplied by an App Store notification or device receipt verification response.</summary>
		private async Task SyncSubscriptionsFromReceiptAsync(IAppStoreUnifiedReceipt receipt) {
			if (receipt.LatestReceiptInfo == null || !receipt.LatestReceiptInfo.Any() || String.IsNullOrWhiteSpace(receipt.LatestReceipt)) {
				return;
			}
			var accountGroups = receipt.LatestReceiptInfo.GroupBy(
				transaction => transaction.OriginalTransactionId
			);
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				foreach (var accountGroup in accountGroups) {
					var originalTransaction = accountGroup.Single(
						transaction => transaction.TransactionId == transaction.OriginalTransactionId
					);
					var originalTransactionId = originalTransaction.TransactionId;
					var originalPurchaseDate = DateTimeOffset
						.FromUnixTimeMilliseconds(
							Int64.Parse(originalTransaction.PurchaseDateMs)
						)
						.UtcDateTime;
					await db.CreateOrUpdateSubscriptionAccountAsync(
						provider: SubscriptionProvider.Apple,
						providerAccountId: originalTransaction.TransactionId,
						userAccountId: User.GetUserAccountIdOrDefault(),
						dateCreated: originalPurchaseDate
					);
					await db.CreateOrUpdateSubscriptionAsync(
						provider: SubscriptionProvider.Apple,
						providerSubscriptionId: originalTransaction.TransactionId,
						providerAccountId: originalTransaction.TransactionId,
						dateCreated: originalPurchaseDate,
						latestReceipt: receipt.LatestReceipt
					);
					foreach (
						var transaction in accountGroup.OrderBy(
							transaction => transaction.PurchaseDateMs
						)
					) {
						var purchaseDate = DateTimeOffset
							.FromUnixTimeMilliseconds(
								Int64.Parse(transaction.PurchaseDateMs)
							)
							.UtcDateTime;
						Nullable<DateTime> refundDate;
						string refundReason;
						if (transaction.CancellationDateMs != null && transaction.IsUpgraded != "true") {
							refundDate = new Nullable<DateTime>(
								DateTimeOffset
									.FromUnixTimeMilliseconds(
										Int64.Parse(transaction.CancellationDateMs)
									)
									.UtcDateTime
							);
							refundReason = transaction.CancellationReason ?? "-1";
						} else {
							refundDate = null;
							refundReason = null;
						}
						await db.CreateOrUpdateSubscriptionPeriodAsync(
							provider: SubscriptionProvider.Apple,
							providerPeriodId: transaction.WebOrderLineItemId,
							providerSubscriptionId: originalTransactionId,
							providerPriceId: transaction.ProductId,
							providerPaymentMethodId: null,
							beginDate: purchaseDate,
							endDate: DateTimeOffset
								.FromUnixTimeMilliseconds(
									Int64.Parse(transaction.ExpiresDateMs)
								)
								.UtcDateTime,
							dateCreated: purchaseDate,
							paymentStatus: SubscriptionPaymentStatus.Succeeded,
							datePaid: purchaseDate,
							dateRefunded: refundDate,
							refundReason: refundReason,
							prorationDiscount: null
						);
					}
				}
				foreach (var renewalInfo in receipt.PendingRenewalInfo) {
					await db.CreateSubscriptionRenewalStatusChangeAsync(
						provider: SubscriptionProvider.Apple,
						providerSubscriptionId: renewalInfo.OriginalTransactionId,
						dateCreated: DateTime.UtcNow,
						autoRenewEnabled: renewalInfo.AutoRenewStatus == "1",
						providerPriceId: renewalInfo.AutoRenewProductId,
						expirationIntent: renewalInfo.ExpirationIntent
					);
				}
			}
		}

		/// <summary>Attempts to update a <c>SubscriptionPaymentMethod</c> from a Stripe <c>PaymentMethod</c>.</summary>
		/// <remarks>If any errors occur they will be logged and the return value will be <c>null</c>.</remarks>
		/// <returns>An updated <c>SubscriptionPaymentMethod</c> or <c>null</c>.</returns>
		private async Task<SubscriptionPaymentMethod> UpdatePaymentMethodFromStripeAsync(Stripe.PaymentMethod paymentMethod, SubscriptionEventSource eventSource) {
			if (paymentMethod.Card == null) {
				logger.LogError("Stripe PaymentMethod with id: {PaymentMethodId} does not contain a card object.", paymentMethod.Id);
				return null;
			}
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				try {
					return await db.UpdateSubscriptionPaymentMethodAsync(
						provider: SubscriptionProvider.Stripe,
						providerPaymentMethodId: paymentMethod.Id,
						eventSource: eventSource,
						expirationMonth: (int)paymentMethod.Card.ExpMonth,
						expirationYear: (int)paymentMethod.Card.ExpYear
					);
				} catch (Exception ex) {
					logger.LogError(ex, "Failed to update payment method with id: {PaymentMethodId}.", paymentMethod.Id);
					return null;
				}
			}
		}

		/// <summary>Attempts to update a Stripe subscription.</summary>
		/// <remarks>If any errors occur they will be logged and the return value will be null.</remarks>
		/// <returns>An updated Subscription or null.</returns>
		private async Task<Stripe.Subscription> UpdateStripeSubscriptionAsync(string providerSubscriptionId, Stripe.SubscriptionUpdateOptions options) {
			try {
				return await new Stripe.SubscriptionService()
					.UpdateAsync(
						id: providerSubscriptionId,
						options: options
					);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to update Stripe subscription with id: {SubscriptionId}.", providerSubscriptionId);
				return null;
			}
		}

		/// <summary>Updates a Stripe subscription auto-renew status or schedules a price downgrade.</summary>
		/// <remarks>
		/// <para>A valid <c>providerPriceId</c> is always required.</para>
		/// <para>If <c>autoRenewEnabled</c> is <c>false</c> then <c>providerPriceId</c> must be equal to the current price, otherwise a lower price may be provided to schedule a downgrade.</para>
		/// </remarks>
		/// <returns>A <c>SubscriptionRenewalStatusChange</c> instance if successful or null if an error occurred.</returns>
		private async Task<SubscriptionRenewalStatusChange> UpdateStripeSubscriptionAutoRenewStatus(string providerSubscriptionId, bool autoRenewEnabled, string providerPriceId) {
			// get the item options to set the price
			var items = await GetSubscriptionItemOptionsForPriceChangeAsync(
				providerSubscriptionId: providerSubscriptionId,
				providerPriceId: providerPriceId
			);
			if (items == null) {
				return null;
			}
			// update the subscription
			var stripeSubscription = await UpdateStripeSubscriptionAsync(
				providerSubscriptionId: providerSubscriptionId,
				options: new Stripe.SubscriptionUpdateOptions {
					CancelAtPeriodEnd = !autoRenewEnabled,
					Items = items,
					ProrationBehavior = StripeSubscriptionProrationBehavior.None
				}
			);
			if (stripeSubscription == null) {
				return null;
			}
			// update the auto-renew status
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				try {
					return await db.CreateSubscriptionRenewalStatusChangeAsync(
						provider: SubscriptionProvider.Stripe,
						providerSubscriptionId: providerSubscriptionId,
						dateCreated: stripeSubscription.StripeResponse.Date?.UtcDateTime ?? DateTime.UtcNow,
						autoRenewEnabled: autoRenewEnabled,
						providerPriceId: providerPriceId,
						expirationIntent: null
					);
				} catch (Exception ex) {
					logger.LogError(ex, "Failed to create a subscription renewal status change for Stripe subscription with id: {SubscriptionId}.", providerSubscriptionId);
					return null;
				}
			}
		}

		private async Task<AppStoreReceiptVerificationResponse> VerifyAppStoreReceipt(string base64EncodedReceipt) {
			AppStoreReceiptVerificationResponse response;
			using (var httpClient = this.httpClientFactory.CreateClient()) {
				var appStoreResponse = await httpClient.PostAsync(
					requestUri: subscriptionsOptions.AppStoreSandboxUrl,
					new StringContent(
						content: JsonSerializer.Serialize(
							new AppStoreReceiptVerificationRequest {
								ReceiptData = base64EncodedReceipt,
								Password = subscriptionsOptions.AppleAppSecret,
								ExcludeOldTransactions = false
							}
						),
						encoding: Encoding.UTF8,
						mediaType: "application/json"
					)
				);
				var responseContent = await appStoreResponse.Content.ReadAsStringAsync();
				try {
					response = JsonSerializer.Deserialize<AppStoreReceiptVerificationResponse>(responseContent);
					// log receipt
					await System.IO.File.WriteAllTextAsync(
						path: $@"logs/{DateTime.UtcNow.ToString("s").Replace(':', '-')}_AppleSubscriptionValidation_{User.GetUserAccountId()}_{Path.GetRandomFileName()}",
						contents: responseContent
					);
				} catch (Exception ex) {
					logger.LogError(ex, "Failed to parse App Store receipt verification response body with content: {Body}.", responseContent);
					response = null;
				}
			}
			return response;
		}

		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> AppStoreNotification() {
			// read the body text
			string body;
			using (
				var bodyReader = new StreamReader(Request.Body)
			) {
				body = await bodyReader.ReadToEndAsync();
			}
			// parse the app store event
			AppStoreNotification notification;
			try {
				notification = JsonSerializer.Deserialize<AppStoreNotification>(body);
				if (notification.Password != subscriptionsOptions.AppleAppSecret) {
					throw new ArgumentException($"Invalid App Store Notification password: {notification.Password}");
				}
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to parse App Store notification request body with content: {Body}.", body);
				return BadRequest();
			}
			// sync to database as background task so we can return immediately
			taskQueue.QueueBackgroundWorkItem(
				async cancellationToken => {
					await SyncSubscriptionsFromReceiptAsync(notification.UnifiedReceipt);
				}
			);
			// log notification
			await System.IO.File.WriteAllTextAsync(
				path: $@"logs/{DateTime.UtcNow.ToString("s").Replace(':', '-')}_AppStoreNotification_{Path.GetRandomFileName()}",
				contents: body
			);
			// return ok
			return Ok();
		}

		[HttpPost]
		public async Task<ActionResult<AppleSubscriptionValidationResponse>> AppleSubscriptionValidation(
			[FromBody] AppleSubscriptionValidationRequest request
		) {
			// verify receipt with app store
			var response = await VerifyAppStoreReceipt(request.Base64EncodedReceipt);
			if (response == null) {
				return Problem("Failed to verify receipt with App Store.", statusCode: 500);
			}

			if (response.LatestReceiptInfo == null || !response.LatestReceiptInfo.Any()) {
				return new AppleSubscriptionEmptyReceiptResponse();
			}

			// sync to database
			await SyncSubscriptionsFromReceiptAsync(response);

			// return current status
			var latestTransaction = response.LatestReceiptInfo
				.OrderByDescending(
					transaction => transaction.ExpiresDateMs
				)
				.First();

			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				var subscriptionStatus = await db.GetSubscriptionStatusForSubscriptionAccountAsync(
					provider: SubscriptionProvider.Apple,
					providerAccountId: latestTransaction.OriginalTransactionId
				);
				var subscriptionUser = await db.GetUserAccountById(subscriptionStatus.UserAccountId);
				if (subscriptionUser.Id == User.GetUserAccountId()) {
					return new AppleSubscriptionAssociatedWithCurrentUserResponse(
						SubscriptionStatusClientModel.FromSubscriptionStatus(
							user: subscriptionUser,
							status: await db.GetCurrentSubscriptionStatusForUserAccountAsync(
								userAccountId: User.GetUserAccountId()
							)
						)
					);
				} else {
					return new AppleSubscriptionAssociatedWithAnotherUserResponse(
						subscribedUsername: subscriptionUser.Name
					);
				}
			}
		}

		[HttpPost]
		public IActionResult AppleSubscriptionPurchaseFailure(
			[FromBody] AppleSubscriptionPurchaseFailureRequest request
		) {
			logger.LogError("Received Apple subscription purchase failure with code: {Code} and description: {Description} for user with id: {UserId}.", request.Code, request.Description, User.GetUserAccountId());
			return Ok();
		}

		[HttpGet]
		public async Task<SubscriptionDistributionSummaryResponse> DistributionSummary() {
			var userAccountId = User.GetUserAccountId();
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				var status = await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccountId);
				var state = status?.GetCurrentState(DateTime.UtcNow);
				SubscriptionDistributionReportClientModel currentPeriod;
				if (state == SubscriptionState.Active) {
					currentPeriod = new SubscriptionDistributionReportClientModel(
						await db.RunDistributionReportForSubscriptionPeriodCalculationAsync(
							provider: status.Provider,
							providerPeriodId: status.LatestPeriod.ProviderPeriodId
						)
					);
				} else {
					currentPeriod = SubscriptionDistributionReportClientModel.Empty;
				}
				SubscriptionDistributionReportClientModel completedPeriods;
				if (state != null) {
					// make sure any lapsed periods have been distributed before running the report
					await db.CreateDistributionsForLapsedSubscriptionPeriodsAsync(
						userAccountId: userAccountId
					);
					completedPeriods = new SubscriptionDistributionReportClientModel(
						await db.RunDistributionReportForSubscriptionPeriodDistributionsAsync(
							userAccountId: userAccountId
						)
					);
				} else {
					completedPeriods = SubscriptionDistributionReportClientModel.Empty;
				}
				return new SubscriptionDistributionSummaryResponse(
					subscriptionStatus: SubscriptionStatusClientModel.FromSubscriptionStatus(
						await db.GetUserAccountById(userAccountId),
						status
					),
					currentPeriod: currentPeriod,
					completedPeriods: completedPeriods
				);
			}
		}

		[HttpPost]
		public async Task<SubscriptionStatusResponse> AppleSubscriptionStatusUpdateRequest() {
			SubscriptionStatus status;
			UserAccount user;
			var userAccountId = User.GetUserAccountId();
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				status = await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccountId: userAccountId);
				user = await db.GetUserAccountById(userAccountId: userAccountId);
			}
			if (
				status.Provider == SubscriptionProvider.Apple &&
				!String.IsNullOrWhiteSpace(status.LatestReceipt)
			) {
				var receiptResponse = await VerifyAppStoreReceipt(status.LatestReceipt);
				if (receiptResponse != null) {
					await SyncSubscriptionsFromReceiptAsync(receiptResponse);
					using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
						status = await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccountId: userAccountId);
					}
				}
			}
			return new SubscriptionStatusResponse(
				SubscriptionStatusClientModel.FromSubscriptionStatus(user, status)
			);
		}

		[HttpGet]
		public async Task<ActionResult<SubscriptionPriceLevelsResponse>> PriceLevels(
			[FromQuery] SubscriptionPriceLevelsRequest request
		) {
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				return new SubscriptionPriceLevelsResponse(
					prices: (await db.GetStandardSubscriptionPriceLevelsForProviderAsync(
							request.Provider.ToSubscriptionProvider()
						))
						.Select(
							price => new SubscriptionPriceClientModel(
								id: price.ProviderPriceId,
								name: price.Name,
								amount: price.Amount
							)
						)
						.ToArray()
				);
			}
		}

		[HttpGet]
		public async Task<ActionResult<SubscriptionStatusResponse>> Status() {
			var userAccountId = User.GetUserAccountId();
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				return new SubscriptionStatusResponse(
					status: SubscriptionStatusClientModel.FromSubscriptionStatus(
						user: await db.GetUserAccountById(userAccountId),
						status: await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccountId)
					)
				);
			}
		}

		[HttpPost]
		public async Task<ActionResult<SubscriptionStatusResponse>> StripeAutoRenewStatus(
			[FromBody] StripeAutoRenewStatusRequest request
		) {
			SubscriptionStatus status;
			UserAccount user;
			var userAccountId = User.GetUserAccountId();
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				status = await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccountId: userAccountId);
				user = await db.GetUserAccountById(userAccountId: userAccountId);
			}
			if (
				status.Provider == SubscriptionProvider.Stripe &&
				status.IsAutoRenewEnabled() != request.AutoRenewEnabled
			) {
				var statusChange = await UpdateStripeSubscriptionAutoRenewStatus(
					providerSubscriptionId: status.ProviderSubscriptionId,
					autoRenewEnabled: request.AutoRenewEnabled,
					providerPriceId: status.LatestPeriod.ProviderPriceId
				);
				if (statusChange == null) {
					return Problem("Failed to update subscription.", statusCode: 500);
				}
				using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
					status = await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccountId: userAccountId);
				}
			}
			return new SubscriptionStatusResponse(
				SubscriptionStatusClientModel.FromSubscriptionStatus(user, status)
			);
		}

		[HttpPost]
		public async Task<ActionResult<StripePaymentResponse>> StripePaymentConfirmation(
			[FromBody] StripePaymentConfirmationRequest request
		) {
			var userAccountId = User.GetUserAccountId();
			var invoice = await GetInvoiceWithPaymentIntentAsync(invoiceId: request.InvoiceId);
			if (invoice == null) {
				return Problem("Subscription not found.", statusCode: 500);
			}
			var invoiceProcessResult = await ProcessInvoicePaymentAttemptAsync(invoice);
			if (!invoiceProcessResult.IsSuccessful) {
				return Problem("Failed to update payment status.", statusCode: 500);
			}
			return await CreateStripePaymentResponseActionResultAsync(invoice.PaymentIntent);
		}

		[HttpPost]
		public async Task<ActionResult<StripePaymentResponse>> StripePriceChange(
			[FromBody] StripePriceChangeRequest request
		) {
			// get the new price amount
			var newPriceAmount = await GetPriceAmountAsync(request);
			if (!newPriceAmount.HasValue) {
				return Problem("Invalid price selection.", statusCode: 400);
			}
			// retrieve the current subscription status
			SubscriptionStatus status;
			var userAccountId = User.GetUserAccountId();
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				status = await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccountId: userAccountId);
			}
			// validate the price change
			SubscriptionPriceLevel newPrice;
			if (
				status.Provider != SubscriptionProvider.Stripe ||
				(
					newPriceAmount == status.LatestPeriod.PriceAmount &&
					(
						status.LatestRenewalStatusChange == null ||
						status.LatestRenewalStatusChange.ProviderPriceId == status.LatestPeriod.ProviderPriceId
					)
				) ||
				(newPrice = await GetOrCreatePriceLevelFromPriceSelectionAsync(request)) == null
			) {
				return Problem("Invalid price selection.", statusCode: 400);
			}
			// check if we're upgrading or downgrading
			if (newPriceAmount > status.LatestPeriod.PriceAmount) {
				// get the item options for the upgrade
				var upgradeItems = await GetSubscriptionItemOptionsForPriceChangeAsync(
					providerSubscriptionId: status.ProviderSubscriptionId,
					providerPriceId: newPrice.ProviderPriceId
				);
				if (upgradeItems == null) {
					return Problem("Failed to lookup subscription.", statusCode: 500);
				}
				// update the subscription
				var updatedSubscription = await UpdateStripeSubscriptionAsync(
					providerSubscriptionId: status.ProviderSubscriptionId,
					options: new Stripe.SubscriptionUpdateOptions {
						BillingCycleAnchor = Stripe.SubscriptionBillingCycleAnchor.Now,
						Expand = new List<string> {
							StripeSubscriptionExpandProperties.LatestInvoicePaymentIntent
						},
						Items = upgradeItems,
						PaymentBehavior = StripeSubscriptionPaymentBehavior.PendingIfIncomplete
					}
				);
				if (updatedSubscription == null) {
					return Problem("Failed to update subscription.", statusCode: 500);
				}
				// process the payment attempt
				var invoiceProcessResult = await ProcessInvoicePaymentAttemptAsync(updatedSubscription.LatestInvoice);
				if (!invoiceProcessResult.IsSuccessful) {
					return Problem("Failed to create new period.", statusCode: 500);
				}
				return await CreateStripePaymentResponseActionResultAsync(updatedSubscription.LatestInvoice.PaymentIntent);
			} else {
				// perform a downgrade
				var statusChange = await UpdateStripeSubscriptionAutoRenewStatus(
					providerSubscriptionId: status.ProviderSubscriptionId,
					autoRenewEnabled: true,
					providerPriceId: newPrice.ProviderPriceId
				);
				if (statusChange == null) {
					return Problem("Failed to update subscription.", statusCode: 500);
				}
				// return a payment success
				using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
					return new StripePaymentSucceededResponse(
						SubscriptionStatusClientModel.FromSubscriptionStatus(
							await db.GetUserAccountById(userAccountId: userAccountId),
							await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccountId: userAccountId)
						)
					);
				}
			}
		}

		[HttpPost]
		public async Task<ActionResult<StripePaymentResponse>> StripeSubscription(
			[FromBody] StripeSubscriptionPaymentRequest request
		) {
			// TODO: This entire mess of an operation should be locked with some type of db-generated token
			// to prevent the possibility that multiple requests for the same account are running concurrently.

			// retrieve the readup user account, subscription status and stripe account
			UserAccount userAccount;
			SubscriptionAccount subscriptionAccount;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				// first check for an active subscription
				var subscriptionStatus = await db.GetCurrentSubscriptionStatusForUserAccountAsync(
					User.GetUserAccountId()
				);
				if (
					subscriptionStatus?.GetCurrentState(DateTime.UtcNow) == SubscriptionState.Active
				) {
					logger.LogError("User with account id: {UserAccountId} attempted to create duplicate subscription.", User.GetUserAccountId());
					return Problem("You already have an active subscription.", statusCode: 400);
				}
				// get the user account
				userAccount = await db.GetUserAccountById(
					User.GetUserAccountId()
				);
				// check for an existing stripe account assigned to this user
				subscriptionAccount = (await db.GetSubscriptionAccountsForUserAccountAsync(userAccount.Id))
					.SingleOrDefault(
						account => account.Provider == SubscriptionProvider.Stripe
					);
			}

			// create and assign if one doesn't exist
			if (subscriptionAccount == null) {
				// create the stripe customer
				Stripe.Customer stripeCustomer;
				try {
					stripeCustomer = await new Stripe.CustomerService()
						.CreateAsync(
							new Stripe.CustomerCreateOptions {
								Email = userAccount.Email,
								Name = userAccount.Name,
								Metadata = new Dictionary<string, string>() {
									{
										"readup-user-account-id",
										userAccount.Id.ToString()
									}
								}
							}
						);
				} catch (Exception ex) {
					logger.LogError(ex, "Failed to create Stripe customer for user with account id: {UserAccountId}.", userAccount.Id);
					return Problem("Failed to create provider customer.", statusCode: 500);
				}

				// assign the stripe customer to the readup user
				using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
					try {
						subscriptionAccount = await db.CreateOrUpdateSubscriptionAccountAsync(
							provider: SubscriptionProvider.Stripe,
							providerAccountId: stripeCustomer.Id,
							userAccountId: userAccount.Id,
							dateCreated: stripeCustomer.Created
						);
					} catch (Exception ex) {
						logger.LogError(ex, "Failed to assign Stripe customer with id: {CustomerId} to user with account id: {UserAccountId}.", stripeCustomer.Id, userAccount.Id);
						return Problem("Failed to create subscription account.", statusCode: 500);
					}
				}
			}

			// set the default payment method
			var paymentMethod = await SetDefaultPaymentMethodAsync(
				providerPaymentMethodId: request.PaymentMethodId,
				providerAccountId: subscriptionAccount.ProviderAccountId
			);
			if (paymentMethod == null) {
				return Problem("Unable to set payment method.", statusCode: 500);
			}

			// get the price level from the request
			var priceLevel = await GetOrCreatePriceLevelFromPriceSelectionAsync(request);
			if (priceLevel == null) {
				return Problem("Unable to resolve price.", statusCode: 500);
			}

			// first check for an unexpired, incomplete subscription with a matching price and attempt to pay it if found
			SubscriptionStatus matchingIncompleteSubscription;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				matchingIncompleteSubscription = (await db.GetSubscriptionStatusesForUserAccountAsync(userAccount.Id))
					.SingleOrDefault(
						subscriptionStatus =>
							subscriptionStatus.GetCurrentState(DateTime.UtcNow) == SubscriptionState.Incomplete &&
							subscriptionStatus.LatestPeriod.ProviderPriceId == priceLevel.ProviderPriceId
					);
			}
			Stripe.Subscription stripeSubscription;
			if (matchingIncompleteSubscription != null) {
				// attempt to get the subscription from stripe
				stripeSubscription = await GetStripeSubscriptionWithLatestInvoiceAsync(providerSubscriptionId: matchingIncompleteSubscription.ProviderSubscriptionId);
				if (stripeSubscription == null) {
					return Problem("Failed to verify existing subscription status.", statusCode: 500);
				}
				// only process if incomplete, otherwise continue to subscription creation
				if (stripeSubscription.Status == api.Subscriptions.StripeSubscriptionStatus.Incomplete) {
					// attempt to pay the existing invoice
					var invoice = await PayInvoiceHandlingCardErrorsAsync(invoiceId: stripeSubscription.LatestInvoiceId);
					if (invoice == null) {
						return Problem("Failed to pay invoice.", statusCode: 500);
					}
					// update the period payment status and return
					var invoicePaymentProcessResult = await ProcessInvoicePaymentAttemptAsync(invoice);
					if (!invoicePaymentProcessResult.IsSuccessful) {
						return Problem("Failed to update payment status.", statusCode: 500);
					}
					return await CreateStripePaymentResponseActionResultAsync(invoice.PaymentIntent, userAccount);
				}
			}

			// create the stripe subscription
			try {
				stripeSubscription = await new Stripe.SubscriptionService()
					.CreateAsync(
						new Stripe.SubscriptionCreateOptions {
							Customer = subscriptionAccount.ProviderAccountId,
							Items = new List<Stripe.SubscriptionItemOptions> {
								new Stripe.SubscriptionItemOptions {
									Price = priceLevel.ProviderPriceId
								}
							},
							Expand = new List<string> {
								StripeSubscriptionExpandProperties.LatestInvoicePaymentIntent
							}
						}
					);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to create Stripe subscription with price id: {PriceId} for customer with id: {CustomerId}.", priceLevel.ProviderPriceId, subscriptionAccount.ProviderAccountId);
				return Problem("Failed to create Stripe subscription.", statusCode: 500);
			}

			// create the subscription
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				try {
					await db.CreateOrUpdateSubscriptionAsync(
						provider: SubscriptionProvider.Stripe,
						providerSubscriptionId: stripeSubscription.Id,
						providerAccountId: stripeSubscription.CustomerId,
						// Ensure that the creation date of the subscription and first period match.
						dateCreated: stripeSubscription.LatestInvoice.Created,
						latestReceipt: null
					);
				} catch (Exception ex) {
					logger.LogError(ex, "Failed to create subscription for Stripe subscription with id: {SubscriptionId}. Stripe request id: {RequestId}.", stripeSubscription.Id, stripeSubscription.StripeResponse.RequestId);
					return Problem("Failed to create subscription.", statusCode: 500);
				}
			}

			// Create the subscription period.
			var invoiceProcessResult = await ProcessInvoicePaymentAttemptAsync(stripeSubscription.LatestInvoice);
			if (!invoiceProcessResult.IsSuccessful) {
				return Problem("Failed to create subscription period.", statusCode: 500);
			}

			// Return a StripePaymentResponse.
			return await CreateStripePaymentResponseActionResultAsync(stripeSubscription.LatestInvoice.PaymentIntent, userAccount);
		}

		[HttpPost]
		public async Task<ActionResult<StripePaymentResponse>> StripeUpgradePayment(
			[FromBody] StripeSubscriptionPaymentRequest request
		) {
			// get the new price amount
			var newPriceAmount = await GetPriceAmountAsync(request);
			if (!newPriceAmount.HasValue) {
				return Problem("Invalid price selection.", statusCode: 400);
			}
			// retrieve the current subscription status
			SubscriptionStatus status;
			var userAccountId = User.GetUserAccountId();
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				status = await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccountId: userAccountId);
			}
			// check for a matching pending upgrade
			var stripeSubscription = await GetStripeSubscriptionWithLatestInvoiceAsync(providerSubscriptionId: status.ProviderSubscriptionId);
			if (
				stripeSubscription == null ||
				stripeSubscription.LatestInvoice.BillingReason != StripeInvoiceBillingReason.SubscriptionUpdate ||
				stripeSubscription.LatestInvoice.Status != StripeInvoiceStatus.Open ||
				!stripeSubscription.LatestInvoice.Lines.Any(
					line => line.Amount == newPriceAmount
				)
			) {
				return Problem("Failed to verify pending upgrade.", statusCode: 500);
			}
			// update the default payment method
			var paymentMethod = await SetDefaultPaymentMethodAsync(
				providerPaymentMethodId: request.PaymentMethodId,
				providerAccountId: status.ProviderAccountId
			);
			if (paymentMethod == null) {
				return Problem("Unable to update method.", statusCode: 500);
			}
			// attempt to pay the upgrade invoice
			var invoice = await PayInvoiceHandlingCardErrorsAsync(invoiceId: stripeSubscription.LatestInvoice.Id);
			if (invoice == null) {
				return Problem("Failed to pay invoice.", statusCode: 500);
			}
			// process the payment and return
			var invoiceProcessResult = await ProcessInvoicePaymentAttemptAsync(invoice);
			if (!invoiceProcessResult.IsSuccessful) {
				return Problem("Failed to update payment status.", statusCode: 500);
			}
			return await CreateStripePaymentResponseActionResultAsync(invoice.PaymentIntent);
		}

		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> StripeWebhook() {
			// read the body text
			string body;
			using (
				var bodyReader = new StreamReader(Request.Body)
			) {
				body = await bodyReader.ReadToEndAsync();
			}
			// parse the stripe event
			Stripe.Event stripeEvent;
			try {
				stripeEvent = Stripe.EventUtility.ConstructEvent(body, Request.Headers["Stripe-Signature"], subscriptionsOptions.StripeWebhookSigningSecret);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to parse Stripe webhook request body with content: {Body}.", body);
				return BadRequest();
			}
			// handle the stripe event as a background task so we can return immediately and keep the stripe servers happy
			taskQueue.QueueBackgroundWorkItem(
				async cancellationToken => {
					switch (stripeEvent.Type) {
						// finalize and pay renewal invoices immediately in order to avoid 1-hour delay
						case Stripe.Events.InvoiceCreated:
							var newInvoice = stripeEvent.Data.Object as Stripe.Invoice;
							if (newInvoice.BillingReason == StripeInvoiceBillingReason.SubscriptionCycle) {
								// the invoice must be finalized first
								// method throws if invoice has already been finalized
								try {
									await new Stripe.InvoiceService()
										.FinalizeInvoiceAsync(id: newInvoice.Id);
								} catch (Exception ex) {
									logger.LogError(ex, "Failed to finalize Stripe subscription renewal invoice with id: {InvoiceId}.", newInvoice.Id);
								}
								// then attempt payment and create the new period using the payment status
								newInvoice = await PayInvoiceHandlingCardErrorsAsync(invoiceId: newInvoice.Id);
								if (newInvoice != null) {
									await ProcessInvoicePaymentAttemptAsync(newInvoice);
								}
							}
							break;
						// The following three payment events should generally be handled during the normal initial and renewal payment processing flows but there are
						// situations where the client may have failed to complete a request after payment confirmation or some other unexpected server error occurred
						// so we should handle them here as well as a backup. The invoice update database function is designed to be called any number of times in any order.
						case Stripe.Events.InvoicePaid:
						case Stripe.Events.InvoicePaymentActionRequired:
						case Stripe.Events.InvoicePaymentFailed:
							var updatedInvoice = stripeEvent.Data.Object as Stripe.Invoice;
							// The PaymentIntent is required to update the payment status and is not included with the webhook object so we need to fetch a new invoice.
							await ProcessInvoicePaymentAttemptAsync(
								await GetInvoiceWithPaymentIntentAsync(invoiceId: updatedInvoice.Id)
							);
							break;
						// If a charge is disputed or refunded we need to cancel the subscription and update the associated subscription period.
						case Stripe.Events.ChargeDisputeCreated:
						case Stripe.Events.ChargeRefunded:
							// Get the invoiceId and refund params.
							DateTime dateRefunded;
							string
								refundReason,
								invoiceId;
							switch (stripeEvent.Type) {
								case Stripe.Events.ChargeDisputeCreated:
									var dispute = stripeEvent.Data.Object as Stripe.Dispute;
									dateRefunded = dispute.Created;
									refundReason = dispute.Reason ?? "charge.dispute.created";
									try {
										var disputedPaymentIntent = await new Stripe.PaymentIntentService()
											.GetAsync(
												id: dispute.PaymentIntentId
											);
										invoiceId = disputedPaymentIntent.InvoiceId;
									} catch (Exception ex) {
										logger.LogError(ex, "Failed to retrieve PaymentIntent with id: {PaymentIntentId} for {StripeEventType} event processing.", dispute.PaymentIntentId, stripeEvent.Type);
										invoiceId = null;
									}
									break;
								case Stripe.Events.ChargeRefunded:
									var refundedCharge = stripeEvent.Data.Object as Stripe.Charge;
									dateRefunded = refundedCharge.Created;
									refundReason = refundedCharge.Refunds?.SingleOrDefault()?.Reason ?? "charge.refunded";
									invoiceId = refundedCharge.InvoiceId;
									break;
								default:
									throw new Exception($"Unexpected event type: {stripeEvent.Type}.");
							}
							if (invoiceId == null) {
								break;
							}
							// Retrieve the period.
							string subscriptionId;
							using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
								var refundedPeriod = await db.GetSubscriptionPeriodAsync(
									provider: SubscriptionProvider.Stripe,
									providerPeriodId: invoiceId
								);
								if (refundedPeriod == null) {
									logger.LogError("Failed to retrieve SubscriptionPeriod with id: {PeriodId} for {StripeEventType} event processing.", invoiceId, stripeEvent.Type);
									break;
								}
								subscriptionId = refundedPeriod.ProviderSubscriptionId;
								// Update the period if not yet refunded.
								try {
									await db.CreateOrUpdateSubscriptionPeriodAsync(
										provider: SubscriptionProvider.Stripe,
										providerSubscriptionId: refundedPeriod.ProviderSubscriptionId,
										providerPeriodId: refundedPeriod.ProviderPeriodId,
										providerPriceId: refundedPeriod.ProviderPriceId,
										providerPaymentMethodId: refundedPeriod.ProviderPaymentMethodId,
										beginDate: refundedPeriod.BeginDate,
										endDate: refundedPeriod.EndDate,
										dateCreated: refundedPeriod.DateCreated,
										paymentStatus: refundedPeriod.PaymentStatus,
										datePaid: refundedPeriod.DatePaid,
										dateRefunded: dateRefunded,
										refundReason: refundReason,
										prorationDiscount: null
									);
								} catch (Exception ex) {
									logger.LogError(ex, "Failed to update SubscriptionPeriod with id: {PeriodId} for {StripeEventType} event processing.", invoiceId, stripeEvent.Type);
								}
							}
							// Cancel the subscription.
							await CancelSubscriptionAsync(
								providerSubscriptionId: subscriptionId
							);
							break;
						// Update payment method if we get a notification from the network via Stripe.
						case Stripe.Events.PaymentMethodAutomaticallyUpdated:
							await UpdatePaymentMethodFromStripeAsync(stripeEvent.Data.Object as Stripe.PaymentMethod, SubscriptionEventSource.ProviderNotification);
							break;
					}
				}
			);
			// return
			return Ok();
		}
	}
}