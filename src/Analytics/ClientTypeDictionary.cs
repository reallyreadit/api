using System;
using System.Collections.Generic;
using System.Linq;

namespace api.Analytics {
	public static class ClientTypeDictionary {
		private static IEnumerable<Tuple<string, ClientType>> valuePairs = new Tuple<string, ClientType>[] {
			new Tuple<string, ClientType>("ios/app", ClientType.IosApp),
			new Tuple<string, ClientType>("ios/share-extension", ClientType.IosExtension),
			new Tuple<string, ClientType>("web/app/client", ClientType.WebAppClient),
			new Tuple<string, ClientType>("web/app/server", ClientType.WebAppServer),
			new Tuple<string, ClientType>("web/extension", ClientType.WebExtension)
		};
		public static IDictionary<string, ClientType> StringToEnum => valuePairs.ToDictionary(pair => pair.Item1, pair => pair.Item2);
		public static IDictionary<ClientType, string> EnumToString => valuePairs.ToDictionary(pair => pair.Item2, pair => pair.Item1);
	}
}