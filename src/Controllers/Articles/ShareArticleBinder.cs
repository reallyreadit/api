using System;
using System.Linq;
using System.Net;

namespace api.Controllers.Articles {
	public class ShareArticleBinder {
		private string message;
		private string[] emailAddresses = new string[0];
		public long ArticleId { get; set; }
		public string[] EmailAddresses {
			get { return emailAddresses; }
			set {
				emailAddresses = (value ?? new string[0])
					.Where(address => !String.IsNullOrWhiteSpace(address))
					.Select(address => address.Trim())
					.Distinct()
					.ToArray();
			}
		}
		public string Message {
			get { return message; }
			set {
				if (!String.IsNullOrWhiteSpace(value)) {
					message = WebUtility.HtmlEncode(value.Trim());
				}
			}
		}
	}
}