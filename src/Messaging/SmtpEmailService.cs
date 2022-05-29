// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using System.Threading.Tasks;
using api.Configuration;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Mvc.RenderViewToString;
using api.BackgroundProcessing;
using System;

namespace api.Messaging {
	public class SmtpEmailService: EmailService {
		private readonly SmtpServerOptions smtpOptions;
		public SmtpEmailService(
			IOptions<DatabaseOptions> dbOpts,
			RazorViewToStringRenderer viewRenderer,
			IOptions<EmailOptions> emailOpts,
			IOptions<ServiceEndpointsOptions> serviceOpts,
			IOptions<TokenizationOptions> tokenizationOptions,
			IBackgroundTaskQueue taskQueue
		) : base(
			dbOpts,
			viewRenderer,
			emailOpts,
			serviceOpts,
			tokenizationOptions,
			taskQueue
		) {
			smtpOptions = emailOpts.Value.SmtpServer;
		}
		public override async Task Send(params EmailMessage[] messages) {
			if (
				String.IsNullOrWhiteSpace(smtpOptions.Host) ||
				smtpOptions.Port == 0
			) {
				return;
			}
			using (var client = new SmtpClient()) {
				await client.ConnectAsync(smtpOptions.Host, smtpOptions.Port);
				foreach (var message in messages) {
					var mimeMessage = new MimeMessage(
						from: new[] { new MailboxAddress(message.From.Name, message.From.Address) },
						to: new [] { new MailboxAddress(message.To.Name, message.To.Address) },
						subject: message.Subject,
						body: new TextPart(TextFormat.Html) {
							Text = message.Body
						}
					);
					if (message.ReplyTo != null) {
						mimeMessage.ReplyTo.Add(new MailboxAddress(message.ReplyTo.Name, message.ReplyTo.Address));
					}
					await client.SendAsync(mimeMessage);
				}
				await client.DisconnectAsync(quit: true);
			}
		}
	}
}