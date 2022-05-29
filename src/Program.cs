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
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace api {
	public class Program {
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
								optional: true,
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