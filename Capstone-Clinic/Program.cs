using Capstone_Clinic.Data;
using Capstone_Clinic.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Identity;
using Capstone_Clinic.Models;
using Capstone_Clinic.Models.Authorization;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Do not start the application with a missing/weak JWT signing key.
// Configure Jwt:Key through .NET User Secrets for development and a secret
// store/environment variable for hosted environments.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) ||
    System.Text.Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be configured as a secret with at least 32 bytes. " +
        "Use .NET User Secrets in development and environment/secret-store configuration when hosted.");
}

// Add services to the container.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        CookieAuthenticationDefaults.AuthenticationScheme;

    options.DefaultSignInScheme =
        CookieAuthenticationDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(
    CookieAuthenticationDefaults.AuthenticationScheme,
    options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    })
.AddCookie(
    "StudentScheme",
    options =>
    {
        options.LoginPath = "/StudentLogin";
        options.AccessDeniedPath = "/StudentLogin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    })
.AddJwtBearer(
    "MobileJwt",
    options =>
    {
        options.TokenValidationParameters =
            new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromSeconds(30),

                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],

                IssuerSigningKey =
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(jwtKey))
            };
    });
builder.Services.AddAuthorization(options =>
{
    // All interactive CMS pages require a recognized staff role.
    options.AddPolicy("StaffAccess", policy =>
        policy.RequireRole(StaffRoles.Admin, StaffRoles.ClinicPersonnel));

    // Account creation is a privileged administrative function.
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(StaffRoles.Admin));
});
builder.Services.AddHttpClient<Esp32Service>(c => c.Timeout = TimeSpan.FromSeconds(90));
builder.Services.AddSingleton<IoTDevicePresence>();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/", "StaffAccess");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/AccessDenied");
    options.Conventions.AuthorizeFolder("/StaffManagement", "AdminOnly");
});
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CmsTestPwa", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Clinic Monitoring System Mobile API",
        Version = "v1",
        Description = "Backend API for the CMS student mobile application."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("CmsTestPwa");

app.UseStaticFiles();

app.UseRouting();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
