using System;

namespace api.DataAccess.Models { 
	public class ProvisionalUserArticle {
		public long ArticleId { get; set; }
		public long ProvisionalUserAccountId { get; set; }
		public DateTime DateCreated { get; set; }
		public DateTime? LastModified { get; set; }
		public int ReadableWordCount { get; set; }
		public int[] ReadState { get; set; }
		public int WordsRead { get; set; }
		public DateTime? DateCompleted { get; set; }
	}
}