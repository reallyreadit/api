using System;

namespace api.DataAccess.Models {
	public class DisplayPreference {
		public long Id { get; set; }
		public long UserAccountId { get; set; }
		public DateTime LastModified { get; set; }
		public DisplayTheme Theme { get; set; }
		public int TextSize { get; set; }
		public bool HideLinks { get; set; }
	}
}