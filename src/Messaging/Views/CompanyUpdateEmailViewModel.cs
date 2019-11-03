namespace api.Messaging.Views {
	public class CompanyUpdateEmailViewModel {
		public CompanyUpdateEmailViewModel(
			string html
		) {
			Html = html;
		}
		public string Html { get; }
	}
}