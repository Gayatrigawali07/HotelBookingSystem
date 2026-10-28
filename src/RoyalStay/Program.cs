using Microsoft.AspNetCore.Authentication.Cookies;
using RoyalStay.Data;

var builder = WebApplication.CreateBuilder(args);

// Data access (ADO.NET / Microsoft.Data.SqlClient) - one instance per web request
builder.Services.AddScoped<BookingRepository>();
builder.Services.AddScoped<RoomRepository>();
builder.Services.AddScoped<UserRepository>();

builder.Services.AddMemoryCache(); // used for login throttling

// Cookie authentication for the admin
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "RoyalStay.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Friendly error page in every environment (database errors never show a stack trace to guests).
app.UseExceptionHandler("/Home/Error");
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Create the first admin from configuration (only if no admin exists yet)
AdminSeeder.Seed(app.Services, app.Logger);

app.Run();
