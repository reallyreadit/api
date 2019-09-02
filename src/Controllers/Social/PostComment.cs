using System;
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
			Text = comment.Text;
		}
		public PostComment(
			DbPost post,
			ObfuscationService obfuscationService
		) {
			if (!post.CommentId.HasValue) {
				throw new ArgumentException("CommentId required", nameof(post));
			}
			Id = obfuscationService.Encode(post.CommentId.Value);
			Text = post.CommentText;
		}
		public string Id { get; }
		public string Text { get; }
	}
}