using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace api {
	public class Program {
		private static readonly HttpClient httpClient = new HttpClient();
		public static HttpClient HttpClient => httpClient;
		public static void Main(string[] args) {
			new WebHostBuilder()
				.ConfigureLogging(
					(context, logging) => {
						if (context.HostingEnvironment.IsDevelopment()) {
							logging.AddSerilog(
								new LoggerConfiguration()
									.WriteTo
									.Console()
									.CreateLogger()
							);
						} else if (context.HostingEnvironment.IsProduction()) {
							logging.AddSerilog(
								new LoggerConfiguration()
									.MinimumLevel
									.Error()
									.WriteTo
									.File(
										path: Path.Combine("logs", "app.txt"),
										rollingInterval: RollingInterval.Day
									)
									.CreateLogger()
							);
						} else {
							throw new ArgumentException("Unexpected environment");
						}
					}
				)
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