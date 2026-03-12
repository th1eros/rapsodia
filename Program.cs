using API_SVsharp.Data;
using API_SVsharp.Application.Interfaces;
using API_SVsharp.Services.Assets;
using API_SVsharp.Services.Vulns;
using API_SVsharp.Services.Auth;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. CONFIGURAÇÃO DE BANCO DE DADOS COM DEBUG
// ============================================================

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection");

Console.WriteLine("🔍 [DEBUG] ConnectionString Original:");
Console.WriteLine($"   {MaskPassword(connectionString ?? "")}");

// [CTO] Tradução de URL do Render (postgresql://) para formato .NET
if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgresql://"))
{
    Console.WriteLine("🔄 [DEBUG] Convertendo postgresql:// para formato .NET...");
    try
    {
        var databaseUri = new Uri(connectionString);
        var userInfo = databaseUri.UserInfo.Split(':');

        connectionString = $"Host={databaseUri.Host};" +
                           $"Port={databaseUri.Port};" +
                           $"Database={databaseUri.AbsolutePath.TrimStart('/')};" +
                           $"Username={userInfo[0]};" +
                           $"Password={userInfo[1]};" +
                           "SslMode=Require;Trust Server Certificate=true;Connection Timeout=30;";

        Console.WriteLine("✅ [DEBUG] Conversão bem-sucedida!");
        Console.WriteLine($"   Host: {databaseUri.Host}");
        Console.WriteLine($"   Port: {databaseUri.Port}");
        Console.WriteLine($"   Database: {databaseUri.AbsolutePath.TrimStart('/')}");
        Console.WriteLine($"   Username: {userInfo[0]}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ [DEBUG] ERRO NA CONVERSÃO: {ex.GetType().Name}");
        Console.WriteLine($"   Mensagem: {ex.Message}");
    }
}

Console.WriteLine($"\n📋 [DEBUG] ConnectionString Final:");
Console.WriteLine($"   {MaskPassword(connectionString ?? "")}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions
            .CommandTimeout(60)
            .EnableRetryOnFailure(maxRetryCount: 3)
    ));

// ============================================================
// 2. CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ============================================================
// 3. DEPENDENCY INJECTION
// ============================================================

builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IVulnService, VulnService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// ============================================================
// 4. JWT CONFIGURATION
// ============================================================

var jwtKey = builder.Configuration["Jwt:Key"]
           ?? Environment.GetEnvironmentVariable("Jwt__Key")
           ?? "CHAVE_ULTRA_SECRETA_DETECCAO_DE_VULNERABILIDADES_2026";

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
              ?? Environment.GetEnvironmentVariable("Jwt__Issuer")
              ?? "API_SVsharp";

var jwtAudience = builder.Configuration["Jwt:Audience"]
                ?? Environment.GetEnvironmentVariable("Jwt__Audience")
                ?? "API_SVsharp_Clients";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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

// ============================================================
// 5. CONTROLLERS AND JSON
// ============================================================

builder.Services
    .AddControllers()
    .AddJsonOptions(x =>
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// ============================================================
// 6. SWAGGER/OPENAPI
// ============================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SVSharp API - Gestão de Vulnerabilidades",
        Version = "v1.0",
        Description = "API corporativa para gerenciamento de ativos e vulnerabilidades"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header",
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

// ============================================================
// BUILD APPLICATION
// ============================================================

var app = builder.Build();

// ============================================================
// 7. HEALTH CHECK ROUTES
// ============================================================

app.MapGet("/", () =>
    Results.Json(new
    {
        status = "✅ API Online",
        version = "1.0.0",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    }));

app.MapGet("/health", () =>
    Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// ============================================================
// 8. DATABASE CONNECTION TEST
// ============================================================

Console.WriteLine("\n" + new string('=', 60));
Console.WriteLine("🔌 INICIANDO TESTE DE CONEXÃO COM BANCO DE DADOS");
Console.WriteLine(new string('=', 60));

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        Console.WriteLine("\n⏳ Tentando obter DbContext...");
        var context = services.GetRequiredService<AppDbContext>();

        Console.WriteLine("✅ DbContext obtido com sucesso");

        Console.WriteLine("\n⏳ Tentando conectar ao banco (CanConnect)...");

        if (context.Database.CanConnect())
        {
            Console.WriteLine("✅ [DATABASE] Conexão com PostgreSQL estabelecida com sucesso!");
            Console.WriteLine($"   Database: {context.Database.GetDbConnection().Database}");
            Console.WriteLine($"   Server: {context.Database.GetDbConnection().DataSource}");
        }
        else
        {
            Console.WriteLine("❌ [DATABASE] CanConnect retornou false!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n❌ [DATABASE] ERRO AO CONECTAR:");
        Console.WriteLine($"   Tipo: {ex.GetType().Name}");
        Console.WriteLine($"   Mensagem: {ex.Message}");

        if (ex.InnerException != null)
        {
            Console.WriteLine($"\n   🔸 Inner Exception ({ex.InnerException.GetType().Name}):");
            Console.WriteLine($"      {ex.InnerException.Message}");
        }

        Console.WriteLine("\n   💡 Possíveis causas:");
        if (ex.Message.Contains("password authentication failed", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("      → Senha incorreta no banco");
        }
        if (ex.Message.Contains("could not translate host name", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("      → Host/domínio não encontrado");
        }
        if (ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("      → Timeout na conexão (banco pode estar offline)");
        }
        if (ex.Message.Contains("SSL", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("      → Problema com SSL/TLS");
        }
    }
}

Console.WriteLine("\n" + new string('=', 60));

// ============================================================
// 9. SWAGGER UI
// ============================================================

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SVSharp API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
    });
}

// ============================================================
// 10. MIDDLEWARE PIPELINE
// ============================================================

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ============================================================
// 11. START APPLICATION
// ============================================================

try
{
    Console.WriteLine("\n🚀 Iniciando SVSharp API...");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"\n💥 Erro crítico na inicialização:");
    Console.WriteLine($"   {ex.GetType().Name}: {ex.Message}");
    throw;
}

// ============================================================
// HELPER FUNCTION
// ============================================================

static string MaskPassword(string connectionString)
{
    if (string.IsNullOrEmpty(connectionString))
        return "(vazio)";

    return System.Text.RegularExpressions.Regex.Replace(
        connectionString,
        @"(Password|password)=([^;]*)",
        "Password=***");
}
