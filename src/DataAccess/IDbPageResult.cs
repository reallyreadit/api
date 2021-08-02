using System.Collections.Generic;

namespace api.DataAccess.Models {
	public interface IDbPageResult<T> {
		IEnumerable<T> Items { get; }
		int TotalCount { get; }
	}
}