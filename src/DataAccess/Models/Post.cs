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
	public class Post {
		public DateTime DateCreated { get; set; }
		public string UserName { get; set; }
		public long ArticleId { get; set; }
		public long? CommentId { get; set; }
		public string CommentText { get; set; }
		public CommentAddendum[] CommentAddenda { get; set; }
		public long? SilentPostId { get; set; }
		public DateTime? DateDeleted { get; set; }
		public bool HasAlert { get; set; }
	}
}