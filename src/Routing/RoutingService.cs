using System;
using System.Net;
using api.Configuration;
using api.Encryption;
using Microsoft.Extensions.Options;

namespace api.Routing {
	public class RoutingService {
		private readonly ServiceEndpointsOptions endpoints;
		private readonly ObfuscationService obfuscation;
		private readonly TokenizationOptions tokenizationOptions;
		public RoutingService(
			IOptions<ServiceEndpointsOptions> endpointOptions,
			ObfuscationService obfuscation,
			IOptions<TokenizationOptions> tokenizationOptions
		) {
			this.endpoints = endpointOptions.Value;
			this.obfuscation = obfuscation;
			this.tokenizationOptions = tokenizationOptions.Value;
		}
		public Uri CreateArticleUrl(string slug) {
			var slugParts = slug.Split('_');
			return new Uri(endpoints.WebServer.CreateUrl($"/read/{slugParts[0]}/{slugParts[1]}"));
		}
		public Uri CreateCommentsUrl(string slug) {
			var slugParts = slug.Split('_');
			return new Uri(endpoints.WebServer.CreateUrl($"/comments/{slugParts[0]}/{slugParts[1]}"));
		}
		public Uri CreateCommentUrl(string slug, long commentId) {
			var slugParts = slug.Split('_');
			return new Uri(endpoints.WebServer.CreateUrl($"/comments/{slugParts[0]}/{slugParts[1]}/{obfuscation.Encode(commentId)}"));
		}
		public Uri CreatePasswordResetUrl(long resetRequestId) {
			var token = WebUtility.UrlEncode(StringEncryption.Encrypt(resetRequestId.ToString(), tokenizationOptions.EncryptionKey));
			return new Uri(endpoints.WebServer.CreateUrl($"/resetPassword?token={token}"));
		}
		public Uri CreatePostUrl(string authorName, long? commentId, long? silentPostId) {
			if (
				(!commentId.HasValue && !silentPostId.HasValue) ||
				(commentId.HasValue && silentPostId.HasValue)
			) {
				throw new ArgumentException("Post must have only commentId or silentPostId");
			}
			string path;
			if (commentId.HasValue) {
				path = $"/@{authorName}/comment/{obfuscation.Encode(commentId.Value)}";
			} else {
				path = $"/@{authorName}/post/{obfuscation.Encode(silentPostId.Value)}";
			}
			return new Uri(endpoints.WebServer.CreateUrl(path));
		}
		public Uri CreateProfileUrl(string name) {
			return new Uri(endpoints.WebServer.CreateUrl($"/@{name}"));
		}
	}
}