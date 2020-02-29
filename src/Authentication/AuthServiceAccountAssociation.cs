using System;
using api.DataAccess.Models;

namespace api.Authentication {
	public class AuthServiceAccountAssociation {
		public AuthServiceAccountAssociation(
			AuthServiceAccount account
		) {
			DateAssociated = account.DateUserAccountAssociated.Value;
			EmailAddress = account.ProviderUserEmailAddress;
			Handle = account.ProviderUserHandle;
			IdentityId = account.IdentityId;
			Integrations = account.IsPostIntegrationEnabled ?
				AuthServiceIntegration.Post :
				AuthServiceIntegration.None;
			Provider = account.Provider;
		}
		public DateTime DateAssociated { get; }
		public string EmailAddress { get; }
		public string Handle { get; }
		public long IdentityId { get; }
		public AuthServiceIntegration Integrations { get; }
		public AuthServiceProvider Provider { get; }
	}
}