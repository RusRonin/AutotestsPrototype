using Core.Chrome;
using Core.HtmlStorage;
using Core.RemoteDebuggerPortManager;
using Core.Screenshot;
using Core.Awaiters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Backend", Version = "v1" });
            });

            services.AddSingleton<IRemoteDebuggerPortManager, RemoteDebuggerPortManager>();
            services.AddSingleton<IScreenshotTaker, ChromeScreenshotTaker>();

            services.AddScoped<ChromeProcess>();
            services.AddScoped<IHtmlStorage, FileSystemHtmlStorage>();

            services.AddTransient<IDomLoadedAwaiter, DomLoadedAwaiter>();
            services.AddTransient<IScriptExecutionCompletedAwaiter, ScriptExecutionCompletedAwaiter>();
            services.AddTransient<IPageLoadedAwaiter, PageLoadedAwaiter>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Backend v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
