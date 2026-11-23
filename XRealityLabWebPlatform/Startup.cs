using Application.Security;
using XRealityLab.AuthorizationRequirements;
using XRealityLab.Data;
using XRealityLab.Security;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Wkhtmltopdf.NetCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Common.Constants;
using XRealityLab.Setup;

namespace ARIWebPlatform
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            // Storage and mail settings come from appsettings / environment variables.
            PlatformSetup.LoadSettings(configuration, environment.ContentRootPath);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string connection = Configuration.GetConnectionString("xrealitylabdb");
            if (string.IsNullOrWhiteSpace(connection))
                throw new InvalidOperationException("Connection string 'xrealitylabdb' is missing. Set it in appsettings.json or in the environment variable ConnectionStrings__xrealitylabdb.");

            // Keep the encryption keys (login cookie, pass tokens in URLs) across restarts when a folder is configured.
            string keysPath = Configuration["DataProtection:KeysPath"];
            if (!string.IsNullOrWhiteSpace(keysPath))
            {
                Directory.CreateDirectory(keysPath);
                services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath)).SetApplicationName("XRealityLab");
            }

            // Needed when the platform runs behind a reverse proxy (Caddy, nginx, IIS ARR).
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            services.AddDbContext<XRealityLabdbContext>(options => options.UseSqlServer(connection));

            services.AddDbContext<AppDbContext>(config => {
                config.UseInMemoryDatabase("Memory");
            });

            //AddIdentity registers the services
            services.AddIdentity<IdentityUser, IdentityRole>(config =>
            {
                config.Password.RequiredLength = 4;
                config.Password.RequireDigit = false;
                config.Password.RequireNonAlphanumeric = false;
                config.Password.RequireUppercase = false;
                config.SignIn.RequireConfirmedEmail = true;
            })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(config =>
            {
                config.Cookie.Name = "XRealityLab.Cookie";
                config.LoginPath = "/Auth/Login";
                config.AccessDeniedPath = "/Auth/Index";
            });

            services.AddAuthorization(config =>
            {
                config.AddPolicy("Claim.Role", policyBuilder =>
                {
                    policyBuilder.RequireCustomClaim(ClaimTypes.Role);
                });
            });

            services.AddScoped<IAuthorizationHandler, CustomRequireClaimHandler>();

            services.AddTransient<GoogleReCAPTCHAService>();

            services.AddMvc(options =>
              options.EnableEndpointRouting = false)
              .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            services.AddSingleton<UniqueCode>();
            services.AddSingleton<CustomIDataProtection>();

            services.AddSession(options =>
            {
                // Set session timeout value
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
            });

            services.Configure<ReCAPTCHASettings>(Configuration.GetSection("GoogleReCAPTCHA"));

            services.AddControllersWithViews().AddRazorRuntimeCompilation();//ONLY FOR THE DEVELOPMENT (addrazorRuntimeCompilation)
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Auth/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            // Set "HttpsRedirection": false when a reverse proxy already terminates HTTPS.
            if (!string.Equals(Configuration["HttpsRedirection"], "false", StringComparison.OrdinalIgnoreCase))
                app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Folders the upload / export features write to (not kept in git).
            Directory.CreateDirectory(Path.Combine(env.WebRootPath, "research-files", "for-upload"));
            Directory.CreateDirectory(Path.Combine(env.WebRootPath, "research-files", "for-download"));
            foreach (var folder in new[] { "organization-photos", "lab-photos", "project-photos", "user-photos" })
                Directory.CreateDirectory(Path.Combine(env.WebRootPath, "_images", folder));

            // Local file storage (used when no Azure storage account is configured) is served under /files.
            if (!AzureGPV2Credentials.UseAzure)
            {
                Directory.CreateDirectory(AzureGPV2Credentials.LOCAL_STORAGE_ROOT);
                PlatformSetup.SeedLocalStockIcons(env.WebRootPath);
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(AzureGPV2Credentials.LOCAL_STORAGE_ROOT),
                    RequestPath = "/files"
                });
            }

            // Creates the schema, the lookup rows and the first Administrator on a fresh installation (Database:AutoCreate).
            using (var scope = app.ApplicationServices.CreateScope())
            {
                PlatformSetup.PrepareDatabase(scope.ServiceProvider.GetRequiredService<XRealityLabdbContext>(), Configuration);
            }

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseMvcWithDefaultRoute();
            app.UseRouting();

            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Auth}/{action=Index}/{id?}");
            });
        }
    }
}
