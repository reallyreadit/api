using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using api.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvc.RenderViewToString;
using api.BackgroundProcessing;
using System;
using System.Linq;

namespace api.Messaging {
	public class AmazonSesEmailService: EmailService {
		private readonly RegionEndpoint regionEndpoint;
		private readonly ILogger<AmazonSesEmailService> logger;
		public AmazonSesEmailService(
			IOptions<DatabaseOptions> dbOpts,
			RazorViewToStringRenderer viewRenderer,
			IOptions<EmailOptions> emailOpts,
			IOptions<ServiceEndpointsOptions> serviceOpts,
			IOptions<TokenizationOptions> tokenizationOptions,
			ILogger<AmazonSesEmailService> logger,
			IBackgroundTaskQueue taskQueue
		) : base(
			dbOpts,
			viewRenderer,
			emailOpts,
			serviceOpts,
			tokenizationOptions,
			taskQueue
		) {
			regionEndpoint = RegionEndpoint.GetBySystemName(emailOpts.Value.AmazonSesRegionEndpoint);
			this.logger = logger;
		}
		public override async Task Send(params EmailMessage[] messages) {
			using (var client = new AmazonSimpleEmailServiceClient(regionEndpoint)) {
				foreach (var message in messages.Where(m => !m.To.Address.Contains(","))) {
					var request = new SendEmailRequest(
						source: $"{message.From.Name} <{message.From.Address}>",
						destination: new Destination(new List<string>() { $"{message.To.Name} <{message.To.Address}>" }),
						message: new Message(
							subject: new Content(message.Subject),
							body: new Body() {
								Html = new Content(message.Body)
							}
						)
					);
					if (message.ReplyTo != null) {
						request.ReplyToAddresses.Add($"{message.ReplyTo.Name} <{message.ReplyTo.Address}>");
					}
					// mail link debugging (this is the only version that has worked to prevent link errors so far)
					if (message.Subject.StartsWith("AOTD:")) {
						logger.LogError(
							"{Subject}\nTo: {ToAddress}\nBody: {Body}",
							message.Subject,
							message.To.Address,
							message.Body
						);
					}
					// SendEmailAsync will throw an exception if the email address contains illegal characters
					try {
						var response = await client.SendEmailAsync(request);
						if (response.HttpStatusCode != HttpStatusCode.OK) {
							logger.LogError(
								"Ses dispatch failed. Status code: {StatusCode} Ses response: {SesResponse} Sent to: {ToAddress}",
								response.HttpStatusCode,
								response.ResponseMetadata?.Metadata,
								message.To.Address
							);
						}
					} catch (Exception ex) {
						logger.LogError(
							ex,
							"Ses dispatch thew exception. Sent to: {ToAddress}",
							message.To.Address
						);
					}
				}
			}
		}
	}
}