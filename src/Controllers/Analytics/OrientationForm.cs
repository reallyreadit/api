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
using api.DataAccess.Models;

namespace api.Controllers.Analytics {
	public class OrientationForm {
		public int TrackingPlayCount { get; set; }
		public bool TrackingSkipped { get; set; }
		public int TrackingDuration { get; set; }
		public int ImportPlayCount { get; set; }
		public bool ImportSkipped { get; set; }
		public int ImportDuration { get; set; }
		public NotificationAuthorizationRequestResult NotificationsResult { get; set; }
		public bool NotificationsSkipped { get; set; }
		public int NotificationsDuration { get; set; }
		public Guid? ShareResultId { get; set; }
		public bool ShareSkipped { get; set; }
		public int ShareDuration { get; set; }
	}
}