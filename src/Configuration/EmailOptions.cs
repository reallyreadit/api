// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using Amazon;
using api.Messaging;

namespace api.Configuration {
	public class EmailOptions {
		public EmailMailboxOptions From { get; set; }
		public EmailDeliveryMethod DeliveryMethod { get; set; }
		public string AmazonSesRegionEndpoint { get; set; }
		public SmtpServerOptions SmtpServer { get; set; }
	}
}