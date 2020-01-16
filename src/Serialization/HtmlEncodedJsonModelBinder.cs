using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;

namespace api.Serialization {
	public class HtmlEncodedJsonModelBinder : IModelBinder {
		public Task BindModelAsync(ModelBindingContext bindingContext) => (
			new JsonModelBinder()
				.BindModelAsync(
					bindingContext,
					stringValue => WebUtility.HtmlDecode(stringValue)
				)
		);
	}
}