// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using System;
using api.Formatting;

namespace api.Messaging {
	public class EmailNotification<TContent> {
		public EmailNotification(
			long userId,
			EmailMailbox to,
			string subject,
			Uri openUrl,
			TContent content
		) {
			UserId = userId;
			To = to;
			Subject = subject.RemoveControlCharacters();
			OpenUrl = openUrl;
			Content = content;
		}
		public EmailNotification(
			long userId,
			EmailMailbox replyTo,
			EmailMailbox to,
			string subject,
			Uri openUrl,
			TContent content
		) : this(
			userId,
			to,
			subject,
			openUrl,
			content
		) {
			ReplyTo = replyTo;
		}
		public long UserId { get; }
		public EmailMailbox ReplyTo { get; }
		public EmailMailbox To { get; }
		public string Subject { get; }
		public Uri OpenUrl { get; }
		public TContent Content { get; }
		public string Subscription { get; }
	}
}