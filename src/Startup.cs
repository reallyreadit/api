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
using api.DataAccess;
using Mvc.RenderViewToString;
using Microsoft.AspNetCore.Mvc.Razor;
using System;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Serilog;
using api.Security;
using api.Authentication;
using System.Linq;
using System.Security.Claims;

namespace api {
	public class Startup {
		private IHostingEnvironment env;
		private IConfigurationRoot config;
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
			services
				.Configure<MyAuthenticationOptions>(config.GetSection("Authentication"))
				.Configure<CaptchaOptions>(config.GetSection("Captcha"))
				.Configure<CorsOptions>(config.GetSection("Cors"))
				.Configure<DatabaseOptions>(config.GetSection("Database"))
				.Configure<EmailOptions>(config.GetSection("Email"))
				.Configure<RazorViewEngineOptions>(x => x.ViewLocationFormats.Add("/src/Messaging/Views/{0}.cshtml"))
				.Configure<ServiceEndpointsOptions>(config.GetSection("ServiceEndpoints"));
			// configure services
			services
				.AddScoped<CaptchaService>()
				.AddScoped<EmailService>()
				.AddTransient<RazorViewToStringRenderer>();
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
			IOptions<MyAuthenticationOptions> authOpts,
			IOptions<CorsOptions> corsOpts,
			IOptions<DatabaseOptions> databaseOpts,
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
			app.UseCors(cors => cors
				.WithOrigins(corsOpts.Value.Origins)
				.AllowCredentials()
				.AllowAnyHeader()
				.AllowAnyMethod());
			// configure cookie authentication
			app.UseCookieAuthentication(new CookieAuthenticationOptions() {
				AuthenticationScheme = authOpts.Value.Scheme,
				AutomaticAuthenticate = true,
				AutomaticChallenge = true,
				CookieDomain = authOpts.Value.CookieDomain,
				CookieName = authOpts.Value.CookieName,
				CookieSecure = authOpts.Value.CookieSecure,
				Events = new CookieAuthenticationEvents() {
					OnRedirectToLogin = context => {
						context.Response.StatusCode = 401;
						return Task.CompletedTask;
					},
					OnRedirectToAccessDenied = context => {
						context.Response.StatusCode = 403;
						return Task.CompletedTask;
					}
				},
				ExpireTimeSpan = TimeSpan.FromDays(7),
				SlidingExpiration = true
			});
			// configure routes
			app.UseMvcWithDefaultRoute();
			// configure Npgsql
			NpgsqlConnection.MapEnumGlobally<SourceRuleAction>();
			NpgsqlConnection.MapEnumGlobally<UserAccountRole>();
			// configure Dapper
			Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
		}
	}
}