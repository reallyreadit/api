using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace api.Messaging {
	public static class ViewRendering {
		public static async Task<string> RenderView(Controller controller, ICompositeViewEngine viewEngine, string viewName, object model) {
			using (var stringWriter = new StringWriter()) {
				var view = viewEngine.FindView(controller.ControllerContext, viewName, isMainPage: true).View;
				controller.ViewData.Model = model;
				await view.RenderAsync(new ViewContext(
					actionContext: controller.ControllerContext,
					view: view,
					viewData: controller.ViewData,
					tempData: controller.TempData,
					writer: stringWriter,
					htmlHelperOptions: new HtmlHelperOptions()
				));
				return stringWriter.ToString();
			}
		}
	}
}