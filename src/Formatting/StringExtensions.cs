using System;
using System.Linq;

namespace api.Formatting {
	public static class StringExtensions {
		public static string RemoveControlCharacters(this string instance) {
			if (instance == null) {
				return null;
			}
			return new String(
				instance
					.Where(
						character => !Char.IsControl(character)
					)
					.ToArray()
			);
		}
	}
}