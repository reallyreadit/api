using api.DataAccess.Models;

namespace api.Controllers.UserAccounts {
	public class DisplayPreferenceForm {
		public DisplayTheme Theme { get; set; }
		public int TextSize { get; set; }
		public bool HideLinks { get; set; }
	}
}