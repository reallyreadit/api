using System;
using api.DataAccess.Models;

namespace api.Controllers.BulkMailings {
	public class BulkMailingBinder {
		public string Subject { get; set; }
		public string Body { get; set; }
		public BulkEmailSubscriptionStatusFilter? SubscriptionStatusFilter { get; set; }
		public bool? FreeForLifeFilter { get; set; }
		public DateTime? UserCreatedAfterFilter { get; set; }
		public DateTime? UserCreatedBeforeFilter { get; set; }
	}
}