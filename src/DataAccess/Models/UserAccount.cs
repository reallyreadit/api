using System;
using System.Runtime.Serialization;

namespace api.DataAccess.Models {
	public class UserAccount {
		public Guid Id { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		[IgnoreDataMember]
		public byte[] PasswordHash { get; set; }
		[IgnoreDataMember]
		public byte[] PasswordSalt { get; set; }
	}
}