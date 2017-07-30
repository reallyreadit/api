using System;
using System.Collections.Generic;
using System.Linq;
using api.DataAccess.Models;

namespace api.DataAccess {
	public class PageResult<T> {
		public static PageResult<T> Create<TDbPageResult>(IEnumerable<TDbPageResult> items, int pageNumber, int pageSize) where TDbPageResult : class, T, IDbPageResult {
			var totalCount = items.FirstOrDefault()?.TotalCount ?? 0;
			return new PageResult<T>(items, totalCount, totalCount > 0 ? pageNumber : 1, pageSize);
		}
		public PageResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize) {
			Items = items;
			TotalCount = totalCount;
			PageNumber = pageNumber;
			PageSize = pageSize;
		}
		public IEnumerable<T> Items { get; }
		public int TotalCount { get; }
		public int PageNumber { get; }
		public int PageSize { get; }
		public int PageCount => Math.Max((int)Math.Ceiling((double)TotalCount / PageSize), 1);
	}
}