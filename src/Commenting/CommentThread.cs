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
using System.Collections.Generic;
using api.DataAccess.Models;
using System.Linq;
using api.Encryption;
using api.DataAccess.Stats;

namespace api.Commenting {
    public class CommentThread {
        public CommentThread(
            Comment comment,
            LeaderboardBadge badge,
			bool isAuthor,
            ObfuscationService obfuscationService
        ) {
            Id = obfuscationService.Encode(comment.Id);
            DateCreated = comment.DateCreated;
            if (comment.DateDeleted.HasValue) {
                Text = String.Empty;
                Addenda = new CommentAddendum[0];
                UserAccount = String.Empty;
                Badge = LeaderboardBadge.None;
            } else {
				Text = comment.Text;
                Addenda = comment.Addenda
                    .OrderBy(addendum => addendum.DateCreated)
					.ToArray(); ;
				UserAccount = comment.UserAccount;
				Badge = badge;
            }
            ArticleId = comment.ArticleId;
            ArticleSlug = comment.ArticleSlug;
            ArticleTitle = comment.ArticleTitle;
            ParentCommentId = (
                comment.ParentCommentId.HasValue ?
                obfuscationService.Encode(comment.ParentCommentId.Value) :
                null
            );
            DateDeleted = comment.DateDeleted;
            Children = new List<CommentThread>();
			IsAuthor = isAuthor;
        }
		public string Id { get; set; }
		public DateTime DateCreated { get; set; }
		public string Text { get; set; }
        public CommentAddendum[] Addenda { get; set; }
		public long ArticleId { get; set; }
		public string ArticleTitle { get; set; }
		public string ArticleSlug { get; set; }
		public string UserAccount { get; set; }
        public LeaderboardBadge Badge { get; set; }
		public string ParentCommentId { get; set; }
        public DateTime? DateDeleted { get; set; }
        public List<CommentThread> Children { get; }
		public bool IsAuthor { get; }
        public DateTime MaxDate => new DateTime(Math.Max(DateCreated.Ticks, Children.Any() ? Children.Max(c => c.MaxDate).Ticks : 0));
    }
}