using English.Authorization;
using English.Interfaces;
using English.Models;
using English.Models.Entities;
using English.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using English.Data;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".English.Auth";
        options.LoginPath = "/Account/Login";
    });

builder.Services.AddScoped<IPasswordHasher<AspNetUser>, PasswordHasher<AspNetUser>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthorizationHandler, ActiveAccountHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.ActiveAccount, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new ActiveAccountRequirement());
    });

    options.AddPolicy(AuthorizationPolicies.StudentOnly, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(nameof(UserRole.Student));
        policy.AddRequirements(new ActiveAccountRequirement());
    });

    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole(nameof(UserRole.Admin));
        policy.AddRequirements(new ActiveAccountRequirement());
    });
});

var app = builder.Build();

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
