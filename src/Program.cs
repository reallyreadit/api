using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace api {
	public class Program {
		public static void Main(string[] args) {
			new WebHostBuilder()
				.UseKestrel()
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseConfiguration(new ConfigurationBuilder()
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("hosting.json")
					.Build())
				.UseStartup<Startup>()
				.Build()
				.Run();
		}
	}
}