namespace api.ReadingVerification {
	public class ProofTokenData {
		public ProofTokenData(long articleId, long userAccountId) {
			ArticleId = articleId;
			UserAccountId = userAccountId;
		}
		public long ArticleId { get; }
		public long UserAccountId { get; }
	}
}