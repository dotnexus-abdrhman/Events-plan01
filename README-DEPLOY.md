# MinaEvents – Quick Deploy Guide (safe, no code changes)

This guide lets you deploy the app without changing existing behavior. We only add templates and instructions.

## 1) Build and publish (Release)

- Prerequisites: .NET 8 SDK on your machine; .NET 8 Runtime on the server

```bash
# run on your dev machine
dotnet restore
dotnet test
dotnet publish -c Release -o ./publish
```

The output folder `publish/` is your deployable artifact. Zip it and upload to the server.

## 2) Production settings

- Copy `RourtPPl01/appsettings.Production.json.template` to `RourtPPl01/appsettings.Production.json`
- Fill the connection string for your PRODUCTION SQL Server (or set via env var `ConnectionStrings__Default` on the server)
- Set environment on server: `ASPNETCORE_ENVIRONMENT=Production`

Example `appsettings.Production.json`:
```json
{
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": "Server=HOST;Database=DB;User Id=USER;Password=PASS;TrustServerCertificate=True;"
  },
  "Logging": {"LogLevel": {"Default": "Information", "Microsoft": "Warning"}}
}
```

## 3) Database (choose one)

- Option A (recommended): Run EF Core migrations on the production DB before first run:
```bash
# On a machine with dotnet-ef and access to the DB
dotnet tool install --global dotnet-ef
cd RourtPPl01
DOTNET_ENVIRONMENT=Production dotnet ef database update
```
- Option B: Restore a SQL backup provided by you, then (optionally) run migrations if needed.

## 4) Static files and uploads

- Create the uploads folder and give write permissions to the app user:
```bash
# Linux example
mkdir -p /var/www/mina/wwwroot/uploads
chown -R www-data:www-data /var/www/mina/wwwroot/uploads
chmod -R 775 /var/www/mina/wwwroot/uploads
```
- Put Arabic fonts (Cairo / Noto Kufi / Tajawal) into `wwwroot/fonts` to enable professional Arabic in PDFs

## 5) Deploy options

Pick one environment and follow its example file in `deploy/`:

### A) Windows / IIS
- Install ".NET 8 Hosting Bundle" on the server
- Create a site in IIS pointing to the `publish/` folder
- Ensure HTTPS is configured
- See: `deploy/iis/web.config.example`

### B) Linux (Ubuntu) + Nginx + systemd
- Copy `publish/` to e.g. `/var/www/mina`
- Use `deploy/linux/mina.service.example` (copy to `/etc/systemd/system/mina.service`, adjust paths)
- Use `deploy/linux/nginx.conf.example` (include in Nginx site, adjust domain + SSL)
- Forwarded headers behind proxy: see `deploy/snippets/ForwardedHeaders.cs.snippet`

### C) Docker
- Use `deploy/docker/Dockerfile.example` to build an image from the publish output

## 6) Health and logs
- Optional health endpoints (snippet only): `deploy/snippets/HealthChecks.cs.snippet`
- Logs go to console by default; with IIS, check `stdoutLogFile` if enabled; with Linux/systemd: `journalctl -u <service> -f`

## Notes specific to this project
- No runtime code behavior changed by these templates.
- User deletion uses soft-delete fallback when related data exists.
- Event builder attachment picker was fixed; no extra setup needed for production.


## 7) Quick Start (very short)
- Linux service:
```bash
sudo systemctl daemon-reload
sudo systemctl enable mina --now
journalctl -u mina -f
```
- Reverse proxy (Nginx):
```bash
sudo nginx -t && sudo systemctl reload nginx
# then obtain SSL with Let's Encrypt (example)
sudo certbot --nginx -d example.com
```
- All files in `deploy/` are examples only and not auto-enabled. Apply and adapt on the server.

## Note: QuestPDF Native Assets for old GLIBC (Linux)
- Added NativeAssets compatible with older glibc to avoid QuestPDF runtime issues on Linux servers (e.g., glibc 2.28): SkiaSharp.NativeAssets.Linux.NoDependencies (2.88.3) and HarfBuzzSharp.NativeAssets.Linux (2.8.2).
- Build remains Self-Contained and without SingleFile to ensure native .so libraries are present in publish/.

