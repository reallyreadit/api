using System.Linq;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;

namespace api.Markdown {
	public class HttpOnlyLinkInlineExtension : IMarkdownExtension {
		public void Setup(
			MarkdownPipelineBuilder pipelineBuilder
		) {

		}
		public void Setup(
			MarkdownPipeline pipeline,
			IMarkdownRenderer renderer
		) {
			var htmlRenderer = renderer as HtmlRenderer;
			if (htmlRenderer != null) {
				foreach (var linkRenderer in htmlRenderer.ObjectRenderers.OfType<LinkInlineRenderer>().ToArray()) {
					htmlRenderer.ObjectRenderers.Remove(linkRenderer);
				}
				if (!htmlRenderer.ObjectRenderers.Contains<HttpOnlyLinkInlineRenderer>()) {
					htmlRenderer.ObjectRenderers.Add(new HttpOnlyLinkInlineRenderer());
				}
			}
		}
	}
}