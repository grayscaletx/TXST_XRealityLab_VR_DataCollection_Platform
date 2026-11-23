using Application.Security;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.HttpOverrides;
using XRealityLab.Setup;

namespace XRealityLabAPI
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
            // Same database as the web platform. "xrealitylab" is still accepted for older configuration files.
            string connection = Configuration.GetConnectionString("xrealitylabdb");
            if (string.IsNullOrWhiteSpace(connection)) connection = Configuration.GetConnectionString("xrealitylab");
            if (string.IsNullOrWhiteSpace(connection))
                throw new InvalidOperationException("Connection string 'xrealitylabdb' is missing. Set it in appsettings.json or in the environment variable ConnectionStrings__xrealitylabdb.");

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            services.AddDbContext<XRealityLabdbContext>(options => options.UseSqlServer(connection));

            //Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Version = "0.9", Description = "XRealityLabAPI" });
            });

            services.AddMvc(options =>
             options.EnableEndpointRouting = false)
             .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddSingleton<UniqueCode>();
            services.AddSingleton<CustomIDataProtection>();

            services.AddControllers().AddNewtonsoftJson(options =>
                options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (!string.Equals(Configuration["HttpsRedirection"], "false", StringComparison.OrdinalIgnoreCase))
                app.UseHttpsRedirection();

            using (var scope = app.ApplicationServices.CreateScope())
            {
                PlatformSetup.PrepareDatabase(scope.ServiceProvider.GetRequiredService<XRealityLabdbContext>(), Configuration);
            }
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "XRealityLabAPI V0.9");
                c.RoutePrefix = string.Empty;
            });
        }
    }
}
