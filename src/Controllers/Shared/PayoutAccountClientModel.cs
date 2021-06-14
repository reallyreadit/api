namespace api.Controllers.Shared {
	public class PayoutAccountClientModel {
		public PayoutAccountClientModel(bool payoutsEnabled) {
			PayoutsEnabled = payoutsEnabled;
		}
		public bool PayoutsEnabled { get; }
	}
}