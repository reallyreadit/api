namespace api.DataAccess.Models {
	public class NotificationPreferenceOptions {
		public bool CompanyUpdateViaEmail { get; set; }
		public bool SuggestedReadingViaEmail { get; set; }
		public bool AotdViaEmail { get; set; }
		public bool AotdViaExtension { get; set; }
		public bool AotdViaPush { get; set; }
		public bool ReplyViaEmail { get; set; }
		public bool ReplyViaExtension { get; set; }
		public bool ReplyViaPush { get; set; }
		public NotificationEventFrequency ReplyDigestViaEmail { get; set; }
		public bool LoopbackViaEmail { get; set; }
		public bool LoopbackViaExtension { get; set; }
		public bool LoopbackViaPush { get; set; }
		public NotificationEventFrequency LoopbackDigestViaEmail { get; set; }
		public bool PostViaEmail { get; set; }
		public bool PostViaExtension { get; set; }
		public bool PostViaPush { get; set; }
		public NotificationEventFrequency PostDigestViaEmail { get; set; }
		public bool FollowerViaEmail { get; set; }
		public bool FollowerViaExtension { get; set; }
		public bool FollowerViaPush { get; set; }
		public NotificationEventFrequency FollowerDigestViaEmail { get; set; }
	}
}