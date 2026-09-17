using Microsoft.EntityFrameworkCore;
using MVCSampleApp;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
    options.Cookie.HttpOnly = true; // Ensures the session cookie is accessible only by the server
    options.Cookie.IsEssential = true; // Required for GDPR compliance
});

// 2. Add services to the container (Configured for SQLite on macOS)
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<MVCSampleApp.AppContext>(options => 
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MVCSampleApp.AppContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Session middleware must be placed BEFORE MapControllerRoute
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();