using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ProceraAssessment.Data;
using ProceraAssessment.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// CONFIGURE COOKIE AUTHENTICATION
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";

        // Security best practices
        options.Cookie.HttpOnly = true; // Prevents JavaScript/XSS from stealing the cookie
        options.Cookie.SameSite = SameSiteMode.Lax; // Protects against broad CSRF attacks

        // Use 'Always' in production, but allow HTTP during local development
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    });

var app = builder.Build();

// INITIAL USER PROVISIONING (Development Only)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        var adminUsername = config["AssessmentAdmin:Username"];
        var adminPassword = config["AssessmentAdmin:Password"];

        if (!string.IsNullOrEmpty(adminUsername) && !string.IsNullOrEmpty(adminPassword))
        {
            // Only provision if the username doesn't already exist
            if (!context.Users.Any(u => u.Username == adminUsername))
            {
                // Hash the password using BCrypt before storing it
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword);

                context.Users.Add(new User
                {
                    Username = adminUsername,
                    PasswordHash = hashedPassword
                });
                context.SaveChanges();
                logger.LogInformation("Initial assessment user provisioned successfully.");
            }
            else
            {
                logger.LogInformation("Initial assessment user already exists. Provisioning skipped.");
            }
        }
        else
        {
            logger.LogWarning("AssessmentAdmin credentials not found in configuration. Provisioning skipped.");
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();