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

namespace api
{
	public class Startup
	{
		public static string AuthenticationScheme => "rrit-auth-scheme";
		public static string DbConnectionString { get; private set;}
		public void ConfigureDevelopmentServices(IServiceCollection services) {
			ConfigureServices(services, new DelayActionFilter(500));
		}
		public void ConfigureStagingServices(IServiceCollection services) {
			ConfigureServices(services);
		}
		private void ConfigureServices(IServiceCollection services, params IFilterMetadata[] filters) {
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
		public void ConfigureDevelopment(IApplicationBuilder app) {
			Configure(
				app: app,
				origin: "http://dev.reallyread.it",
				dbConnectionString: "Host=localhost;Username=postgres;Password=postgres;Database=rrit",
				cookieDomain: "dev.reallyread.it"
			);
		}
		public void ConfigureStaging(IApplicationBuilder app) {
			Configure(
				app: app,
				origin: "https://beta.reallyread.it",
				dbConnectionString: "Host=reallyreadit.ch8jfpdyappi.us-east-2.rds.amazonaws.com;Username=rrit;Password=6uLrDpCQoPgu8U8e;Database=rrit",
				cookieDomain: "beta.reallyread.it"
			);
		}
		private void Configure(IApplicationBuilder app, string origin, string dbConnectionString, string cookieDomain) {
			app.UseDeveloperExceptionPage();
			app.UseCors(cors => cors
				.WithOrigins(
					origin,
					// jeff dev
					"chrome-extension://ibdjhkiiiiifdgmdalkofacfnihpomkn",
					// jeff stage
					"chrome-extension://llgoboocmmlfigcihicbkhkgbadjaeeh",
					// bill stage
					"chrome-extension://dffjnmdjeoihleodhcjlndjpfkhoiknh",
					// chrome store dev
					"chrome-extension://lmkbmgmeghekjbjobpgpidcjnkkoajee",
					// chrome store stage
					"chrome-extension://mkeiglkfdfamdjehidenkklibndmljfi",
					// asus dev
					"chrome-extension://jgdmbgabdmlamnmfdedjeflbhlfmbmkp"
				)
				.AllowCredentials()
				.AllowAnyHeader()
				.AllowAnyMethod()
				.WithExposedHeaders("Content-Length"));
			app.UseCookieAuthentication(new CookieAuthenticationOptions() {
				AuthenticationScheme = AuthenticationScheme,
				AutomaticAuthenticate = true,
				AutomaticChallenge = true,
				CookieDomain = cookieDomain,
				CookieName = "sessionKey",
				Events = new CookieAuthenticationEvents() {
					OnRedirectToLogin = context => {
						context.Response.StatusCode = 401;
						return Task.CompletedTask;
					},
					OnRedirectToAccessDenied = context => {
						context.Response.StatusCode = 403;
						return Task.CompletedTask;
					}
				}
			});
			app.UseMvc(routes => {
				routes.MapRoute(
					name: "default",
					template: "{controller=Home}/{action=Index}/{slug?}"
				);
			});

			Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
			DbConnectionString = dbConnectionString;
			NpgsqlConnection.MapCompositeGlobally<CreateArticleAuthor>();
		}
	}
}