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

namespace api.DataAccess.Models {
	public class ArticleAuthorAssignment {
		public long ArticleId { get; set; }
		public long AuthorId { get; set; }
		public DateTime DateAssigned { get; set; }
		public DateTime? DateUnassigned { get; set; }
		public AuthorAssignmentMethod AssignmentMethod { get; set; }
		public long? AssignedByUserAccountId { get; set; }
		public long? UnassignedByUserAccountId { get; set; }
	}
}