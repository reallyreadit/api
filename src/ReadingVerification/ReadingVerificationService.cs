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