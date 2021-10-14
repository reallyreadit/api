using System;
using api.DataAccess.Models;

namespace api.Controllers.Shared {
	public class FreeTrialCreditClientModel {
		public FreeTrialCreditClientModel(FreeTrialCredit credit) {
			DateCreated = credit.DateCreated;
			Trigger = credit.CreditTrigger;
			Type = credit.CreditType;
			Amount = credit.AmountCredited;
		}
		public DateTime DateCreated { get;}
		public FreeTrialCreditTrigger Trigger { get;}
		public FreeTrialCreditType Type { get;}
		public int Amount { get; }
	}
}