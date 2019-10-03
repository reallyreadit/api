using System;
using System.Collections.Generic;
using api.DataAccess.Models;
using api.Encryption;

namespace api.Notifications {
	public class NotificationToken {
		private const char tokenStringSeparator = ',';
		public NotificationToken(
			long receiptId,
			NotificationChannel? channel = null,
			NotificationAction? action = null,
			ViewActionResource? viewActionResource = null,
			long? viewActionResourceId = null
		) {
			ReceiptId = receiptId;
			Channel = channel;
			Action = action;
			ViewActionResource = viewActionResource;
			ViewActionResourceId = viewActionResourceId;
		}
		public NotificationToken(
			string tokenString,
			string key
		) {
			var tokenParts = StringEncryption
				.Decrypt(
					text: UrlSafeBase64.Decode(
						urlSafeBase64String: tokenString
					),
					key: key
				)
				.Split(
					separator: tokenStringSeparator
				);
			ReceiptId = Int64.Parse(tokenParts[0]);
			if (tokenParts.Length >= 2 && tokenParts[1].Length > 0) {
				Channel = (NotificationChannel)Int32.Parse(tokenParts[1]);
			}
			if (tokenParts.Length >= 3 && tokenParts[2].Length > 0) {
				Action = (NotificationAction)Int32.Parse(tokenParts[2]);
			}
			if (tokenParts.Length >= 4 && tokenParts[3].Length > 0) {
				ViewActionResource = (ViewActionResource)Int32.Parse(tokenParts[3]);
			}
			if (tokenParts.Length >= 5 && tokenParts[4].Length > 0) {
				ViewActionResourceId = Int64.Parse(tokenParts[4]);
			}
		}
		public long ReceiptId { get; }
		public NotificationChannel? Channel { get; }
		public NotificationAction? Action { get; }
		public ViewActionResource? ViewActionResource { get; }
		public long? ViewActionResourceId { get; }
		public string CreateTokenString(
			string key
		) {
			var values = new List<string>() {
				ReceiptId.ToString()
			};
			if (Channel.HasValue) {
				values.Add(((int)Channel.Value).ToString());
			}
			if (Action.HasValue) {
				values.Add(((int)Action.Value).ToString());
			}
			if (ViewActionResource.HasValue) {
				values.Add(((int)ViewActionResource.Value).ToString());
			}
			if (ViewActionResourceId.HasValue) {
				values.Add(ViewActionResourceId.Value.ToString());
			}
			return UrlSafeBase64.Encode(
				base64String: StringEncryption.Encrypt(
					text: String.Join(
						separator: tokenStringSeparator.ToString(),
						values: values
					),
					key: key
				)
			);
		}
	}
}