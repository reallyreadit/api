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
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace api.Encryption {
	public static class StringEncryption {
		// https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes?view=netcore-3.1
		public static string Encrypt(string text, string key) {
			byte[] encryptionResult;
			using (
				var aes = Aes.Create()
			)
			using (
				var encryptor = aes.CreateEncryptor(
					rgbKey: Convert.FromBase64String(key),
					rgbIV: aes.IV
				)
			)
			using (
				var memoryStream = new MemoryStream()
			)
			using (
				var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write)
			) {
				using (
					var streamWriter = new StreamWriter(cryptoStream)
				) {
					streamWriter.Write(text);
				}
				encryptionResult = memoryStream
					.ToArray()
					.Concat(aes.IV)
					.ToArray();
			}
			return Convert.ToBase64String(encryptionResult);
		}
		public static string Decrypt(string text, string key) {
			// hack to correct for raw plus signs getting converted to spaces in query strings
			var inputBuffer = Convert.FromBase64String(
				text.Replace(' ', '+')
			);
			string decryptionResult;
			using (
				var aes = Aes.Create()
			)
			using (
				var decryptor = aes.CreateDecryptor(
					rgbKey: Convert.FromBase64String(key),
					rgbIV: inputBuffer
						.Skip(inputBuffer.Length - aes.IV.Length)
						.ToArray()
				)
			)
			using (
				var memoryStream = new MemoryStream(inputBuffer, 0, inputBuffer.Length - aes.IV.Length)
			)
			using (
				var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read)
			)
			using (
				var streamReader = new StreamReader(cryptoStream)
			) {
				decryptionResult = streamReader.ReadToEnd();
			}
			return decryptionResult;
		}
	}
}