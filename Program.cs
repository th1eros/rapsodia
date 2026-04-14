using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
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

// 0. ENCODING + CARREGAR .ENV
// ---------------------------------------------------------
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envFile))
    Env.Load(envFile);

var builder = WebApplication.CreateBuilder(args);

// 1. CORS
// ---------------------------------------------------------
var allowedOrigins = new[]
{
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "https://ab1tat.github.io",
    "https://th1eros.github.io",
    "https://th1eros.com",     
    "https://www.th1eros.com",  
    "https://th1eros.dev"      
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// 2. BANCO DE DADOS (Refatorado para Segurança/DevSecOps)
// ---------------------------------------------------------
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ERRO CRÍTICO: String de conexão não encontrada. " +
        "Verifique se o arquivo .env está na raiz do projeto (mesma pasta do .csproj).");
}

var dbPass = Environment.GetEnvironmentVariable("DB_PASSWORD");
if (!string.IsNullOrEmpty(dbPass) && connectionString.Contains("{DB_PASSWORD}"))
{
    connectionString = connectionString.Replace("{DB_PASSWORD}", dbPass);
}

connectionString = Rapsodia.Infrastructure.PostgresConnectionString.Normalize(connectionString);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    })
);

// 3. INJEÇÃO DE DEPENDÊNCIA
// ---------------------------------------------------------
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IVulnService, VulnService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITelemetryService, TelemetryService>();

builder.Services.AddControllers().AddJsonOptions(x =>
{
    x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    x.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// 4. RATE LIMITING
// ---------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth-limit", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.QueueLimit = 0;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new ResponseModel<object>
        {
            Status = false,
            Mensagem = "Muitas tentativas. Tente novamente em 1 minuto."
        }, token);
    };
});

// 5. AUTENTICAÇÃO JWT
// ---------------------------------------------------------
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? builder.Configuration["Jwt:Key"];
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? builder.Configuration["Jwt:Issuer"] ?? "Rapsodia";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? builder.Configuration["Jwt:Audience"] ?? "Rapsodia_Clients";

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Chave JWT não configurada.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// 6. SWAGGER
// ---------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "aBitat- Rapsodia!", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Exemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
app.UsePathBase("/api");
app.UseForwardedHeaders();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 aBitat- Rapsodia! — Iniciando setup do sistema...");

// 7. GLOBAL ERROR HANDLING
// ---------------------------------------------------------
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = feature?.Error;
        var localLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        localLogger.LogError(exception, "❌ EXCEÇÃO NÃO TRATADA: {Message}", exception?.Message);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ResponseModel<object>
        {
            Status = false,
            Mensagem = app.Environment.IsDevelopment()
                ? $"Erro Interno: {exception?.Message}"
                : "Erro interno no servidor."
        });
    });
});

// 8. PIPELINE DE MIDDLEWARE
// ---------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;");
    await next();
});

app.UseSwagger();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger"))
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("v1/swagger.json", "aBitat- Rapsodia!");
        c.RoutePrefix = "swagger";
    });
}

app.UseRouting();
app.UseCors("DefaultPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "API Online", timestamp = DateTime.UtcNow }));
app.MapControllers();

// 9. SINCRONIZAÇÃO AUTOMÁTICA DO BANCO
// ---------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        logger.LogInformation("✅ Banco de dados sincronizado e tabelas prontas!");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "❌ ERRO AO PREPARAR O BANCO: {Message}", ex.Message);
    }
}

app.Run();