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
// 1. CONFIGURAÇÃO DE BANCO DE DADOS
// ============================================================

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// [CTO] Tradução de URL do Render (postgresql://) para formato .NET
if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgresql://"))
{
    try
    {
        var databaseUri = new Uri(connectionString);
        var userInfo = databaseUri.UserInfo.Split(':');

        connectionString = $"Host={databaseUri.Host};" +
                           $"Port={databaseUri.Port};" +
                           $"Database={databaseUri.AbsolutePath.TrimStart('/')};" +
                           $"Username={userInfo[0]};" +
                           $"Password={userInfo[1]};" +
                           "SslMode=Require;Trust Server Certificate=true;";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ AVISO: Erro ao converter URL do Render: {ex.Message}");
        // Continua com a string padrão
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ============================================================
// 2. CORS - Permite requisições do Frontend
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()           // Ajuste em produção para origem específica
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ============================================================
// 3. INJEÇÃO DE DEPENDÊNCIA (Dependency Injection)
// ============================================================

builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IVulnService, VulnService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// ============================================================
// 4. CONFIGURAÇÃO DE AUTENTICAÇÃO JWT
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
// 5. CONTROLLERS E JSON
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
        Description = "API corporativa para gerenciamento de ativos e vulnerabilidades",
        Contact = new OpenApiContact 
        { 
            Name = "Equipe de Segurança",
            Email = "security@svsharp.com"
        }
    });

    // [CISO] Configuração do Bearer Token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header\nExemplo: Bearer {seu_token_aqui}",
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
// 7. MIDDLEWARE PIPELINE
// ============================================================

// [CISO] Health Check na raiz para Render monitorar
app.MapGet("/", () => 
    Results.Json(new 
    { 
        status = "✅ API Online",
        version = "1.0.0",
        timestamp = DateTime.UtcNow,
        environment = app.Environment.EnvironmentName
    }));

// [CTO] Rota de health check padrão (alguns orquestradores usam /health)
app.MapGet("/health", () => 
    Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// ============================================================
// 8. TESTE DE CONEXÃO COM BANCO
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        if (context.Database.CanConnect())
        {
            Console.WriteLine("✅ [DATABASE] Conexão com PostgreSQL estabelecida com sucesso!");
        }
        else
        {
            Console.WriteLine("❌ [DATABASE] Falha ao conectar com PostgreSQL!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ [DATABASE] Erro ao conectar: {ex.GetType().Name} - {ex.Message}");
        // NÃO lança exceção - deixa a app iniciar mesmo sem DB
    }
}

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
// 10. MIDDLEWARE ORDER (IMPORTANTE!)
// ============================================================

app.UseCors("AllowAll");                    // CORS primeiro
app.UseHttpsRedirection();                  // Redirecionar HTTP → HTTPS (se em HTTPS)
app.UseAuthentication();                    // Autenticação
app.UseAuthorization();                     // Autorização
app.MapControllers();                       // Rotas dos controllers

// ============================================================
// 11. INICIALIZAR APLICAÇÃO
// ============================================================

try
{
    Console.WriteLine("🚀 Iniciando SVSharp API...");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"💥 Erro crítico na inicialização: {ex}");
    throw;
}
