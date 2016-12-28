using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace api.ActionFilters {
	public class LogActionFilter : ResultFilterAttribute {
		public override void OnResultExecuted(ResultExecutedContext context) {
			var originalColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.Write($"{DateTime.Now.ToString("s")} - ");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write($"[{context.HttpContext.Request.Method}] ");
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.Write($"{context.HttpContext.Request.Path}{context.HttpContext.Request.QueryString}\n");
			Console.ForegroundColor = originalColor;
			base.OnResultExecuted(context);
		}
	}
}