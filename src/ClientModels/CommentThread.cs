using System;
using System.Collections.Generic;
using api.DataAccess.Models;
using System.Linq;
using api.Encryption;
using api.DataAccess.Stats;

namespace api.ClientModels {
    public class CommentThread {
        public CommentThread(
            Comment comment,
            LeaderboardBadge badge,
            ObfuscationService obfuscationService
        ) {
            Id = obfuscationService.Encode(comment.Id);
            DateCreated = comment.DateCreated;
            Text = comment.Text;
            ArticleId = comment.ArticleId;
            ArticleSlug = comment.ArticleSlug;
            ArticleTitle = comment.ArticleTitle;
            UserAccount = comment.UserAccount;
            Badge = badge;
            ParentCommentId = (
                comment.ParentCommentId.HasValue ?
                obfuscationService.Encode(comment.ParentCommentId.Value) :
                null
            );
            Children = new List<CommentThread>();
        }
		public string Id { get; set; }
		public DateTime DateCreated { get; set; }
		public string Text { get; set; }
		public long ArticleId { get; set; }
		public string ArticleTitle { get; set; }
		public string ArticleSlug { get; set; }
		public string UserAccount { get; set; }
        public LeaderboardBadge Badge { get; set; }
		public string ParentCommentId { get; set; }
        public List<CommentThread> Children { get; }
        public DateTime MaxDate => new DateTime(Math.Max(DateCreated.Ticks, Children.Any() ? Children.Max(c => c.MaxDate).Ticks : 0));
    }
}