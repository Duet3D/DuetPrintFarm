using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace DuetPrintFarm
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            // Make sure the job directory exists
            string gcodesDirectory = configuration.GetValue<string>("gcodesDirectory");
            if (!Directory.Exists(gcodesDirectory))
            {
                Directory.CreateDirectory(gcodesDirectory);
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
#if DEBUG
            services.AddCors(options => options.AddDefaultPolicy(configurePolicy => configurePolicy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
#endif
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Use default files
            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Use default routing and CORS in dev mode
            app.UseRouting();
#if DEBUG
            app.UseCors();
#endif

            // Define endpoints
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=PrintFarm}");
                endpoints.MapControllerRoute("default", "{controller=Machine}");
            });
        }
    }
}
