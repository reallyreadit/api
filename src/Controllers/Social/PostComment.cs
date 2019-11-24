using System;
using System.Linq;
using api.DataAccess.Models;
using api.Encryption;
using DbPost = api.DataAccess.Models.Post;

namespace api.Controllers.Social {
	public class PostComment {
		public PostComment(
			Comment comment,
			ObfuscationService obfuscationService
		) {
			Id = obfuscationService.Encode(comment.Id);
			if (comment.DateDeleted.HasValue) {
				Text = String.Empty;
				Addenda = new CommentAddendum[0];
			} else {
				Text = comment.Text;
				Addenda = comment.Addenda;
			}
		}
		public PostComment(
			DbPost post,
			ObfuscationService obfuscationService
		) {
			if (!post.CommentId.HasValue) {
				throw new ArgumentException("CommentId required", nameof(post));
			}
			Id = obfuscationService.Encode(post.CommentId.Value);
			if (post.DateDeleted.HasValue) {
				Text = String.Empty;
				Addenda = new CommentAddendum[0];
			} else {
				Text = post.CommentText;
				Addenda = post.CommentAddenda
					.OrderBy(addendum => addendum.DateCreated)
					.ToArray();
			}
		}
		public string Id { get; }
		public string Text { get; }
		public CommentAddendum[] Addenda { get; set; }
	}
}