// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using api.Configuration;
using Microsoft.Extensions.Options;
using HashidsNet;
using System;

namespace api.Encryption {
	public class ObfuscationService {
		private readonly Hashids hashids;
		public ObfuscationService(IOptions<HashidsOptions> options) {
			hashids = new Hashids(
				salt: options.Value.Salt,
				minHashLength: 6
			);
		}
		public string Encode(params long[] numbers) => hashids.EncodeLong(numbers);
		public long[] Decode(string hash) => hashids.DecodeLong(hash);
		public long? DecodeSingle(string hash) {
			var result = hashids.Decode(hash);
			return result.Length == 1 ? new Nullable<int>(result[0]) : null;
		}
	}
}