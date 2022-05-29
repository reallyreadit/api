// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.DataAccess.Models;

namespace api.DataAccess {
	public class PageResult<T> {
		public static PageResult<T> Create(IDbPageResult<T> result, int pageNumber, int pageSize) {
			return new PageResult<T>(result.Items, result.TotalCount, result.TotalCount > 0 ? pageNumber : 1, pageSize);
		}
		public static PageResult<T> Create<TSource>(PageResult<TSource> source, Func<IEnumerable<TSource>, IEnumerable<T>> map) => new PageResult<T>(
			items: map(source.Items),
			totalCount: source.TotalCount,
			pageNumber: source.PageNumber,
			pageSize: source.PageSize
		);
		public static async Task<PageResult<T>> CreateAsync<TSource>(PageResult<TSource> source, Func<IEnumerable<TSource>, Task<IEnumerable<T>>> map) => new PageResult<T>(
			items: await map(source.Items),
			totalCount: source.TotalCount,
			pageNumber: source.PageNumber,
			pageSize: source.PageSize
		);
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