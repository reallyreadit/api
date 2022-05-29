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

namespace api.Messaging.Views.Shared {
	public class PostViewModel {
		public PostViewModel(
			string author,
			string article,
			string text,
			IEnumerable<CommentAddendum> addenda,
			Uri readArticleUrl,
			Uri viewPostUrl
		) {
			Author = author;
			Article = article;
			CommentText = new CommentTextViewModel(text, addenda);
			ReadArticleUrl = readArticleUrl.ToString();
			ViewPostUrl = viewPostUrl.ToString();
		}
		public string Author { get; }
		public string Article { get; }
		public CommentTextViewModel CommentText { get; }
		public string ReadArticleUrl { get; }
		public string ViewPostUrl { get; }
	}
}