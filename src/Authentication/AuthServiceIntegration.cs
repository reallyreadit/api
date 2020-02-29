using System;

namespace api.Authentication {
	[Flags]
	public enum AuthServiceIntegration {
		None = 0,
		Post = 1
	}
}