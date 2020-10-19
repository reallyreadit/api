using api.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers.Auth {
	public class AppleWebRedirectForm : AppleWebForm {
		[ModelBinder(typeof(HtmlEncodedJsonModelBinder))]
		public AppleWebRedirectState State { get; set; }
	}
}