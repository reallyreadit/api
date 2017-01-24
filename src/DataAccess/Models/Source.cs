using System;

namespace api.DataAccess.Models {
	public class Source {
		public Guid Id { get; set; }
		public string Name { get; set; }
		public string Slug { get; set; }
		public string Url { get; set; }
		public string Hostname { get; set; }
		public string Parser { get; set; }
	}
}