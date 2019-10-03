using api.DataAccess.Models;

namespace api.Controllers.UserAccounts {
	public class NotificationPreference {
		public NotificationPreference() { }
		public NotificationPreference(
			NotificationPreferenceOptions options
		) {
			CompanyUpdate = (
				options.CompanyUpdateViaEmail ?
					NotificationChannel.Email :
					NotificationChannel.None
			);
			SuggestedReading = (
				options.SuggestedReadingViaEmail ?
					NotificationEventFrequency.Weekly :
					NotificationEventFrequency.Never
			);
			Aotd = NotificationChannel.None;
			if (options.AotdViaEmail) {
				Aotd |= NotificationChannel.Email;
			}
			if (options.AotdViaExtension) {
				Aotd |= NotificationChannel.Extension;
			}
			if (options.AotdViaPush) {
				Aotd |= NotificationChannel.Push;
			}
			if (options.ReplyDigestViaEmail != NotificationEventFrequency.Never) {
				Reply = NotificationChannel.None;
				ReplyDigest = options.ReplyDigestViaEmail;
			} else {
				Reply = NotificationChannel.None;
				if (options.ReplyViaEmail) {
					Reply |= NotificationChannel.Email;
				}
				if (options.ReplyViaExtension) {
					Reply |= NotificationChannel.Extension;
				}
				if (options.ReplyViaPush) {
					Reply |= NotificationChannel.Push;
				}
				ReplyDigest = NotificationEventFrequency.Never;
			}
			if (options.LoopbackDigestViaEmail != NotificationEventFrequency.Never) {
				Loopback = NotificationChannel.None;
				LoopbackDigest = options.LoopbackDigestViaEmail;
			} else {
				Loopback = NotificationChannel.None;
				if (options.LoopbackViaEmail) {
					Loopback |= NotificationChannel.Email;
				}
				if (options.LoopbackViaExtension) {
					Loopback |= NotificationChannel.Extension;
				}
				if (options.LoopbackViaPush) {
					Loopback |= NotificationChannel.Push;
				}
				LoopbackDigest = NotificationEventFrequency.Never;
			}
			if (options.PostDigestViaEmail != NotificationEventFrequency.Never) {
				Post = NotificationChannel.None;
				PostDigest = options.PostDigestViaEmail;
			} else {
				Post = NotificationChannel.None;
				if (options.PostViaEmail) {
					Post |= NotificationChannel.Email;
				}
				if (options.PostViaExtension) {
					Post |= NotificationChannel.Extension;
				}
				if (options.PostViaPush) {
					Post |= NotificationChannel.Push;
				}
				PostDigest = NotificationEventFrequency.Never;
			}
			if (options.FollowerDigestViaEmail != NotificationEventFrequency.Never) {
				Follower = NotificationChannel.None;
				FollowerDigest = options.FollowerDigestViaEmail;
			} else {
				Follower = NotificationChannel.None;
				if (options.FollowerViaEmail) {
					Follower |= NotificationChannel.Email;
				}
				if (options.FollowerViaExtension) {
					Follower |= NotificationChannel.Extension;
				}
				if (options.FollowerViaPush) {
					Follower |= NotificationChannel.Push;
				}
				FollowerDigest = NotificationEventFrequency.Never;
			}
		}
		public NotificationChannel CompanyUpdate { get; set; }
		public NotificationEventFrequency SuggestedReading { get; set; }
		public NotificationChannel Aotd { get; set; }
		public NotificationChannel Reply { get; set; }
		public NotificationEventFrequency ReplyDigest { get; set; }
		public NotificationChannel Loopback { get; set; }
		public NotificationEventFrequency LoopbackDigest { get; set; }
		public NotificationChannel Post { get; set; }
		public NotificationEventFrequency PostDigest { get; set; }
		public NotificationChannel Follower { get; set; }
		public NotificationEventFrequency FollowerDigest { get; set; }
	}
}