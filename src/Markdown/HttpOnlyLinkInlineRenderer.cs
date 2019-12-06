using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;

namespace api.Markdown {
	public class HttpOnlyLinkInlineRenderer : LinkInlineRenderer {
		protected override void Write(
			HtmlRenderer renderer,
			LinkInline link
		) {
			if (link.Url?.StartsWith("http") ?? false) {
				base.Write(renderer, link);
			} else {
				renderer.WriteChildren(link);
			}
		}
	}
}