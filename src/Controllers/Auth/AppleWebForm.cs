using System.ComponentModel;
using api.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers.Auth {
	public class AppleWebForm {
		public string Code { get; set; }
		[BindProperty(Name = "id_token")]
		public string IdToken { get; set; }
		[ModelBinder(typeof(HtmlEncodedJsonModelBinder))]
		public AppleWebState State { get; set; }
		[ModelBinder(typeof(JsonModelBinder))]
		public AppleWebUser User { get; set; }
		public string Error { get; set; }
	}
}