using api.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers.Auth {
	public class AppleWebPopupForm : AppleWebForm {
		[ModelBinder(typeof(HtmlEncodedJsonModelBinder))]
		public AppleWebPopupState State { get; set; }
	}
}