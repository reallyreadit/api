using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using api.ActionFilters;
using Npgsql;
using api.DataAccess.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Authorization;
using api.Configuration;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using MyAuthenticationOptions = api.Configuration.AuthenticationOptions;
using MyDataProtectionOptions = api.Configuration.DataProtectionOptions;
using Microsoft.Extensions.Options;
using api.Messaging;
using Mvc.RenderViewToString;
using Microsoft.AspNetCore.Mvc.Razor;
using System;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Serilog;
using api.Security;
using api.ReadingVerification;
using api.Encryption;
using Microsoft.AspNetCore.Http;

namespace api {
	public class Startup {
		private IHostingEnvironment env;
		private IConfiguration config;
		public Startup(IHostingEnvironment env) {
			// set the IHostingEnvironment
			this.env = env;
			// read configuration
			var currentDirectory = Directory.GetCurrentDirectory();
			var config = new ConfigurationBuilder()
				.SetBasePath(currentDirectory)
				.AddJsonFile("appsettings.json");
			var envConfigFile = $"appsettings.{env.EnvironmentName}.json";
			if (File.Exists(Path.Combine(currentDirectory, envConfigFile))) {
				config.AddJsonFile(envConfigFile);
			}
			this.config = config.Build();
			// create logger
			if (env.IsDevelopment()) {
				Log.Logger = new LoggerConfiguration()
					.WriteTo
					.Console()
					.CreateLogger();
			} else if (env.IsProduction()) {
				Log.Logger = new LoggerConfiguration()
					.MinimumLevel
					.Error()
					.WriteTo
					.File(
						path: Path.Combine("logs", "app.txt"),
						rollingInterval: RollingInterval.Day
					)
					.CreateLogger();
			} else {
				throw new ArgumentException("Unexpected environment");
			}
		}
		public void ConfigureServices(IServiceCollection services) {
			// configure options
			var authOptsConfigSection = config.GetSection("Authentication");
			var authOpts = authOptsConfigSection.Get<MyAuthenticationOptions>();
			services
				.Configure<MyAuthenticationOptions>(authOptsConfigSection)
				.Configure<CaptchaOptions>(config.GetSection("Captcha"))
				.Configure<CorsOptions>(config.GetSection("Cors"))
				.Configure<DatabaseOptions>(config.GetSection("Database"))
				.Configure<EmailOptions>(config.GetSection("Email"))
				.Configure<HashidsOptions>(config.GetSection("Hashids"))
				.Configure<RazorViewEngineOptions>(x => x.ViewLocationFormats.Add("/src/Messaging/Views/{0}.cshtml"))
				.Configure<ReadingVerificationOptions>(config.GetSection("ReadingVerification"))
				.Configure<ServiceEndpointsOptions>(config.GetSection("ServiceEndpoints"));
			// configure services
			services
				.AddScoped<CaptchaService>()
				.AddScoped<EmailService>()
				.AddScoped<ObfuscationService>()
				.AddTransient<RazorViewToStringRenderer>()
				.AddScoped<ReadingVerificationService>();
			// configure authentication
			services
				.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
				.AddCookie(
					options => {
						options.Cookie.Domain = authOpts.CookieDomain;
						options.Cookie.Name = authOpts.CookieName;
						options.Cookie.SecurePolicy = authOpts.CookieSecure;
						options.Cookie.Expiration = TimeSpan.FromDays(180);
						options.Cookie.HttpOnly = true;
						options.Cookie.SameSite = SameSiteMode.None;
						options.ExpireTimeSpan = TimeSpan.FromDays(180);
						options.SlidingExpiration = true;
						options.Events.OnRedirectToLogin = context => {
							context.Response.StatusCode = 401;
							return Task.CompletedTask;
						};
						options.Events.OnRedirectToAccessDenied = context => {
							context.Response.StatusCode = 403;
							return Task.CompletedTask;
						};
					}
				);
			// configure shared key ring in production
			if (env.IsProduction()) {
				var dataProtectionOptions = new MyDataProtectionOptions();
				config.GetSection("DataProtection").Bind(dataProtectionOptions);
				services
					.AddDataProtection()
					.SetApplicationName(dataProtectionOptions.ApplicationName)
					.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionOptions.KeyPath));
			}
			// configure MVC and global filters
			services.AddMvc(options => {
				options.Filters.Add(new AuthorizeFilter(
					policy: new AuthorizationPolicyBuilder()
						.RequireAuthenticatedUser()
						.Build()
				));
				if (env.IsDevelopment()) {
					options.Filters.Add(new DelayActionFilter(500));
				}
			});
		}
		public void Configure(
			IApplicationBuilder app,
			IOptions<CorsOptions> corsOpts,
			ILoggerFactory loggerFactory
		) {
			// use dev exception page until the db connection leak issue is solved and we have reliable logging
			app.UseDeveloperExceptionPage();
			// configure ILoggerFactory
			loggerFactory.AddSerilog();
			// configure forwarded headers
			app.UseForwardedHeaders(new ForwardedHeadersOptions() {
				RequireHeaderSymmetry = false
			});
			// configure cors
			app.UseCors(
				cors => cors
					.WithOrigins(corsOpts.Value.Origins)
					.AllowCredentials()
					.AllowAnyHeader()
					.AllowAnyMethod()
					.SetPreflightMaxAge(TimeSpan.FromDays(1))
				);
			// configure cookie authentication
			app.UseAuthentication();
			// configure routes
			app.UseMvcWithDefaultRoute();
			// configure Npgsql
			NpgsqlConnection.MapEnumGlobally<SourceRuleAction>();
			NpgsqlConnection.MapEnumGlobally<UserAccountRole>();
			NpgsqlConnection.MapCompositeGlobally<Ranking>();
			NpgsqlConnection.MapCompositeGlobally<StreakRanking>();
			// configure Dapper
			Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
		}
	}
}