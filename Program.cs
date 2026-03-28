using API_SVsharp.Data;
using API_SVsharp.Application.Interfaces;
using API_SVsharp.Services.Assets;
using API_SVsharp.Services.Vulns;
using API_SVsharp.Services.Auth;
using API_SVsharp.Services.Telemetries;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using API_SVsharp.DTO.Response;

var builder = WebApplication.CreateBuilder(args);

// 0. LOGGING E CONFIGURAÇÃO INICIAL
// ---------------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 1. CONFIGURAÇÃO DE CORS (Whitelist)
// ---------------------------------------------------------
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                    ?? new[] { "http://localhost:3000", "http://localhost:5173" }; 

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// 2. CONFIGURAÇÃO DE BANCO DE DADOS (RENDER / LOCAL)
// ---------------------------------------------------------
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgresql://"))
{
    var databaseUri = new Uri(connectionString);
    var userInfo = databaseUri.UserInfo.Split(':');
    var port = databaseUri.Port == -1 ? 5432 : databaseUri.Port;

    connectionString = $"Host={databaseUri.Host};Port={port};Database={databaseUri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SslMode=Require;Trust Server Certificate=true;";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

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

var key = !string.IsNullOrEmpty(jwtKey) 
    ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    : null;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (key == null) throw new InvalidOperationException("Chave JWT não configurada.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = Environment.GetEnvironmentVariable("Jwt__Issuer") ?? builder.Configuration["Jwt:Issuer"] ?? "API_SVsharp",
            ValidateAudience = true,
            ValidAudience = Environment.GetEnvironmentVariable("Jwt__Audience") ?? builder.Configuration["Jwt:Audience"] ?? "API_SVsharp_Clients",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// 6. SWAGGER
// ---------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SVSharp API", Version = "v1" });
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

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 SVSharp API — Iniciando setup do sistema...");

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

        var response = new ResponseModel<object>
        {
            Status = false,
            Mensagem = app.Environment.IsDevelopment() 
                ? $"Erro Interno: {exception?.Message}" 
                : "Ocorreu um erro interno no servidor. Por favor, tente novamente mais tarde."
        };

        await context.Response.WriteAsJsonAsync(response);
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
    // CSP ajustada: permite inline scripts/styles necessários para Swagger e SPAs modernos
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;");
    await next();
});

app.UseRouting();
app.UseCors("DefaultPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SVSharp API v1");
        c.RoutePrefix = "swagger";
    });
}

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
