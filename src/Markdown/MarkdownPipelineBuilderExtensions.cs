using Markdig;

namespace api.Markdown {
	public static class MarkdownPipelineBuilderExtensions {
		public static MarkdownPipelineBuilder DisableNonHttpLinks(
			this MarkdownPipelineBuilder builder
		) {
			if (!builder.Extensions.Contains<HttpOnlyLinkInlineExtension>()) {
				builder.Extensions.Add(new HttpOnlyLinkInlineExtension());
			}
			return builder;
		}
	}
}