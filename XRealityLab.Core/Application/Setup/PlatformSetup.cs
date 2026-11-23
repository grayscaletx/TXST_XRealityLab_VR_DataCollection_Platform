using Common;
using Common.Constants;
using Infrastructure.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using XRealityLab.Common;

namespace XRealityLab.Setup
{
    /// <summary>
    /// Start-up helpers shared by the web platform and the API:
    /// loads the Storage / Mail settings from configuration and prepares a fresh database.
    /// </summary>
    public static class PlatformSetup
    {
        /// <summary>Reads the "Storage" and "Mail" configuration sections into the shared settings classes.</summary>
        public static void LoadSettings(IConfiguration config, string contentRootPath)
        {
            // ---- Storage ----
            AzureGPV2Credentials.STORAGE_ACCOUNT_CONNECTION_STRING = config["Storage:AzureConnectionString"] ?? string.Empty;

            string localRoot = config["Storage:LocalPath"];
            if (string.IsNullOrWhiteSpace(localRoot)) localRoot = "App_Data/files";
            if (!Path.IsPathRooted(localRoot)) localRoot = Path.Combine(contentRootPath, localRoot);
            AzureGPV2Credentials.LOCAL_STORAGE_ROOT = localRoot;

            string publicUrl = config["Storage:PublicBaseUrl"];
            if (string.IsNullOrWhiteSpace(publicUrl)) publicUrl = "/files/";
            BaseLink.AZURE_BLOB_URL = publicUrl;

            // ---- Mail ----
            SendGridCredentials.PROVIDER = config["Mail:Provider"] ?? "None";
            SendGridCredentials.API_KEY = config["Mail:SendGridApiKey"] ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(config["Mail:FromEmail"])) SendGridCredentials.SET_FROM_EMAIL = config["Mail:FromEmail"];
            if (!string.IsNullOrWhiteSpace(config["Mail:FromName"])) SendGridCredentials.SET_FROM_NAME = config["Mail:FromName"];
            SendGridCredentials.SMTP_HOST = config["Mail:SmtpHost"] ?? string.Empty;
            if (int.TryParse(config["Mail:SmtpPort"], out int port)) SendGridCredentials.SMTP_PORT = port;
            SendGridCredentials.SMTP_USER = config["Mail:SmtpUser"] ?? string.Empty;
            SendGridCredentials.SMTP_PASSWORD = config["Mail:SmtpPassword"] ?? string.Empty;
            if (bool.TryParse(config["Mail:SmtpUseSsl"], out bool ssl)) SendGridCredentials.SMTP_USE_SSL = ssl;
            SendGridCredentials.PLATFORM_URL = config["PlatformUrl"] ?? string.Empty;
        }

        /// <summary>
        /// When local storage is used, copies the default (stock) icons that ship with the web platform
        /// into the storage folder so that new users / labs / projects get a picture.
        /// </summary>
        public static void SeedLocalStockIcons(string webRootPath)
        {
            if (AzureGPV2Credentials.UseAzure || string.IsNullOrEmpty(webRootPath)) return;

            var icons = new[]
            {
                new[] { AzureGPV2Credentials.CONTAINER_USER_ICONS, "user-stock-icon.png" },
                new[] { AzureGPV2Credentials.CONTAINER_USER_ICONS, "administrator-stock-icon.png" },
                new[] { AzureGPV2Credentials.CONTAINER_USER_ICONS, "director-it-stock-icon.png" },
                new[] { AzureGPV2Credentials.CONTAINER_USER_ICONS, "lab-director-stock-icon.png" },
                new[] { AzureGPV2Credentials.CONTAINER_USER_ICONS, "principle-investigator-stock-icon.png" },
                new[] { AzureGPV2Credentials.CONTAINER_USER_ICONS, "research-assistant-stock-icon.png" },
                new[] { AzureGPV2Credentials.CONTAINER_ORGANIZATION_ICONS, "organization-stock-icon.png" },
                new[] { AzureGPV2Credentials.CONTAINER_LAB_ICONS, "lab-stock-icon.png" },
                new[] { AzureGPV2Credentials.CONTAINER_PROJECT_ICONS, "project-stock-icon.png" },
            };

            foreach (var icon in icons)
            {
                try
                {
                    string source = Path.Combine(webRootPath, "_images", "stock", icon[1]);
                    string folder = Path.Combine(AzureGPV2Credentials.LOCAL_STORAGE_ROOT, icon[0]);
                    string target = Path.Combine(folder, icon[1]);
                    if (!File.Exists(source) || File.Exists(target)) continue;
                    Directory.CreateDirectory(folder);
                    File.Copy(source, target);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Setup] Could not copy stock icon " + icon[1] + ": " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Creates the database schema when it does not exist yet (Database:AutoCreate = true) and makes sure
        /// the fixed lookup rows (roles, research method types) and the configured Administrator exist.
        /// Safe to run on every start: existing data is never changed.
        /// </summary>
        public static void PrepareDatabase(XRealityLabdbContext context, IConfiguration config)
        {
            bool autoCreate = string.Equals(config["Database:AutoCreate"], "true", StringComparison.OrdinalIgnoreCase);
            if (!autoCreate) return;

            if (context.Database.EnsureCreated())
                Console.WriteLine("[Setup] Database schema created.");

            if (!context.Role.Any())
            {
                foreach (RoleType role in Enum.GetValues(typeof(RoleType)))
                    context.Role.Add(new Role { RoleId = (int)role, Type = role.GetEnumDescription() });
                context.SaveChanges();
                Console.WriteLine("[Setup] Roles inserted.");
            }

            if (!context.ResearchMethod.Any())
            {
                // Inserted one by one so the generated IDs follow the ResearchType enum (1, 2, 3).
                foreach (ResearchType type in Enum.GetValues(typeof(ResearchType)))
                {
                    context.ResearchMethod.Add(new ResearchMethod { TypeId = type.GetEnumDescription() });
                    context.SaveChanges();
                }
                Console.WriteLine("[Setup] Research method types inserted.");
            }

            string adminEmail = config["Seed:AdminEmail"];
            string adminPassword = config["Seed:AdminPassword"];
            // Created when no user with this e-mail exists yet, so an extra Administrator can be added
            // to an existing installation by changing the Seed settings and restarting.
            if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword)
                && !context.User.Any(k => k.Email == adminEmail.Trim()))
            {
                context.User.Add(new User
                {
                    IdentityId = Guid.NewGuid().ToString(),
                    Rank = (int)Rank.Mx,
                    Firstname = "Platform",
                    Lastname = "Administrator",
                    Email = adminEmail.Trim(),
                    PasswordHash = IdentityConversions.MD5Hash(adminPassword),
                    Telephone = string.Empty,
                    PhotoUrl = PhotoUrl.ADMINISTRATOR_ICON_PNG,
                    RoleId = (int)RoleType.Administrator,
                    HasChangedPassword = 1,
                    RegistrationDateTime = DateTime.Now
                });
                context.SaveChanges();
                Console.WriteLine("[Setup] Administrator created: " + adminEmail.Trim());
            }
        }
    }
}
