namespace api.Controllers.Auth {
	public class AppleWebResponseForm {
		public string code { get; set; }
		public string id_token { get; set; }
		public string state { get; set; }
		public string user { get; set; }
		public string error { get; set; }
	}
}