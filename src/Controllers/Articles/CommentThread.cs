using System;
using System.Collections.Generic;
using api.DataAccess.Models;
using System.Linq;

namespace api.Controllers.Articles {
    public class CommentThread : Comment {
        public CommentThread(Comment comment) {
            this.Id = comment.Id;
            this.DateCreated = comment.DateCreated;
            this.Text = comment.Text;
            this.ArticleId = comment.ArticleId;
            this.ArticleSlug = comment.ArticleSlug;
            this.ArticleTitle = comment.ArticleTitle;
            this.UserAccountId = comment.UserAccountId;
            this.UserAccount = comment.UserAccount;
            this.ParentCommentId = comment.ParentCommentId;
            this.DateRead = comment.DateRead;
            this.Children = new List<CommentThread>();
        }
        public List<CommentThread> Children { get; }
        public DateTime MaxDate => new DateTime(Math.Max(DateCreated.Ticks, Children.Any() ? Children.Max(c => c.MaxDate).Ticks : 0));
    }
}