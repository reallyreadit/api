using System;

namespace api.DataAccess.Models {
	public class SourceRule {
		public Guid Id { get; set; }
		public string Hostname { get; set; }
		public string Path { get; set; }
		public int Priority { get; set; }
		public SourceRuleAction Action { get; set; }
	}
}