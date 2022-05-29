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
using System.Collections.Generic;
using System.Linq;

namespace api.Analytics {
	public static class ClientTypeDictionary {
		private static IEnumerable<Tuple<string, ClientType>> valuePairs = new Tuple<string, ClientType>[] {
			new Tuple<string, ClientType>("desktop/app", ClientType.DesktopApp),
			new Tuple<string, ClientType>("ios/app", ClientType.IosApp),
			new Tuple<string, ClientType>("ios/share-extension", ClientType.IosExtension),
			new Tuple<string, ClientType>("ios/notification", ClientType.IosNotification),
			new Tuple<string, ClientType>("web/app/client", ClientType.WebAppClient),
			new Tuple<string, ClientType>("web/app/server", ClientType.WebAppServer),
			new Tuple<string, ClientType>("web/embed", ClientType.WebEmbed),
			new Tuple<string, ClientType>("web/extension", ClientType.WebExtension),
			new Tuple<string, ClientType>("mail", ClientType.Mail)
		};
		public static IDictionary<string, ClientType> StringToEnum => valuePairs.ToDictionary(pair => pair.Item1, pair => pair.Item2);
		public static IDictionary<ClientType, string> EnumToString => valuePairs.ToDictionary(pair => pair.Item2, pair => pair.Item1);
	}
}