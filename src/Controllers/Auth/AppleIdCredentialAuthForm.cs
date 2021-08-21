using System.Text.Json.Serialization;
using api.Analytics;
using api.Authentication;
using api.Notifications;

namespace api.Controllers.Auth {
	public class AppleIdCredentialAuthForm {
		public string AuthorizationCode { get; set; }
		public string Email { get; set; }
		public string IdentityToken { get; set; }
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public AppleRealUserRating RealUserStatus { get; set; }
		public string User { get; set; }
		public SignUpAnalyticsForm Analytics { get; set; }
		public PushDeviceForm PushDevice { get; set; }
		public AppleClient Client { get; set; }
	}
}