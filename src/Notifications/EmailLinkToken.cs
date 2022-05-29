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