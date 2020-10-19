namespace api.Authentication {
	public enum AuthenticationError {
		Cancelled = 1,
		InvalidAuthToken = 2,
		InvalidSessionId = 3,
		EmailAddressRequired = 4,
		Unknown = 5
	}
}