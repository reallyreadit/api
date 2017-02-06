using System;

namespace api.Controllers.Extension {
	public class CommitReadStateBinder {
		public Guid UserPageId { get; set; }
		public int[] ReadState { get; set; }
	}
}