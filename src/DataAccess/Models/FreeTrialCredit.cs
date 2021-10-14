using System;

namespace api.DataAccess.Models {
	public class FreeTrialCredit {
		public long Id { get; set; }
		public DateTime DateCreated { get; set; }
		public long UserAccountId { get; set; }
		public FreeTrialCreditTrigger CreditTrigger { get; set; }
		public FreeTrialCreditType CreditType { get; set; }
		public int AmountCredited { get; set; }
		public int AmountRemaining { get; set; }
	}
}