using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace api {
	public class Program {
		private static readonly HttpClient httpClient = new HttpClient();
		public static HttpClient HttpClient => httpClient;
		public static void Main(string[] args) {
			new HostBuilder()
				.UseContentRoot(Directory.GetCurrentDirectory())
				.ConfigureAppConfiguration(
					(context, config) => {
						config
							.AddJsonFile(
								path: "appsettings.json",
								optional: false,
								reloadOnChange: true
							)
							.AddJsonFile(
								path: $"appsettings.{context.HostingEnvironment.EnvironmentName}.json",
								optional: false,
								reloadOnChange: true
							);
					}
				)
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
				.ConfigureWebHostDefaults(
					webBuilder => {
						webBuilder
							.UseConfiguration(
								new ConfigurationBuilder()
									.SetBasePath(Directory.GetCurrentDirectory())
									.AddJsonFile("hostsettings.json")
									.Build()
							)
							.UseKestrel()
							.UseIIS()
							.UseStartup<Startup>();
					}
				)
				.Build()
				.Run();
		}
	}
}