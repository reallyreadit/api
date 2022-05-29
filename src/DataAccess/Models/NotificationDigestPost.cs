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

namespace api.DataAccess.Models {
	public class NotificationDigestPost {
		public NotificationDigestPost(
			long? commentId,
			long? silentPostId,
			DateTime dateCreated,
			string commentText,
			CommentAddendum[] commentAddenda,
			string author,
			long articleId,
			string articleTitle
		) {
			CommentId = commentId;
			SilentPostId = silentPostId;
			DateCreated = dateCreated;
			CommentText = commentText;
			CommentAddenda = commentAddenda;
			Author = author;
			ArticleId = articleId;
			ArticleTitle = articleTitle;
		}
		public long? CommentId { get; }
		public long? SilentPostId { get; }
		public DateTime DateCreated { get; }
		public string CommentText { get; }
		public CommentAddendum[] CommentAddenda { get; }
		public string Author { get; }
		public long ArticleId { get; }
		public string ArticleTitle { get; }
	}
}