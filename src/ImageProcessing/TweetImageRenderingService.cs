// Copyright (C) 2022 reallyread.it, inc.
//
// This file is part of Readup.
//
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
//
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Globalization;
using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace api.ImageProcessing {
	public class TweetImageRenderingService {
		private readonly FontFamily textFontFamily;
		private readonly FontFamily emojiFontFamily;
		public TweetImageRenderingService(
			FontFamily textFontFamily,
			FontFamily emojiFontFamily
		) {
			this.textFontFamily = textFontFamily;
			this.emojiFontFamily = emojiFontFamily;
		}
		public byte[] RenderTweet(
			string text,
			DateTime datePosted,
			string userName
		) {
			// size parameters
			var width = 507;                    // max width of single twitter image on web
			var height = width / 2;             // slightly smaller than default aspect ratio of single twitter image on web (1:1.5625)
			var paddingTop = (int)Math.Round(width * 0.03);
			var paddingRight = (int)Math.Round(width * 0.03);
			var paddingBottom = (int)Math.Round(width * 0.045);
			var paddingLeft = (int)Math.Round(width * 0.03);
			var defaultFontSize = 20f;
			var maxFontSize = 150;
			var footerFontSize = 12;
			var multiplier = 3;

			// scale size parameters
			width *= multiplier;
			height *= multiplier;
			paddingTop *= multiplier;
			paddingRight *= multiplier;
			paddingBottom *= multiplier;
			paddingLeft *= multiplier;
			defaultFontSize *= multiplier;
			maxFontSize *= multiplier;
			footerFontSize *= multiplier;

			// calculate layout
			var maxTextWidth = width - paddingRight - paddingLeft;
			var maxTextHeight = height - paddingTop - paddingBottom;

			// first measure the text without wrapping
			var fontSize = defaultFontSize;
			var textOptions = new TextOptions(textFontFamily.CreateFont(defaultFontSize)) {
				FallbackFontFamilies = new[] { emojiFontFamily }
			};
			var textRect = TextMeasurer.MeasureBounds(text, textOptions);

			HorizontalAlignment horizontalAlignment;
			if (textRect.Width <= maxTextWidth) {
				var widthRatio = textRect.Width / maxTextWidth;
				fontSize = Math.Min(fontSize / (float)(widthRatio * 1.1), maxFontSize);
				horizontalAlignment = HorizontalAlignment.Center;
				// workaround for emoji rendering bug https://github.com/SixLabors/Fonts/issues/137
				if (Char.GetUnicodeCategory(text, 0) == UnicodeCategory.OtherSymbol) {
					text = ' ' + text + ' ';
				}
			} else {
				// measure the text with wrapping
				var wrappingOptions = new TextOptions(textFontFamily.CreateFont(fontSize)) {
					FallbackFontFamilies = new[] { emojiFontFamily },
					WrappingLength = maxTextWidth
				};
				textRect = TextMeasurer.MeasureBounds(text, wrappingOptions);
				var heightRatio = textRect.Height / maxTextHeight;
				fontSize = fontSize / (float)Math.Sqrt(heightRatio * 1.1);
				horizontalAlignment = HorizontalAlignment.Left;
			}

			// create the image
			using (var stream = new MemoryStream())
			using (var image = new Image<Rgba32>(width, height)) {
				image.Mutate(
					ctx => {
						ctx.BackgroundColor(Color.White);

						ctx.DrawText(
							new RichTextOptions(textFontFamily.CreateFont(fontSize)) {
								FallbackFontFamilies = new[] { emojiFontFamily },
								HorizontalAlignment = horizontalAlignment,
								VerticalAlignment = VerticalAlignment.Center,
								WrappingLength = maxTextWidth,
								Origin = new PointF(paddingLeft, height / 2 + (paddingTop - paddingBottom))
							},
							text,
							Color.Black
						);

						ctx.DrawText(
							new RichTextOptions(textFontFamily.CreateFont(footerFontSize)) {
								HorizontalAlignment = HorizontalAlignment.Center,
								VerticalAlignment = VerticalAlignment.Bottom,
								Origin = new PointF(width / 2, height - (int)(width * 0.02))
							},
							$"Posted on {datePosted.ToString("MMM d, yyyy")} • readup.org/@{userName}",
							Color.Gray
						);
					}
				);
				image.SaveAsPng(stream);
				return stream.ToArray();
			}
		}
	}
}
