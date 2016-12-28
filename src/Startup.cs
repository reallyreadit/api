using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using api.ActionFilters;

namespace api
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services) {
            services.AddMvc(options => {
                options.Filters.Add(new DelayActionFilter(500));
                options.Filters.Add(new LogActionFilter());
            });
        }
        public void Configure(IApplicationBuilder app/*, ILoggerFactory loggerFactory*/) {
            app.UseDeveloperExceptionPage();
            app.UseCors(cors => cors
                .AllowCredentials()
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod());
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{slug?}");
            });
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        }
    }
}