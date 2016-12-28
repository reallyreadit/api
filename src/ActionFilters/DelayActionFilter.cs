using System.Threading;
using Microsoft.AspNetCore.Mvc.Filters;

namespace api.ActionFilters {
	public class DelayActionFilter : ResultFilterAttribute {
		private int delay;
		public DelayActionFilter(int delay) {
			this.delay = delay;
		}
		public override void OnResultExecuted(ResultExecutedContext context) {
			Thread.Sleep(this.delay);
			base.OnResultExecuted(context);
		}
	}
}