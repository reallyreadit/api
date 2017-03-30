using System.Collections.Generic;
using api.DataAccess.Models;

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
            this.Children = new List<CommentThread>();
        }
        public List<CommentThread> Children { get; }
    }
}