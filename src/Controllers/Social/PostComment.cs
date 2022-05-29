// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

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