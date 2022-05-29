// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

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