using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using api.ActionFilters;

namespace api
{
    public class Startup
    {
        public static string DbConnectionString { get; private set;}
        public static string CookieDomain { get; private set; }
        public void ConfigureDevelopmentServices(IServiceCollection services) {
            services.AddMvc(options => {
                options.Filters.Add(new DelayActionFilter(500));
                options.Filters.Add(new LogActionFilter());
            });
        }
        public void ConfigureStagingServices(IServiceCollection services) {
            services.AddMvc(options => {
                options.Filters.Add(new LogActionFilter());
            });
        }
        public void ConfigureDevelopment(IApplicationBuilder app) {
            app.UseDeveloperExceptionPage();
            app.UseCors(cors => cors
                .WithOrigins(
                    "http://dev.reallyread.it",
                    // jeff dev
                    "chrome-extension://ibdjhkiiiiifdgmdalkofacfnihpomkn",
                    // jeff stage
                    "chrome-extension://llgoboocmmlfigcihicbkhkgbadjaeeh",
                    // bill stage
                    "chrome-extension://dffjnmdjeoihleodhcjlndjpfkhoiknh"
                )
                .AllowCredentials()
                .AllowAnyHeader()
                .AllowAnyMethod());
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{slug?}");
            });
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            DbConnectionString = "Host=localhost;Username=postgres;Password=postgres;Database=rrit";
            CookieDomain = "dev.reallyread.it";
        }
        public void ConfigureStaging(IApplicationBuilder app) {
            app.UseDeveloperExceptionPage();
            app.UseCors(cors => cors
                .WithOrigins(
                    "https://beta.reallyread.it",
                    // jeff dev
                    "chrome-extension://ibdjhkiiiiifdgmdalkofacfnihpomkn",
                    // jeff stage
                    "chrome-extension://llgoboocmmlfigcihicbkhkgbadjaeeh",
                    // bill stage
                    "chrome-extension://dffjnmdjeoihleodhcjlndjpfkhoiknh"
                )
                .AllowCredentials()
                .AllowAnyHeader()
                .AllowAnyMethod());
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{slug?}");
            });
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            DbConnectionString = "Host=reallyreadit.ch8jfpdyappi.us-east-2.rds.amazonaws.com;Username=rrit;Password=6uLrDpCQoPgu8U8e;Database=rrit";
            CookieDomain = "beta.reallyread.it";
        }
    }
}