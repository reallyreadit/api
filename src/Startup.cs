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
using Microsoft.Extensions.Options;
using api.Messaging;
using api.DataAccess;
using Mvc.RenderViewToString;
using Microsoft.AspNetCore.Mvc.Razor;
using System;

namespace api {
	public class Startup {
		private IConfigurationRoot config;
		public Startup(IHostingEnvironment env) {
			config = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json")
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json")
				.Build();
		}
		public void ConfigureDevelopmentServices(IServiceCollection services) => ConfigureServices(services, new DelayActionFilter(500));
		public void ConfigureStagingServices(IServiceCollection services) => ConfigureServices(services);
		public void ConfigureProductionServices(IServiceCollection services) => ConfigureServices(services);
		private void ConfigureServices(IServiceCollection services, params IFilterMetadata[] filters) {
			services
				.Configure<MyAuthenticationOptions>(config.GetSection("Authentication"))
				.Configure<CorsOptions>(config.GetSection("Cors"))
				.Configure<DatabaseOptions>(config.GetSection("Database"))
				.Configure<EmailOptions>(config.GetSection("Email"))
				.Configure<RazorViewEngineOptions>(x => x.ViewLocationFormats.Add("/src/Messaging/Views/{0}.cshtml"))
				.Configure<ServiceEndpointsOptions>(config.GetSection("ServiceEndpoints"));
			services
				.AddScoped<DbConnection>()
				.AddScoped<EmailService>()
				.AddTransient<RazorViewToStringRenderer>();
			services.AddMvc(options => {
				options.Filters.Add(new LogActionFilter());
				options.Filters.Add(new AuthorizeFilter(
					policy: new AuthorizationPolicyBuilder()
						.RequireAuthenticatedUser()
						.Build()
				));
				foreach (var filter in filters) {
					options.Filters.Add(filter);
				}
			});
		}
		public void Configure(IApplicationBuilder app, IOptions<MyAuthenticationOptions> authOpts, IOptions<CorsOptions> corsOpts) {
			app.UseDeveloperExceptionPage();
			app.UseCors(cors => cors
				.WithOrigins(corsOpts.Value.Origins)
				.AllowCredentials()
				.AllowAnyHeader()
				.AllowAnyMethod()
				.WithExposedHeaders(corsOpts.Value.ExposedHeaders));
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
			app.UseMvcWithDefaultRoute();

			Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
			NpgsqlConnection.MapCompositeGlobally<CreateArticleAuthor>();
		}
	}
}