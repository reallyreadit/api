using System;

namespace api.DataAccess.Models {
	public class AuthServiceAccount {
		public long IdentityId { get; set; }
		public DateTime DateIdentityCreated { get; set; }
		public string IdentitySignUpAnalytics { get; set; }
		public AuthServiceProvider Provider { get; set; }
		public string ProviderUserId { get; set; }
		public string ProviderUserEmailAddress { get; set; }
		public bool IsEmailAddressPrivate { get; set; }
		public string ProviderUserName { get; set; }
		public string ProviderUserHandle { get; set; }
		public DateTime? DateUserAccountAssociated { get; set; }
		public long? AssociatedUserAccountId { get; set; }
		public string AccessTokenValue { get; set; }
		public string AccessTokenSecret { get; set; }
		public bool IsPostIntegrationEnabled { get; set; }
	}
}