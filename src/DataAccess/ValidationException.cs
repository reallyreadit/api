using System;

namespace api.DataAccess {
	public class ValidationException : Exception {
		public ValidationException(params string[] errors)
		{
			this.Errors = errors;
		}
		public string[] Errors { get; }
	}
}