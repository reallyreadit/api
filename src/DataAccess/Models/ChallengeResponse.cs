using System;

namespace api.DataAccess.Models {
	public class ChallengeResponse {
		public long Id { get; set; }
		public long ChallengeId { get; set; }
		public long UserAccountId { get; set; }
		public DateTime Date { get; set; }
		public ChallengeResponseAction Action { get; set; }
		public long? TimeZoneId { get; set; }
		public string TimeZoneName { get; set; }
	}
}