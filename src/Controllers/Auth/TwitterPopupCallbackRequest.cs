using Microsoft.AspNetCore.Mvc;

namespace api.Controllers.Auth {
	public class TwitterPopupCallbackRequest : TwitterCallbackRequest {
		[BindProperty(Name = "readup_request_id")]
		public string ReadupRequestId { get; set; }
	}
}