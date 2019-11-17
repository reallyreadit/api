using System;
using api.DataAccess.Models;
using api.Formatting;

namespace api.Messaging.Views.Shared {
	public class ArticleViewModel {
		private static int DescriptionLengthLimit = 2500;
		private static string Pluralize(string word, int count) => (
			word + (count == 1 ? String.Empty : "s")
		);
		public ArticleViewModel(
			Article article,
			Uri readArticleUrl,
			Uri viewCommentsUrl,
			Uri viewFirstPosterProfileUrl
		) {
			Title = article.Title;
			if (!String.IsNullOrWhiteSpace(article.Description)) {
				var descriptionLength = article.Description.Length;
				if (descriptionLength <= DescriptionLengthLimit) {
					Description = article.Description;
				} else {
					Description = article.Description.Substring(0, DescriptionLengthLimit) + "...";
				}
			}
			Authors = article.Authors.ToListString();
			Source = article.Source;
			if (article.AotdTimestamp.HasValue) {
				AotdTimestamp = article.AotdTimestamp.Value.ToString("dddd") + " " + article.AotdTimestamp.Value.ToString("M/d/yy");
			}
			var minutes = Math.Max(1, article.WordCount / 184);
			Length = minutes + " " + "min";
			ReadCount = article.ReadCount + " " + Pluralize("read", article.ReadCount);
			CommentCount = article.CommentCount + " " + Pluralize("comment", article.CommentCount);
			if (article.AverageRatingScore.HasValue) {
				AverageRatingScore = article.AverageRatingScore.Value < 10 ?
					article.AverageRatingScore.Value.ToString("n1") :
					article.AverageRatingScore.Value.ToString();
			}
			ReadArticleUrl = readArticleUrl.ToString();
			ViewCommentsUrl = viewCommentsUrl.ToString();
			if (!String.IsNullOrWhiteSpace(article.FirstPoster) && viewFirstPosterProfileUrl != null) {
				FirstPoster = article.FirstPoster;
				ViewFirstPosterProfileUrl = viewFirstPosterProfileUrl.ToString();
			}
		}
		public string Title { get; }
		public string Description { get; }
		public string Authors { get; }
		public string Source { get; }
		public string AotdTimestamp { get; }
		public string Length { get; }
		public string ReadCount { get; }
		public string CommentCount { get; }
		public string AverageRatingScore { get; }
		public string FirstPoster { get; }
		public string ReadArticleUrl { get; }
		public string ViewCommentsUrl { get; }
		public string ViewFirstPosterProfileUrl { get; }
	}
}