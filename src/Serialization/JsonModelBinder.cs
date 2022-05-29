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
using System.Text.Json;
using System;

namespace api.Serialization {
	public class JsonModelBinder : IModelBinder {
		public Task BindModelAsync(ModelBindingContext bindingContext) {
			return BindModelAsync(bindingContext, null);
		}
		public Task BindModelAsync(ModelBindingContext bindingContext, Func<string, string> mapValue) {
			// get the model name
			var modelName = bindingContext.ModelName;
			// get and set the value
			var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);
			if (valueProviderResult == ValueProviderResult.None) {
				return Task.CompletedTask;
			}
			bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);
			// parse the value
			var stringValue = valueProviderResult.FirstValue;
			try {
				var model = JsonSerializer.Deserialize(
					mapValue != null ?
						mapValue(stringValue) :
						stringValue,
					bindingContext.ModelType,
					new JsonSerializerOptions() {
						AllowTrailingCommas = true,
						PropertyNameCaseInsensitive = true
					}
				);
				bindingContext.Result = ModelBindingResult.Success(model);
				return Task.CompletedTask;
			} catch {
				bindingContext.ModelState.TryAddModelError(modelName, "Failed to parse JSON.");
				return Task.CompletedTask;
			}
		}
	}
}