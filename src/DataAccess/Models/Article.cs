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
using api.Formatting;

namespace api.DataAccess.Models {
	public class Article {
		public long Id { get; set; }
		public string Title { get; set; }
		public string Slug { get; set; }
		public string Source { get; set; }
		public DateTime? DatePublished { get; set; }
		public string Section { get; set; }
		public string Description { get; set; }
		public DateTime? AotdTimestamp { get; set; }
		public string Url { get; set; }
		public string[] Authors { get; set; }	// backward compat. new clients use ArticleAuthors
		public string[] Tags { get; set; }
		public int WordCount { get; set; }
		public int CommentCount { get; set; }
		public int ReadCount { get; set; }
		public double? AverageRatingScore { get; set; }
		public DateTime? DateCreated { get; set; }
		public decimal PercentComplete { get; set; }
		public bool IsRead { get; set; }
		public DateTime? DateStarred { get; set; }
		public int? RatingScore { get; set; }
		public DateTime? DatePosted => DatesPosted.Any() ? DatesPosted.Max() : new Nullable<DateTime>(); // backward compat hack
		public DateTime[] DatesPosted { get; set; }
		public int HotScore { get; set; }
		public int HotVelocity { get; set; }	// unused, backward compat for ios
		public int RatingCount { get; set; }
		public string FirstPoster { get; set; }
		public ArticleFlair Flair { get; set; }
		public int AotdContenderRank { get; set; }
		public ArticleAuthor[] ArticleAuthors { get; set; }
		public string ImageUrl { get; set; }
		public string ProofToken { get; set; }
		public string GetFormattedByline(int maxAuthorCount = 3) {
			var byline = Authors.ToListString();
			if (!String.IsNullOrWhiteSpace(Source)) {
				if (!String.IsNullOrWhiteSpace(byline)) {
					byline += " in ";
				}
				byline += Source.Trim();
			}
			return byline;
		}
	}
}