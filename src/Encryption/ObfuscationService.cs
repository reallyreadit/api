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
		public string Encode(long number) => hashids.EncodeLong(number);
		public long? DecodeSingle(string hash) {
			var result = hashids.Decode(hash);
			return result.Length == 1 ? new Nullable<int>(result[0]) : null;
		}
	}
}