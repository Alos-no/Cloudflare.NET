Cloudflare.NET.SampleCoreConsole

This sample shows how to:
- Bootstrap the Cloudflare.NET REST client with the Generic Host
- Create a DNS CNAME record
- Find a DNS record by name
- Enumerate DNS records with automatic pagination
- Export DNS records (BIND)
- Clean up created resources

Configuration
- appsettings.json is included. You can also use environment variables or User Secrets.
  - Cloudflare:ApiToken
  - Cloudflare:AccountId
  - Cloudflare:ZoneId

Run
> dotnet run

Notes
- This sample creates a temporary CNAME record and deletes it at the end.
- Exported BIND data is printed partially (first 200 chars) for demonstration only.