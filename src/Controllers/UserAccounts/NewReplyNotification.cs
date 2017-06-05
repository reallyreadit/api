using System;
using api.DataAccess.Models;

namespace api.Controllers.UserAccounts {
	public class NewReplyNotification {
		private static Int64 CreateUnixTimestamp(DateTime date) => new DateTimeOffset(date, TimeSpan.Zero).ToUnixTimeMilliseconds();
		public NewReplyNotification(UserAccount userAccount, DateTime? lastReplyDate) {
			LastReply = lastReplyDate.HasValue ? CreateUnixTimestamp(lastReplyDate.Value) : 0;
			LastNewReplyAck = CreateUnixTimestamp(userAccount.LastNewReplyAck);
			LastNewReplyDesktopNotification = CreateUnixTimestamp(userAccount.LastNewReplyDesktopNotification);
		}
		public Int64 LastReply { get; }
		public Int64 LastNewReplyAck { get; }
		public Int64 LastNewReplyDesktopNotification { get; }
		public Int64 Timestamp { get; } = CreateUnixTimestamp(DateTime.UtcNow);
	}
}