using System;

namespace api.Controllers.Analytics {
	public class ShareForm {
		public Guid? Id { get; set; }
		public string Action { get; set; }
		public string ActivityType { get; set; }
		public bool? Completed { get; set; }
		public string Error { get; set; }
	}
}