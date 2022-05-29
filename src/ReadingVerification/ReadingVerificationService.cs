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
using System.Net;
using api.Configuration;
using api.DataAccess.Models;
using api.Encryption;
using Microsoft.Extensions.Options;

namespace api.ReadingVerification {
	public class ReadingVerificationService {
		private ReadingVerificationOptions readingVerificationOpts;
		public ReadingVerificationService(IOptions<ReadingVerificationOptions> readingVerificationOpts) {
			this.readingVerificationOpts = readingVerificationOpts.Value;
		}
		public Article AssignProofToken(Article article, long userAccountId) {
			if (article.IsRead) {
				article.ProofToken = UrlSafeBase64.Encode(
					StringEncryption.Encrypt(
						text: article.Id + "/" + userAccountId,
						key: readingVerificationOpts.EncryptionKey
					)
				);
			}
			return article;
		}
		public ProofTokenData GetTokenData(string token) {
			var parts = StringEncryption
				.Decrypt(
					text: UrlSafeBase64.Decode(token),
					key: readingVerificationOpts.EncryptionKey
				)
				.Split('/');
			return new ProofTokenData(
				articleId: Int64.Parse(parts[0]),
				userAccountId: Int64.Parse(parts[1])
			);
		}
	}
}