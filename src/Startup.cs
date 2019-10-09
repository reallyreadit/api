using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using api.ActionFilters;
using Npgsql;
using api.DataAccess.Models;
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
using api.Security;
using api.ReadingVerification;
using api.Encryption;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace api {
	public class Startup {
		private IHostEnvironment env;
		private IConfiguration config;
		public Startup(
			IHostEnvironment env,
			IConfiguration config
		) {
			this.env = env;
			this.config = config;
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
			if (env.IsDevelopment()) {
				services.AddMvc(
					options => {
							options.Filters.Add(new DelayActionFilter(500));
					}
				);
			}
		}
		public void Configure(
			IApplicationBuilder app,
			IOptions<CorsOptions> corsOpts
		) {
			// use dev exception page until the db connection leak issue is solved and we have reliable logging
			app.UseDeveloperExceptionPage();
			// configure forwarded headers
			app.UseForwardedHeaders(new ForwardedHeadersOptions() {
				RequireHeaderSymmetry = false
			});
			// configure routing
			app.UseRouting();
			// configure cors
			app.UseCors(
				cors => cors
					.WithOrigins(corsOpts.Value.Origins)
					.AllowCredentials()
					.AllowAnyHeader()
					.AllowAnyMethod()
					.SetPreflightMaxAge(TimeSpan.FromDays(1))
			);
			// configure cookie authentication & authorization
			app.UseAuthentication();
			app.UseAuthorization();
			// configure mvc
			app.UseEndpoints(
				endpoints => {
					endpoints.MapControllerRoute(
						name: "default",
						pattern: "{controller=Home}/{action=Index}/{id?}"
					);
				}
			);
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