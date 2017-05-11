using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace api.Messaging {
	public static class AmazonSesEmailService {
		public static async Task SendEmail(EmailAddress from, EmailAddress to, string subject, string body, string regionEndpoint) {
			var bodyContent = new Body();
			bodyContent.Html = new Content(body);
			using (var client = new AmazonSimpleEmailServiceClient(RegionEndpoint.GetBySystemName(regionEndpoint))) {
				await client.SendEmailAsync(new SendEmailRequest(
					source: $"{from.Name} <{from.Address}>",
					destination: new Destination(new List<string>() { $"{to.Name} <{to.Address}>" }),
					message: new Message(new Content(subject), bodyContent)
				));
			}
		}
	}
}