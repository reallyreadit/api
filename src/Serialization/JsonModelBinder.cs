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