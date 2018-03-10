namespace api.Controllers {
	public static class RouteHelper {
		public static string GetArticlePath(string articleSlug) {
			var slugParts = articleSlug.Split('_');
			return $"/articles/{slugParts[0]}/{slugParts[1]}";
		}
	}
}