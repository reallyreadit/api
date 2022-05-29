// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

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