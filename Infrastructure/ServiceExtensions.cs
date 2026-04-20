using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Rapsodia.Application.Interfaces;
using Rapsodia.Data;
using Rapsodia.DTO.Response;
using Rapsodia.Services.Assets;
using Rapsodia.Services.Auth;
using Rapsodia.Services.Telemetries;
using Rapsodia.Services.Vulns;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class ServiceExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
        // 1. CORS
        var extraOrigins = (Environment.GetEnvironmentVariable("EXTRA_ALLOWED_ORIGINS") ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var allowedOrigins = env.EnvironmentName switch
        {
            "Development" => new[] { "http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:5073" },
            "Staging" => new[] { "https://th1eros.dev", "http://localhost:5173" },
            _ => new[] { "https://th1eros.com", "https://www.th1eros.com" }
        };

        var finalOrigins = allowedOrigins.Concat(extraOrigins).Distinct().ToArray();

        services.AddCors(options =>
            options.AddPolicy("DefaultPolicy", policy =>
                policy.WithOrigins(finalOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

        // 2. DI & Controllers
        services.AddControllers().AddJsonOptions(x => {
            x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            x.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IVulnService, VulnService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ITelemetryService, TelemetryService>();

        // 3. Rate Limiting
        services.AddRateLimiter(options => {
            options.AddFixedWindowLimiter("auth-limit", opt => {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = 5;
                opt.QueueLimit = 0;
            });
            options.OnRejected = async (context, token) => {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new ResponseModel<object> { Status = false, Mensagem = "Muitas tentativas. Tente novamente em 1 minuto." }, token);
            };
        });

        // 4. JWT
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? config["Jwt:Key"] ?? throw new InvalidOperationException("❌ JWT_KEY ausente.");
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? config["Jwt:Issuer"] ?? throw new InvalidOperationException("❌ JWT_ISSUER ausente.");
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? config["Jwt:Audience"] ?? throw new InvalidOperationException("❌ JWT_AUDIENCE ausente.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        // 5. Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c => {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "aBitat – Rapsodia!", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header. Exemplo: \"Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
            });
        });

        return services;
    }
}