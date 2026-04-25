using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging.EventLog;
using Holonix.Server.Application.Handlers.Auth;
using Holonix.Server.Application.Interfaces;
using Holonix.Server.Domain.Entities;
using Holonix.Server.Infrastructure.Configuration;
using Holonix.Server.Infrastructure.Data;
using Holonix.Server.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

if (OperatingSystem.IsWindows())
{
    builder.Logging.AddFilter<EventLogLoggerProvider>(null, LogLevel.None);
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<MapboxOptions>(builder.Configuration.GetSection(MapboxOptions.SectionName));

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.User.RequireUniqueEmail = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
if (jwtOptions is null ||
    string.IsNullOrWhiteSpace(jwtOptions.Key) ||
    string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience))
{
    throw new InvalidOperationException("Jwt configuration is missing or invalid.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromMinutes(2),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddHttpClient<IGeocodingService, MapboxGeocodingService>((serviceProvider, client) =>
{
    var mapboxOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MapboxOptions>>().Value;
    client.BaseAddress = new Uri(mapboxOptions.BaseUrl);
});
builder.Services.AddScoped<IRegisterUserHandler, RegisterUserHandler>();
builder.Services.AddScoped<ILoginUserHandler, LoginUserHandler>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientApp", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var parsed))
                {
                    return false;
                }

                return string.Equals(parsed.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(parsed.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase);
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

await CountrySeeder.SeedAsync(app.Services);
await BusinessRoleSeeder.SeedAsync(app.Services);
await ServiceSeeder.SeedAsync(app.Services);
await WorkloadReferenceSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Local dev uses HTTP; HTTPS can be enabled when certs are trusted.
app.UseCors("ClientApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
