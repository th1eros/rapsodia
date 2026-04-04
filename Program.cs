using Microsoft.AspNetCore.HttpOverrides;
using Rapsodia.Data;
using Rapsodia.Application.Interfaces;
using Rapsodia.Services.Assets;
using Rapsodia.Services.Vulns;
using Rapsodia.Services.Auth;
using Rapsodia.Services.Telemetries;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Rapsodia.DTO.Response;
using System.Text.Json;

// 0. CONFIGURAÇÃO DE ENCODING (FIX DOS SÍMBOLOS NO TERMINAL)
// ---------------------------------------------------------
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURAÇÃO DE CORS
// ---------------------------------------------------------
var allowedOrigins = new[]
{
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "https://ab1tat.github.io",
    "https://th1eros.github.io",
    "https://rapsodia-roij.onrender.com"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 2. CONFIGURAÇÃO DE BANCO DE DADOS (RENDER / LOCAL)
// ---------------------------------------------------------
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
                    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(connectionString) && (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://")))
{
    var databaseUri = new Uri(connectionString);
    var userInfo = databaseUri.UserInfo.Split(':');
    var dbPort = databaseUri.Port == -1 ? 5432 : databaseUri.Port;
    var host = databaseUri.Host;
    var dbName = databaseUri.AbsolutePath.TrimStart('/');

    connectionString = $"Host={host};Port={dbPort};Database={dbName};Username={userInfo[0]};Password={userInfo[1]};SslMode=Require;Trust Server Certificate=true;";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null
        );
        npgsqlOptions.CommandTimeout(60);
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
var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key") ?? builder.Configuration["Jwt:Key"];
var key = !string.IsNullOrEmpty(jwtKey) ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)) : null;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (key == null) throw new InvalidOperationException("Chave JWT não configurada.");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer") ?? builder.Configuration["Jwt:Issuer"] ?? "Rapsodia",
            ValidateAudience = true,
            ValidAudience = Environment.GetEnvironmentVariable("Jwt__Audience") ?? builder.Configuration["Jwt:Audience"] ?? "Rapsodia_Clients",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// 6. SWAGGER
// ---------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "aBitat API", Version = "v1" });
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
            new string[] {}
        }
    });
});

var app = builder.Build();

app.UseForwardedHeaders();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("🚀 aBitat API — Iniciando setup do sistema...");

// 7. MIDDLEWARE - GLOBAL ERROR HANDLING
// ---------------------------------------------------------
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;
        var localLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        localLogger.LogError(exception, "❌ EXCEÇÃO NÃO TRATADA: {Message}", exception?.Message);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ResponseModel<object>
        {
            Status = false,
            Mensagem = app.Environment.IsDevelopment() ? $"Erro Interno: {exception?.Message}" : "Erro interno no servidor."
        });
    });
});

// 8. MIDDLEWARE - PIPELINE
// ---------------------------------------------------------
if (!app.Environment.IsDevelopment())
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
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;");
    await next();
});

app.UseRouting();
app.UseCors("DefaultPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "aBitat API v1");
        c.RoutePrefix = "swagger";
    });
}

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "API Online", timestamp = DateTime.UtcNow }));
app.MapControllers();

// 9. SINCRONIZAÇÃO AUTOMÁTICA DO BANCO
// ---------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        logger.LogInformation("✅ Banco de dados sincronizado e tabelas prontas!");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "❌ ERRO AO PREPARAR O BANCO: {Message}", ex.Message);
    }
}

app.Run();