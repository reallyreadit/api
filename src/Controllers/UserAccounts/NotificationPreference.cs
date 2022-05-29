// Copyright (C) 2022 reallyread.it, inc.
// 
// This file is part of Readup.
// 
// Readup is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License version 3 as published by the Free Software Foundation.
// 
// Readup is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License version 3 along with Foobar. If not, see <https://www.gnu.org/licenses/>.

using api.DataAccess.Models;

namespace api.Controllers.UserAccounts {
	public class NotificationPreference {
		private static AlertEmailPreference GetEmailPreference(
			bool alertEnabled,
			NotificationEventFrequency frequency
		) => (
			alertEnabled ?
				AlertEmailPreference.Immediately :
				frequency == NotificationEventFrequency.Daily ?
					AlertEmailPreference.DailyDigest :
					frequency == NotificationEventFrequency.Weekly ?
						AlertEmailPreference.WeeklyDigest :
						AlertEmailPreference.None
		);
		public NotificationPreference() { }
		public NotificationPreference(
			NotificationPreferenceOptions options
		) {
			CompanyUpdate = options.CompanyUpdateViaEmail;
			Aotd = new AlertPreference() {
				Email = GetEmailPreference(options.AotdViaEmail, options.AotdDigestViaEmail),
				Extension = options.AotdViaExtension,
				Push = options.AotdViaPush
			};
			Post = new AlertPreference() {
				Email = GetEmailPreference(options.PostViaEmail, options.PostDigestViaEmail),
				Extension = options.PostViaExtension,
				Push = options.PostViaPush
			};
			Reply = new AlertPreference() {
				Email = GetEmailPreference(options.ReplyViaEmail, options.ReplyDigestViaEmail),
				Extension = options.ReplyViaExtension,
				Push = options.ReplyViaPush
			};
			Loopback = new AlertPreference() {
				Email = GetEmailPreference(options.LoopbackViaEmail, options.LoopbackDigestViaEmail),
				Extension = options.LoopbackViaExtension,
				Push = options.LoopbackViaPush
			};
			Follower = new AlertPreference() {
				Email = GetEmailPreference(options.FollowerViaEmail, options.FollowerDigestViaEmail),
				Extension = options.FollowerViaExtension,
				Push = options.FollowerViaPush
			};
		}
		public bool CompanyUpdate { get; set; }
		public AlertPreference Aotd { get; set; }
		public AlertPreference Post { get; set; }
		public AlertPreference Reply { get; set; }
		public AlertPreference Loopback { get; set; }
		public AlertPreference Follower { get; set; }
		public NotificationPreferenceOptions GetOptions() {
			return new NotificationPreferenceOptions() {
				CompanyUpdateViaEmail = CompanyUpdate,
				AotdViaEmail = Aotd.Email == AlertEmailPreference.Immediately,
				AotdViaExtension = Aotd.Extension,
				AotdViaPush = Aotd.Push,
				AotdDigestViaEmail = Aotd.Email == AlertEmailPreference.DailyDigest ?
						NotificationEventFrequency.Daily :
						Aotd.Email == AlertEmailPreference.WeeklyDigest ?
							NotificationEventFrequency.Weekly :
							NotificationEventFrequency.Never,
				PostViaEmail = Post.Email == AlertEmailPreference.Immediately,
				PostViaExtension = Post.Extension,
				PostViaPush = Post.Push,
				PostDigestViaEmail = Post.Email == AlertEmailPreference.DailyDigest ?
						NotificationEventFrequency.Daily :
						Post.Email == AlertEmailPreference.WeeklyDigest ?
							NotificationEventFrequency.Weekly :
							NotificationEventFrequency.Never,
				ReplyViaEmail = Reply.Email == AlertEmailPreference.Immediately,
				ReplyViaExtension = Reply.Extension,
				ReplyViaPush = Reply.Push,
				ReplyDigestViaEmail = Reply.Email == AlertEmailPreference.DailyDigest ?
						NotificationEventFrequency.Daily :
						Reply.Email == AlertEmailPreference.WeeklyDigest ?
							NotificationEventFrequency.Weekly :
							NotificationEventFrequency.Never,
				LoopbackViaEmail = Loopback.Email == AlertEmailPreference.Immediately,
				LoopbackViaExtension = Loopback.Extension,
				LoopbackViaPush = Loopback.Push,
				LoopbackDigestViaEmail = Loopback.Email == AlertEmailPreference.DailyDigest ?
						NotificationEventFrequency.Daily :
						Loopback.Email == AlertEmailPreference.WeeklyDigest ?
							NotificationEventFrequency.Weekly :
							NotificationEventFrequency.Never,
				FollowerViaEmail = Follower.Email == AlertEmailPreference.Immediately,
				FollowerViaExtension = Follower.Extension,
				FollowerViaPush = Follower.Push,
				FollowerDigestViaEmail = Follower.Email == AlertEmailPreference.DailyDigest ?
						NotificationEventFrequency.Daily :
						Follower.Email == AlertEmailPreference.WeeklyDigest ?
							NotificationEventFrequency.Weekly :
							NotificationEventFrequency.Never
			};
		}
	}
}