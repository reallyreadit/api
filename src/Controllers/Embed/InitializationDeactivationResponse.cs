using api.DataAccess.Models;

namespace api.Controllers.Embed {
	public class InitializationDeactivationResponse {
		public InitializationDeactivationResponse() {
			Action = InitializationAction.Deactivate;
		}
		public InitializationAction Action { get; }
	}
}