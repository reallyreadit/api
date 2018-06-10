using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace api {
	public class Program {
		private static readonly HttpClient httpClient = new HttpClient();
		public static HttpClient HttpClient => httpClient;
		public static void Main(string[] args) {
			new WebHostBuilder()
				.UseKestrel()
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseConfiguration(new ConfigurationBuilder()
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("hosting.json")
					.Build())
				.UseIISIntegration()
				.UseStartup<Startup>()
				.Build()
				.Run();
		}
	}
}