using System;

namespace api.Controllers.UserAccounts {
	[Flags]
	public enum NotificationChannel {
		None = 0,
		Email = 1,
		Extension = 2,
		Push = 4
	}
}