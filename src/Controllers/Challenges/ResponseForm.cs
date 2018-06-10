using api.DataAccess.Models;

namespace api.Controllers.Challenges {
	public class ResponseForm {
		public long ChallengeId { get; set; }
		public ChallengeResponseAction Action { get; set; }
		public long? TimeZoneId { get; set; }
	}
}