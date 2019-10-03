using System;
using api.ClientModels;
using api.DataAccess.Models;
using api.DataAccess.Stats;

namespace api.Controllers.Social {
	public class Post {
		public Post(
			DateTime date,
			string userName,
			LeaderboardBadge badge,
			Article article,
			PostComment comment,
			string silentPostId,
			bool hasAlert
		) {
			Date = date;
			UserName = userName;
			Badge = badge;
			Article = article;
			Comment = comment;
			SilentPostId = silentPostId;
			HasAlert = hasAlert;
		}
		public DateTime Date { get; }
		public string UserName { get; }
		public LeaderboardBadge Badge { get; }
		public Article Article { get; }
		public PostComment Comment { get; }
		public string SilentPostId { get; set; }
		public bool HasAlert { get; set; }
	}
}