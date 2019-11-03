using api.DataAccess.Models;

namespace api.Controllers.UserAccounts {
	public class NotificationPreference {
		private static AlertEmailPreference GetEmailPreference(
			bool alertEnabled,
			NotificationEventFrequency frequency
		) => (
			alertEnabled ?
				AlertEmailPreference.Immediately :
				frequency == NotificationEventFrequency.Daily ?
					AlertEmailPreference.DailyDigest :
					frequency == NotificationEventFrequency.Weekly ?
						AlertEmailPreference.WeeklyDigest :
						AlertEmailPreference.None
		);
		public NotificationPreference() { }
		public NotificationPreference(
			NotificationPreferenceOptions options
		) {
			CompanyUpdate = options.CompanyUpdateViaEmail;
			Aotd = new AlertPreference() {
				Email = GetEmailPreference(options.AotdViaEmail, options.AotdDigestViaEmail),
				Extension = options.AotdViaExtension,
				Push = options.AotdViaPush
			};
			Post = new AlertPreference() {
				Email = GetEmailPreference(options.PostViaEmail, options.PostDigestViaEmail),
				Extension = options.PostViaExtension,
				Push = options.PostViaPush
			};
			Reply = new AlertPreference() {
				Email = GetEmailPreference(options.ReplyViaEmail, options.ReplyDigestViaEmail),
				Extension = options.ReplyViaExtension,
				Push = options.ReplyViaPush
			};
			Loopback = new AlertPreference() {
				Email = GetEmailPreference(options.LoopbackViaEmail, options.LoopbackDigestViaEmail),
				Extension = options.LoopbackViaExtension,
				Push = options.LoopbackViaPush
			};
			Follower = new AlertPreference() {
				Email = GetEmailPreference(options.FollowerViaEmail, options.FollowerDigestViaEmail),
				Extension = options.FollowerViaExtension,
				Push = options.FollowerViaPush
			};
		}
		public bool CompanyUpdate { get; set; }
		public AlertPreference Aotd { get; set; }
		public AlertPreference Post { get; set; }
		public AlertPreference Reply { get; set; }
		public AlertPreference Loopback { get; set; }
		public AlertPreference Follower { get; set; }
	}
}