using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MVCSampleApp;
using MVCSampleApp.Models;
using MVCSampleApp.Middleware;
using AppContext = MVCSampleApp.AppContext;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Existing DB connection - keep your current connection string setup
builder.Services.AddDbContext<AppContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


// Identity - handles login, roles, and the 2FA plumbing
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Relaxed rules since this is just for a class assignment
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<AppContext>()
    .AddDefaultTokenProviders();

// SSO - Google login
builder.Services.AddAuthentication()
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// SSL
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// IP Filtering - runs before auth so blocked IPs never even reach login
app.UseMiddleware<IpFilterMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed roles + a default admin user on startup
using (var scope = app.Services.CreateScope())
{
    await SeedData.SeedRolesAndAdminAsync(scope.ServiceProvider);
}

app.Run();