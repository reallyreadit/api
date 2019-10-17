using System;

namespace api.Controllers.UserAccounts {
	public class PushDeviceForm {
		public string InstallationId { get; set; }
		public string Name { get; set; }
		public string Token { get; set; }
		public bool IsValid() => (
			!String.IsNullOrWhiteSpace(InstallationId) &&
			!String.IsNullOrWhiteSpace(Token)
		);
	}
}