# XRealityLab

A web platform for research labs that run studies with participants in virtual and augmented reality.
Researchers plan a study in the browser, register participants, and the VR / AR application sends each
participant's results to the platform over an API.

**The full guide is in [docs/XRealityLab-Guide.pdf](docs/XRealityLab-Guide.pdf)**: roles, a study from start to
finish, connecting Unity and Qualtrics, the API, installation and configuration.

## What is in this repository

| Folder | Content |
| --- | --- |
| `XRealityLabWebPlatform` | The website (ASP.NET Core 3.1 MVC) |
| `XRealityLabAPI` | The API used by Unity applications and questionnaires |
| `XRealityLab.Core` | Shared models, settings and database preparation |
| `XRealityLab.Infrastructure` | Database tables (Entity Framework Core, SQL Server) |
| `XRealityLab.Service` | File storage and e-mail |
| `docs` | The guide and a ready-made Unity script (`docs/unity/XRealityLabClient.cs`) |
| `docker`, `docker-compose.yml` | Docker set-up (provided, not yet verified on a server) |

## Quick start on Windows

You need the [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet/3.1) and SQL Server
(Express or LocalDB).

```powershell
$env:ConnectionStrings__xrealitylabdb = "Server=(localdb)\MSSQLLocalDB;Database=XRealityLabdb;Trusted_Connection=True"
$env:Database__AutoCreate = "true"
$env:Seed__AdminEmail     = "you@your-university.edu"
$env:Seed__AdminPassword  = "choose-a-password"
$env:ASPNETCORE_URLS      = "http://localhost:5080"
$env:HttpsRedirection     = "false"
cd XRealityLabWebPlatform
dotnet run --no-launch-profile
```

Open <http://localhost:5080> and log in with the e-mail and password you chose. The first start creates the
database tables and the first Administrator. Without a mail provider, invitation e-mails are written to the
console window.

Start the API the same way from `XRealityLabAPI` on another port (for example `http://localhost:5081`); it only
needs the connection string. Its root address shows a test page for every call.

## Configuration

No password or key is stored in the source code. Every setting can be given in `appsettings.json` or as an
environment variable (`Mail:Provider` becomes `Mail__Provider`). The guide lists all of them; the main ones:

| Setting | Meaning |
| --- | --- |
| `ConnectionStrings:xrealitylabdb` | SQL Server connection string (web platform and API) |
| `Database:AutoCreate` | `true` prepares an empty database on start |
| `Seed:AdminEmail`, `Seed:AdminPassword` | Administrator account, created on start when that e-mail does not exist yet |
| `Storage:AzureConnectionString`, `Storage:PublicBaseUrl` | Azure Storage; leave empty to store files on the server |
| `Mail:Provider` | `Webhook`, `SendGrid`, `Smtp` or `None` |
| `GoogleReCAPTCHA:*` | Optional reCAPTCHA v3 keys |

## Known limitations

Passwords are stored as MD5 hashes, the API log in carries the password in the address, and the project targets
.NET Core 3.1, which Microsoft no longer supports. See chapter 11 of the guide before using the platform for
sensitive data.
