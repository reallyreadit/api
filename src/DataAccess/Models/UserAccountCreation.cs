using System;

namespace api.DataAccess.Models {
	public class UserAccountCreation {
		public int Id { get; set; }
		public string Name { get; set; }
		public DateTime DateCreated { get; set; }
		public string TimeZoneName { get; set; }
		public string ClientMode { get; set; }
		public int MarketingVariant { get; set; }
		public string ReferrerUrl { get; set; }
		public string InitialPath { get; set; }
		public string CurrentPath { get; set; }
		public string Action { get; set; }
	}
}