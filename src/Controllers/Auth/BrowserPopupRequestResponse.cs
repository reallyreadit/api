namespace api.Controllers.Auth {
	public class BrowserPopupRequestResponse {
		public BrowserPopupRequestResponse(
			string requestId,
			string popupUrl
		) {
			RequestId = requestId;
			PopupUrl = popupUrl;
		}
		public string RequestId { get; }
		public string PopupUrl { get; }
	}
}