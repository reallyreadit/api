using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using api.Authentication;
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

		public SubscriptionsController(
			IOptions<DatabaseOptions> databaseOptions,
			IHttpClientFactory httpClientFactory,
			ILogger<SubscriptionsController> logger,
			IOptions<SubscriptionsOptions> subscriptionsOptions
		) {
			this.databaseOptions = databaseOptions.Value;
			this.httpClientFactory = httpClientFactory;
			this.logger = logger;
			this.subscriptionsOptions = subscriptionsOptions.Value;
		}

		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> AppStoreNotification() {
			var timestamp = DateTime.UtcNow.ToString("s");
			using (
				var body = new StreamReader(Request.Body)
			) {
				await System.IO.File.WriteAllTextAsync(
					path: $@"logs/{timestamp.Replace(':', '-')}_AppStoreNotification_{Path.GetRandomFileName()}",
					contents: "Date: " + DateTime.UtcNow.ToString("s") + "\nContent-Type: " + Request.ContentType + "\nBody:\n" + (await body.ReadToEndAsync())
				);
			}
			return Ok();
		}

		// must allow anonymous access since this can be called by the transaction observer after a user signs out
		//[AllowAnonymous]
		[HttpPost]
		public async Task<ActionResult<AppleSubscriptionValidationResponse>> AppleSubscriptionValidation(
			[FromBody] AppleSubscriptionValidationRequest request
		) {
			// debugging
			var timestamp = DateTime.UtcNow.ToString("s");
			await System.IO.File.WriteAllTextAsync(
				path: $@"logs/{timestamp.Replace(':', '-')}_AppleSubscriptionValidation_{Path.GetRandomFileName()}",
				contents: request.Base64EncodedReceipt
			);
			// if (!User.Identity.IsAuthenticated) {
			// 	return BadRequest();
			// }

			// verify receipt with app store
			AppStoreReceiptVerificationResponse response;
			using (var httpClient = this.httpClientFactory.CreateClient()) {
				var appStoreResponse = await httpClient.PostAsync(
					requestUri: subscriptionsOptions.AppStoreSandboxUrl,
					new StringContent(
						content: JsonSerializer.Serialize(
							new AppStoreReceiptVerificationRequest {
								ReceiptData = request.Base64EncodedReceipt,
								Password = subscriptionsOptions.AppleAppSecret,
								ExcludeOldTransactions = false
							}
						),
						encoding: Encoding.UTF8,
						mediaType: "application/json"
					)
				);
				response = JsonSerializer.Deserialize<AppStoreReceiptVerificationResponse>(
					await appStoreResponse.Content.ReadAsStringAsync()
				);
			}

			if (response.LatestReceiptInfo == null || !response.LatestReceiptInfo.Any()) {
				return new AppleSubscriptionEmptyReceiptResponse();
			}

			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				// sync to database
				var userAccountId = User.GetUserAccountId();
				var accountGroups = response.LatestReceiptInfo.GroupBy(
					transaction => transaction.OriginalTransactionId
				);
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
						userAccountId: userAccountId,
						dateCreated: originalPurchaseDate
					);
					await db.CreateOrUpdateSubscriptionAsync(
						provider: SubscriptionProvider.Apple,
						providerSubscriptionId: originalTransaction.TransactionId,
						providerAccountId: originalTransaction.TransactionId,
						dateCreated: originalPurchaseDate,
						dateTerminated: null,
						latestReceipt: response.LatestReceipt
					);
					foreach (var transaction in accountGroup) {
						var purchaseDate = DateTimeOffset
							.FromUnixTimeMilliseconds(
								Int64.Parse(transaction.PurchaseDateMs)
							)
							.UtcDateTime;
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
							dateRefunded: transaction.CancellationDateMs != null ?
								new Nullable<DateTime>(
									DateTimeOffset
										.FromUnixTimeMilliseconds(
											Int64.Parse(transaction.CancellationDateMs)
										)
										.UtcDateTime
								 ) :
								null,
							refundReason: transaction.CancellationReason
						);
					}
				}

				// return current status
				var latestTransaction = response.LatestReceiptInfo
					.OrderByDescending(
						transaction => transaction.ExpiresDateMs
					)
					.First();
				var subscriptionStatus = await db.GetSubscriptionStatusForSubscriptionAccountAsync(
					provider: SubscriptionProvider.Apple,
					providerAccountId: latestTransaction.OriginalTransactionId
				);
				var subscriptionUser = await db.GetUserAccountById(subscriptionStatus.UserAccountId);
				if (subscriptionUser.Id == userAccountId) {
					return new AppleSubscriptionAssociatedWithCurrentUserResponse(
						SubscriptionStatusClientModel.FromSubscriptionStatus(
							user: subscriptionUser,
							status: subscriptionStatus
						)
					);
				} else {
					return new AppleSubscriptionAssociatedWithAnotherUserResponse(
						subscribedUsername: subscriptionUser.Name
					);
				}
			}
		}

		// must allow anonymous access since this can be called by the transaction observer after a user signs out
		[AllowAnonymous]
		[HttpPost]
		public IActionResult AppleSubscriptionPurchaseFailure(
			[FromBody] AppleSubscriptionPurchaseFailureRequest request
		) {
			this.logger.LogInformation("Received Apple subscription purchase failure with code: {Code} and description: {Description}", request.Code, request.Description);
			if (!User.Identity.IsAuthenticated) {
				return BadRequest();
			}
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
		public async Task<ActionResult<StripePaymentResponse>> StripePaymentConfirmation(
			[FromBody] StripePaymentConfirmationRequest request
		) {
			var userAccountId = User.GetUserAccountId();
			Stripe.Invoice invoice;
			try {
				invoice = await new Stripe.InvoiceService()
					.GetAsync(
						id: request.InvoiceId,
						options: new Stripe.InvoiceGetOptions {
							Expand = new List<string> {
								StripeInvoiceExpandProperties.PaymentIntent
							}
						}
					);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to get payment intent with id: {PaymentIntentId} for confirmation for user with id: {UserId}.", request.InvoiceId, userAccountId);
				return Problem("Subscription not found.", statusCode: 500);
			}
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				try {
					await db.UpdateSubscriptionPeriodPaymentStatusAsync(
						provider: SubscriptionProvider.Stripe,
						providerPeriodId: invoice.Id,
						providerPaymentMethodId: invoice.PaymentIntent.PaymentMethodId,
						paymentStatus: SubscriptionPaymentStatusExtensions.FromStripePaymentIntentStatusString(invoice.PaymentIntent.Status),
						datePaid: invoice.StatusTransitions.PaidAt
					);
				} catch (Exception ex) {
					logger.LogError(ex, "Failed to update payment status for invoice with id: {InvoiceId}. Stripe request id: {RequestId}.", invoice.Id, invoice.StripeResponse.RequestId);
					return Problem("Failed to update payment status.", statusCode: 500);
				}
				try {
					return StripePaymentResponse.FromPaymentIntent(
						invoice.PaymentIntent,
						SubscriptionStatusClientModel.FromSubscriptionStatus(
							await db.GetUserAccountById(userAccountId),
							await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccountId)
						)
					);
				} catch (Exception ex) {
					logger.LogError(ex, "Failed to create payment confirmation response object for invoice with id: {InvoiceId}. Stripe request id: {RequestId}.", invoice.Id, invoice.StripeResponse.RequestId);
					return Problem($"Failed to create response object.", statusCode: 500);
				}
			}
		}

		[HttpPost]
		public async Task<ActionResult<StripePaymentResponse>> StripeSubscription(
			[FromBody] StripeSubscriptionCreationRequest request
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

			// attach stripe payment method to stripe customer (successful no-op if already attached and we need the payment method object either way)
			Stripe.PaymentMethod stripePaymentMethod;
			try {
				stripePaymentMethod = await new Stripe.PaymentMethodService()
					.AttachAsync(
						request.PaymentMethodId,
						new Stripe.PaymentMethodAttachOptions {
							Customer = subscriptionAccount.ProviderAccountId
						}
					);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to attach Stripe payment method with id: {PaymentMethodId} to Stripe customer with id: {CustomerId}.", request.PaymentMethodId, subscriptionAccount.ProviderAccountId);
				return Problem("Failed to attach payment method.", statusCode: 500);
			}

			// this should never fail but we should check anyway since values are longs for some reason
			int
				stripePaymentMethodExpirationMonth,
				stripePaymentMethodExpirationYear;
			try {
				stripePaymentMethodExpirationMonth = Convert.ToInt32(stripePaymentMethod.Card.ExpMonth);
				stripePaymentMethodExpirationYear = Convert.ToInt32(stripePaymentMethod.Card.ExpYear);
			} catch (Exception ex) {
				logger.LogError(ex, "Invalid value for card expiration month: {ExpirationMonth} or year: {ExpirationYear} on Stripe payment method with id: {PaymentMethodId}. Stripe request id: {RequestId}.", stripePaymentMethod.Card.ExpMonth, stripePaymentMethod.Card.ExpYear, stripePaymentMethod.Id, stripePaymentMethod.StripeResponse.RequestId);
				return Problem("Invalid card expiration date.", statusCode: 500);
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
							providerAccountId: subscriptionAccount.ProviderAccountId,
							dateCreated: stripePaymentMethod.Created,
							wallet: cardWallet,
							brand: cardBrand,
							lastFourDigits: stripePaymentMethod.Card.Last4,
							country: stripePaymentMethod.Card.Country,
							expirationMonth: stripePaymentMethodExpirationMonth,
							expirationYear: stripePaymentMethodExpirationYear
						);
					} catch (Exception ex) {
						logger.LogError(ex, "Failed to create payment method for Stripe payment method with id: {PaymentMethodId}. Stripe request id: {RequestId}.", stripePaymentMethod.Id, stripePaymentMethod.StripeResponse.RequestId);
						return Problem("Failed to create payment method.", statusCode: 500);
					}
				}
			} else if (
				paymentMethod.ExpirationMonth != stripePaymentMethodExpirationMonth ||
				paymentMethod.ExpirationYear != stripePaymentMethodExpirationYear
			) {
				// update the payment method
				using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
					try {
						paymentMethod = await db.UpdateSubscriptionPaymentMethodAsync(
							provider: SubscriptionProvider.Stripe,
							providerPaymentMethodId: stripePaymentMethod.Id,
							eventSource: SubscriptionEventSource.UserAction,
							expirationMonth: stripePaymentMethodExpirationMonth,
							expirationYear: stripePaymentMethodExpirationYear
						);
					} catch (Exception ex) {
						logger.LogError(ex, "Failed to update payment method with id: {PaymentMethodId}. Stripe request id: {RequestId}.", stripePaymentMethod.Id, stripePaymentMethod.StripeResponse.RequestId);
						return Problem("Failed to update payment method.", statusCode: 500);
					}
				}
			}

			// update stripe customer's default invoice payment method (successful no-op if already default)
			try {
				await new Stripe.CustomerService()
					.UpdateAsync(
						subscriptionAccount.ProviderAccountId,
						new Stripe.CustomerUpdateOptions() {
							InvoiceSettings = new Stripe.CustomerInvoiceSettingsOptions {
								DefaultPaymentMethod = stripePaymentMethod.Id
							}
						}
					);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to set Stripe payment method with id: {PaymentMethodId} as default for Stripe customer with id: {CustomerId}.", stripePaymentMethod.Id, subscriptionAccount.ProviderAccountId);
				return Problem("Failed to set default Stripe payment method.", statusCode: 500);
			}

			// set the readup default payment method (successful no-op if already default)
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				try {
					await db.AssignDefaultSubscriptionPaymentMethod(
						provider: SubscriptionProvider.Stripe,
						providerAccountId: subscriptionAccount.ProviderAccountId,
						providerPaymentMethodId: stripePaymentMethod.Id
					);
				} catch (Exception ex) {
					logger.LogError(ex, "Failed to set payment method with id: {PaymentMethodId} as default for subscription account with id: {CustomerId}.", stripePaymentMethod.Id, subscriptionAccount.ProviderAccountId);
					return Problem("Failed to set default payment method.", statusCode: 500);
				}
			}

			// resolve the price from the request
			string priceId;
			// check for a price level or custom amount
			if (
				!String.IsNullOrWhiteSpace(request.PriceLevelId)
			) {
				// use the price level id
				priceId = request.PriceLevelId;
			} else {
				// verify the custom amount
				if (request.CustomPriceAmount < 2500 || request.CustomPriceAmount > 100000) {
					logger.LogError("Subscription custom price out of range: {Price}.", request.CustomPriceAmount);
					return Problem("Invalid custom price.", statusCode: 500);
				}
				// check for an existing price that matches the custom amount
				SubscriptionPrice customPrice;
				using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
					customPrice = await db.GetCustomSubscriptionPriceForProviderAsync(
						provider: SubscriptionProvider.Stripe,
						amount: request.CustomPriceAmount
					);
				}
				if (customPrice != null) {
					// use the existing price
					priceId = customPrice.ProviderPriceId;
				} else {
					// create a new stripe price
					Stripe.Price stripeCustomPrice;
					try {
						stripeCustomPrice = await new Stripe.PriceService()
							.CreateAsync(
								new Stripe.PriceCreateOptions {
									Currency = "usd",
									UnitAmount = request.CustomPriceAmount,
									Product = subscriptionsOptions.StripeSubscriptionProductId,
									Recurring = new Stripe.PriceRecurringOptions {
										Interval = "month"
									}
								}
							);
					} catch (Exception ex) {
						logger.LogError(ex, "Failed to create new Stripe price with amount: {Amount}.", request.CustomPriceAmount);
						return Problem("Failed to create custom price.", statusCode: 500);
					}
					// create a new custom price (returns existing price if one already exists)
					using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
						try {
							customPrice = await db.CreateCustomSubscriptionPriceAsync(
								provider: SubscriptionProvider.Stripe,
								providerPriceId: stripeCustomPrice.Id,
								dateCreated: stripeCustomPrice.Created,
								amount: (int)stripeCustomPrice.UnitAmount.Value
							);
						} catch (Exception ex) {
							logger.LogError(ex, "Failed to create price with id: {PriceId}. Stripe request id: {RequestId}.", stripeCustomPrice.Id, stripeCustomPrice.StripeResponse.RequestId);
							return Problem("Failed to store custom price.", statusCode: 500);
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
					// assign the price id from the custom price
					priceId = customPrice.ProviderPriceId;
				}
			}

			// first check for an unexpired, incomplete subscription with a matching price and attempt to pay it if found
			SubscriptionStatus matchingIncompleteSubscription;
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				matchingIncompleteSubscription = (await db.GetSubscriptionStatusesForUserAccountAsync(userAccount.Id))
					.SingleOrDefault(
						subscriptionStatus =>
							subscriptionStatus.GetCurrentState(DateTime.UtcNow) == SubscriptionState.Incomplete &&
							subscriptionStatus.LatestPeriod.ProviderPriceId == priceId
					);
			}
			Stripe.Subscription stripeSubscription;
			if (matchingIncompleteSubscription != null) {
				// attempt to get the subscription from stripe
				try {
					stripeSubscription = await new Stripe.SubscriptionService()
						.GetAsync(
							id: matchingIncompleteSubscription.ProviderSubscriptionId,
							options: new Stripe.SubscriptionGetOptions {
								Expand = new List<string> {
									StripeSubscriptionExpandProperties.LatestInvoicePaymentIntent
								}
							}
						);
				} catch (Exception ex) {
					logger.LogError(ex, "Failed to get Stripe subscription with id: {SubscriptionId}.", matchingIncompleteSubscription.ProviderSubscriptionId);
					return Problem("Failed to verify existing subscription status.", statusCode: 500);
				}
				// only process if incomplete, otherwise continue to subscription creation
				if (stripeSubscription.Status == api.Subscriptions.StripeSubscriptionStatus.Incomplete) {
					// attempt to pay the existing invoice
					Stripe.Invoice invoice;
					try {
						invoice = await new Stripe.InvoiceService()
							.PayAsync(
								id: stripeSubscription.LatestInvoiceId,
								options: new Stripe.InvoicePayOptions {
									Expand = new List<string> {
										StripeInvoiceExpandProperties.PaymentIntent
									}
								}
							);
					} catch (Exception invoicePayEx) {
						if ((invoicePayEx as Stripe.StripeException)?.StripeError.Type == StripeErrorType.CardError) {
							// refresh the invoice and process the response based on the payment intent status
							try {
								invoice = await new Stripe.InvoiceService()
									.GetAsync(
										id: stripeSubscription.LatestInvoiceId,
										options: new Stripe.InvoiceGetOptions {
											Expand = new List<string> {
												StripeInvoiceExpandProperties.PaymentIntent
											}
										}
									);
							} catch (Exception invoiceGetEx) {
								logger.LogError(invoiceGetEx, "Failed to get Stripe invoice with id: {InvoiceId}.", stripeSubscription.LatestInvoiceId);
								return Problem("Failed to get invoice.", statusCode: 500);
							}
						} else {
							logger.LogError(invoicePayEx, "Failed to pay Stripe invoice with id: {InvoiceId}.", stripeSubscription.LatestInvoiceId);
							return Problem("Failed to pay invoice.", statusCode: 500);
						}
					}
					// update the period payment status and return
					using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
						try {
							await db.UpdateSubscriptionPeriodPaymentStatusAsync(
								provider: SubscriptionProvider.Stripe,
								providerPeriodId: invoice.Id,
								providerPaymentMethodId: invoice.PaymentIntent.PaymentMethodId,
								paymentStatus: SubscriptionPaymentStatusExtensions.FromStripePaymentIntentStatusString(invoice.PaymentIntent.Status),
								datePaid: invoice.StatusTransitions.PaidAt
							);
						} catch (Exception ex) {
							logger.LogError(ex, "Failed to update payment status for invoice with id: {InvoiceId}. Stripe request id: {RequestId}.", invoice.Id, invoice.StripeResponse.RequestId);
							return Problem("Failed to update payment status.", statusCode: 500);
						}
						try {
							return StripePaymentResponse.FromPaymentIntent(
								invoice.PaymentIntent,
								SubscriptionStatusClientModel.FromSubscriptionStatus(
									userAccount,
									await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccount.Id)
								)
							);
						} catch (Exception ex) {
							logger.LogError(ex, "Failed to create payment confirmation response object for invoice with id: {InvoiceId}. Stripe request id: {RequestId}.", invoice.Id, invoice.StripeResponse.RequestId);
							return Problem($"Failed to create response object.", statusCode: 500);
						}
					}
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
									Price = priceId
								}
							},
							Expand = new List<string> {
								StripeSubscriptionExpandProperties.LatestInvoicePaymentIntent
							}
						}
					);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to create Stripe subscription with price id: {PriceId} for customer with id: {CustomerId}.", priceId, subscriptionAccount.ProviderAccountId);
				return Problem("Failed to create Stripe subscription.", statusCode: 500);
			}

			// there should only be a single line item
			var invoiceLineItem = stripeSubscription.LatestInvoice.Lines.SingleOrDefault();
			if (invoiceLineItem == null) {
				logger.LogError("Unexpected number of line items on Stripe invoice with id: {InvoiceId}. Stripe request id: {RequestId}.", stripeSubscription.LatestInvoice.Id, stripeSubscription.StripeResponse.RequestId);
				return Problem("Invalid invoice line item.", statusCode: 500);
			}

			// create the subscription
			using (var db = new NpgsqlConnection(databaseOptions.ConnectionString)) {
				try {
					await db.CreateOrUpdateSubscriptionAsync(
						provider: SubscriptionProvider.Stripe,
						providerSubscriptionId: stripeSubscription.Id,
						providerAccountId: stripeSubscription.CustomerId,
						dateCreated: stripeSubscription.Created,
						dateTerminated: null,
						latestReceipt: null
					);
					await db.CreateOrUpdateSubscriptionPeriodAsync(
						provider: SubscriptionProvider.Stripe,
						providerSubscriptionId: stripeSubscription.Id,
						providerPeriodId: stripeSubscription.LatestInvoiceId,
						providerPriceId: invoiceLineItem.Price.Id,
						providerPaymentMethodId: stripeSubscription.LatestInvoice.PaymentIntent.PaymentMethodId,
						beginDate: DateTimeOffset
							.FromUnixTimeSeconds(invoiceLineItem.Period.Start)
							.UtcDateTime,
						endDate: DateTimeOffset
							.FromUnixTimeSeconds(invoiceLineItem.Period.End)
							.UtcDateTime,
						dateCreated: stripeSubscription.Created,
						paymentStatus: SubscriptionPaymentStatusExtensions.FromStripePaymentIntentStatusString(stripeSubscription.LatestInvoice.PaymentIntent.Status),
						datePaid: stripeSubscription.LatestInvoice.StatusTransitions.PaidAt,
						dateRefunded: null,
						refundReason: null
					);
					try {
						return StripePaymentResponse.FromPaymentIntent(
							stripeSubscription.LatestInvoice.PaymentIntent,
							SubscriptionStatusClientModel.FromSubscriptionStatus(
								userAccount,
								await db.GetCurrentSubscriptionStatusForUserAccountAsync(userAccount.Id)
							)
						);
					} catch (Exception ex) {
						logger.LogError(ex, "Failed to create payment confirmation response object for invoice with id: {InvoiceId}. Stripe request id: {RequestId}.", stripeSubscription.LatestInvoiceId, stripeSubscription.StripeResponse.RequestId);
						return Problem($"Failed to create response object.", statusCode: 500);
					}
				} catch (Exception ex) {
					logger.LogError(ex, "Failed to create subscription for Stripe subscription with id: {SubscriptionId}. Stripe request id: {RequestId}.", stripeSubscription.Id, stripeSubscription.StripeResponse.RequestId);
					return Problem("Failed to create subscription.", statusCode: 500);
				}
			}
		}
	}
}