using api.DataAccess.Models;

namespace api.Controllers.Challenges {
	public class StartForm {
		public long ChallengeId { get; set; }
		public long TimeZoneId { get; set; }
	}
}