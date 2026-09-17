# Setup Steps

## 1. Install NuGet packages
```
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.Authentication.Google
dotnet add package Twilio
```

## 2. appsettings.json additions
```json
{
  "Twilio": {
    "AccountSid": "your-twilio-account-sid",
    "AuthToken": "your-twilio-auth-token",
    "FromNumber": "+1xxxxxxxxxx"
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    }
  },
  "IpFilter": {
    "AllowedIPs": ["127.0.0.1"]
  }
}
```

## 3. Run migrations
```
dotnet ef migrations add AddIdentityTables
dotnet ef database update
```

## 4. Add [Authorize] to ClientsController, EmployeesController, ServicesController
```csharp
using Microsoft.AspNetCore.Authorization;

[Authorize]
public class ClientsController : Controller
{
    [Authorize(Roles = "Admin")]
    public IActionResult Create() { ... }

    [Authorize(Roles = "Admin")]
    public IActionResult Edit(int id) { ... }

    [Authorize(Roles = "Admin")]
    public IActionResult Delete(int id) { ... }
}
```

## 5. Add login/logout links in Views/Shared/_Layout.cshtml
```html
@if (User.Identity.IsAuthenticated)
{
    <span class="navbar-text">Hi, @User.Identity.Name</span>
    <a asp-controller="Account" asp-action="Logout" class="nav-link">Logout</a>
}
else
{
    <a asp-controller="Account" asp-action="Login" class="nav-link">Login</a>
}
```

## 6. SSL
Run with the https launch profile and trust your dev cert once:
```
dotnet dev-certs https --trust
```

## Quick test checklist
1. Register with your real phone number.
2. Log out, log back in, confirm you get a text code and it logs you in.
3. Click Sign in with Google, confirm account creation + login.
4. Try /Clients/Create as non-admin, confirm blocked.
5. Change IpFilter:AllowedIPs to exclude your IP, confirm 403 on the whole site.
