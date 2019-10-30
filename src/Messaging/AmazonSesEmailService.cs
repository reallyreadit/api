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
			ILogger<AmazonSesEmailService> logger
		) : base(
			dbOpts,
			viewRenderer,
			emailOpts,
			serviceOpts,
			tokenizationOptions
		) {
			regionEndpoint = RegionEndpoint.GetBySystemName(emailOpts.Value.AmazonSesRegionEndpoint);
			this.logger = logger;
		}
		protected override async Task Send(params EmailMessage[] messages) {
			using (var client = new AmazonSimpleEmailServiceClient(regionEndpoint)) {
				foreach (var message in messages) {
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
					var response = await client.SendEmailAsync(request);
					if (response.HttpStatusCode != HttpStatusCode.OK) {
						logger.LogError(
							"Ses dispatch failed. Status code: {StatusCode} Ses response: {SesResponse}",
							response.HttpStatusCode,
							response.ResponseMetadata?.Metadata
						);
					}
				}
			}
		}
	}
}