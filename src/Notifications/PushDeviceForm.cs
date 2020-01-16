using System;

namespace api.Notifications {
	public class PushDeviceForm {
		public static PushDeviceForm Blank => new PushDeviceForm();
		public string InstallationId { get; set; }
		public string Name { get; set; }
		public string Token { get; set; }
		public bool IsValid() => (
			!String.IsNullOrWhiteSpace(InstallationId) &&
			!String.IsNullOrWhiteSpace(Token)
		);
	}
}