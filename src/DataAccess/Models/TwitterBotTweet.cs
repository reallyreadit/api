using System;

namespace api.DataAccess.Models {
	public class TwitterBotTweet {
		public long Id { get; set; }
		public string Handle { get; set; }
		public DateTime DateTweeted { get; set; }
		public long? ArticleId { get; set; }
		public long? CommentId { get; set; }
		public string Content { get; set; }
		public string TweetId { get; set; }
	}
}