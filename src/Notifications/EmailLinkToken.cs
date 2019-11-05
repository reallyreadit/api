using System;
using api.Encryption;

namespace api.Notifications {
	public class EmailLinkToken {
		private const char tokenStringSeparator = ',';
		public EmailLinkToken(
			long receiptId,
			EmailLinkResource resource,
			long resourceId
		) {
			ReceiptId = receiptId;
			Resource = resource;
			ResourceId = resourceId;
		}
		public EmailLinkToken(
			string tokenString,
			string key
		) {
			var tokenParts = StringEncryption
				.Decrypt(
					text: UrlSafeBase64.Decode(
						tokenString
					),
					key: key
				)
				.Split(
					separator: tokenStringSeparator
				);
			ReceiptId = Int64.Parse(tokenParts[0]);
			Resource = (EmailLinkResource)Int64.Parse(tokenParts[1]);
			ResourceId = Int64.Parse(tokenParts[2]);
		}
		public long ReceiptId { get; }
		public EmailLinkResource Resource { get; }
		public long ResourceId { get; }
		public string CreateTokenString(
			string key
		) {
			return UrlSafeBase64.Encode(
				StringEncryption.Encrypt(
					text: String.Join(
						separator: tokenStringSeparator.ToString(),
						ReceiptId.ToString(),
						((int)Resource).ToString(),
						ResourceId.ToString()
					),
					key: key
				)
			);
		}
	}
}