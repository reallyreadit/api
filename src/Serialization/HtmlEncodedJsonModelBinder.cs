// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;

namespace api.Serialization {
	public class HtmlEncodedJsonModelBinder : IModelBinder {
		public Task BindModelAsync(ModelBindingContext bindingContext) => (
			new JsonModelBinder()
				.BindModelAsync(
					bindingContext,
					stringValue => WebUtility.HtmlDecode(stringValue)
				)
		);
	}
}