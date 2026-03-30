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
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Rapsodia.DTO.Response;

var builder = WebApplication.CreateBuilder(args);

// Configuração para o Render (Porta dinâmica)
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://*:{port}");

// Adicionar suporte a Forwarded Headers (Necessário para Render/Proxies)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
// 1. CONFIGURAÇÃO DE CORS (Whitelist)
// ---------------------------------------------------------
var corsOrigin = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGIN");
var allowedOrigins = !string.IsNullOrEmpty(corsOrigin)
    ? corsOrigin.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    : new[] {
        "http://localhost:3000",
        "http://localhost:5173",
        "http://127.0.0.1:5173",
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
// 2. CONFIGURAÃ‡ÃƒO DE BANCO DE DADOS (RENDER / LOCAL)
// ---------------------------------------------------------
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") // Prioridade para variável padrão do Render
                    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(connectionString) && (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://")))
{
    // Converte formato postgres://usuario:senha@host:porta/database para Npgsql
    var databaseUri = new Uri(connectionString);
    var userInfo = databaseUri.UserInfo.Split(':');
    var dbPort = databaseUri.Port == -1 ? 5432 : databaseUri.Port;
    var host = databaseUri.Host;
    var dbName = databaseUri.AbsolutePath.TrimStart('/');

    connectionString = $"Host={host};Port={dbPort};Database={dbName};Username={userInfo[0]};Password={userInfo[1]};SslMode=Require;Trust Server Certificate=true;";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. INJEÃ‡ÃƒO DE DEPENDÃŠNCIA
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

// 5. AUTENTICAÃ‡ÃƒO JWT
// ---------------------------------------------------------
var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key") ?? builder.Configuration["Jwt:Key"];

var key = !string.IsNullOrEmpty(jwtKey) 
    ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    : null;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (key == null) throw new InvalidOperationException("Chave JWT nÃ£o configurada.");

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

// Habilitar Forwarded Headers (CRÍTICO para Render)
app.UseForwardedHeaders();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
// ...
// (rest of the file remains same, but let's update allowedOrigins too)
logger.LogInformation("ðŸš€ aBitat API â€” Iniciando setup do sistema...");

// 7. MIDDLEWARE - GLOBAL ERROR HANDLING
// ---------------------------------------------------------
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        var localLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        localLogger.LogError(exception, "âŒ EXCEÃ‡ÃƒO NÃƒO TRATADA: {Message}", exception?.Message);

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
    // CSP ajustada: permite inline scripts/styles necessÃ¡rios para Swagger e SPAs modernos
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;");
    await next();
});

app.UseRouting();
app.UseCors("DefaultPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment() || true) // Forçamos a exibição para teste inicial no Render
{
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "aBitat API v1");
        c.RoutePrefix = "swagger"; // Acessível em /swagger
    });
}

// Rota raiz para evitar 404 e confirmar que a API está ativa
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/health", () => Results.Ok(new { status = "API Online", timestamp = DateTime.UtcNow }));
app.MapControllers();

// 9. SINCRONIZAÃ‡ÃƒO AUTOMÃTICA DO BANCO
// ---------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        logger.LogInformation("âœ… Banco de dados sincronizado e tabelas prontas!");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "âŒ ERRO AO PREPARAR O BANCO: {Message}", ex.Message);
    }
}

app.Run();
