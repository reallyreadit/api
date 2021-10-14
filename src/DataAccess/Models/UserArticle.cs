using System;

namespace api.DataAccess.Models {
	public class UserArticle {
		public long Id { get; set; }
		public long ArticleId { get; set; }
		public long UserAccountId { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime? DateViewed { get; set; }
		public DateTime? LastModified { get; set; }
		public int ReadableWordCount { get; set; }
		public int[] ReadState { get; set; }
		public int WordsRead { get; set; }
		public DateTime? DateCompleted { get; set; }
		public long? FreeTrialCreditId { get; set; }
	}
}