using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace api.Encryption {
	public static class StringEncryption {
		public static string Encrypt(string text, string key) {
			var inputBuffer = Encoding.UTF8.GetBytes(text);
			using (var aes = Aes.Create())
			using (var encryptor = aes.CreateEncryptor(
				rgbKey: Convert.FromBase64String(key),
				rgbIV: aes.IV
			)) {
				return Convert.ToBase64String(
					inArray: encryptor
						.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length)
						.Concat(aes.IV)
						.ToArray()
				);
			}
		}
		public static string Decrypt(string text, string key) {
			var inputBuffer = Convert.FromBase64String(text);
			using (var aes = Aes.Create())
			using (var decryptor = aes.CreateDecryptor(
				rgbKey: Convert.FromBase64String(key),
				rgbIV: inputBuffer.Skip(inputBuffer.Length - aes.IV.Length).ToArray()
			)) {
				return Encoding.UTF8.GetString(
					bytes: decryptor.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length - aes.IV.Length)
				);
			}
		}
	}
}