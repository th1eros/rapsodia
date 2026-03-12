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

// 1. CONFIGURAÇÃO DE SERVIÇOS
// ---------------------------------------------------------

// [CISO] Extrai a string de conexão com prioridade: Variável de Ambiente > appsettings.json
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// [CTO] Tradutor de URL do Render para .NET (Render usa formato postgresql://)
if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgresql://"))
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

// [CISO] Log da configuração do banco (SEM a senha!)
var dbHostFromConfig = connectionString?.Split(';').FirstOrDefault(x => x.Contains("Host="))?.Replace("Host=", "") ?? "UNKNOWN";
Console.WriteLine($"[STARTUP] Banco de dados configurado: {dbHostFromConfig}");

// Configurar DbContext com Npgsql
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// [CISO] CORS - Permitir comunicação segura com Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// [CTO] Registrar serviços de negócio
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IVulnService, VulnService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// [CTO] Configurar Controllers com suporte a JSON e referências cíclicas
builder.Services.AddControllers().AddJsonOptions(x =>
    x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// [CISO] Documentação com Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "SVSharp API - Gestão de Ativos & Vulnerabilidades", 
        Version = "v1.0",
        Description = "API REST de nível corporativo para gerenciamento centralizado de ativos tecnológicos e vulnerabilidades."
    });
    
    // [CISO] Suporte a JWT no Swagger
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

// 2. MIDDLEWARE E HEALTH CHECKS (CRÍTICO PARA RENDER)
// ---------------------------------------------------------

// [CIO] 🚨 ROTA DE SAÚDE - O RENDER DEPENDE DISSO!
// Esta rota é verificada continuamente pelo Render durante o deploy
// Se não responder em 5-10 segundos, o deploy falha
app.MapGet("/health", async (AppDbContext context) =>
{
    try
    {
        // Tentar conectar ao banco
        var canConnect = await context.Database.CanConnectAsync();
        
        if (canConnect)
        {
            // [CISO] Retorna dados estruturados que o Render entende
            return Results.Ok(new 
            { 
                status = "healthy",
                timestamp = DateTime.UtcNow,
                database = "connected",
                message = "SVSharp API is operational"
            });
        }
        else
        {
            return Results.ServiceUnavailable(new 
            { 
                status = "unhealthy",
                database = "disconnected",
                message = "Database connection failed"
            });
        }
    }
    catch (Exception ex)
    {
        // [CISO] Log de erro para debug
        Console.WriteLine($"[HEALTH_CHECK_ERROR] {ex.Message}");
        
        return Results.ServiceUnavailable(new 
        { 
            status = "error",
            message = ex.Message
        });
    }
})
.WithName("Health")
.WithOpenApi()
.Produces(200)
.Produces(503);

// [CIO] Rota de Readiness - Verifica se está pronto para receber tráfego
app.MapGet("/health/ready", async (AppDbContext context) =>
{
    try
    {
        // Executar uma query simples para validar conexão de verdade
        var dbVersion = await context.Database.ExecuteScalarAsync<string>("SELECT version()");
        
        return Results.Ok(new 
        { 
            status = "ready",
            database = "responding",
            version = dbVersion
        });
    }
    catch
    {
        return Results.ServiceUnavailable();
    }
})
.WithName("Readiness")
.WithOpenApi();

// [CIO] Rota raiz - Informações da API
app.MapGet("/", () => Results.Ok(new 
{ 
    status = "API Online",
    service = "SVSharp",
    version = "1.0",
    message = "Acesse /swagger para documentação",
    endpoints = new
    {
        swagger = "/swagger",
        health = "/health",
        readiness = "/health/ready",
        api = "/api/*"
    }
}))
.WithName("Root")
.WithOpenApi();

// 3. TESTE DE CONEXÃO COM BANCO (EXECUTA APENAS UMA VEZ NO STARTUP)
// ---------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        // [CTO] Teste simples e rápido
        var canConnect = await context.Database.CanConnectAsync();
        
        if (canConnect)
        {
            logger.LogInformation("✅ [STARTUP] Conexão com PostgreSQL estabelecida com sucesso!");
            Console.WriteLine("✅ [STARTUP] Banco de dados: ONLINE");
            
            // [CISO] Opcional: Aplicar migrations automaticamente em dev
            if (app.Environment.IsDevelopment())
            {
                logger.LogInformation("[MIGRATION] Aplicando migrations pendentes...");
                // await context.Database.MigrateAsync(); // Descomente se quiser auto-migrate
            }
        }
        else
        {
            logger.LogWarning("⚠️  [STARTUP] Não foi possível conectar ao PostgreSQL");
            Console.WriteLine("⚠️  [STARTUP] Banco de dados: OFFLINE (API iniciará, mas sem persistência)");
        }
    }
    catch (Exception ex)
    {
        logger.LogError($"❌ [STARTUP] Erro ao conectar no banco: {ex.Message}");
        Console.WriteLine($"❌ [STARTUP] Erro: {ex.Message}");
        
        // [CTO] NÃO falha a aplicação - Render vai tentar reconectar
        // A aplicação responde ao /health mesmo sem o banco no início
    }
}

// 4. MIDDLEWARE PIPELINE
// ---------------------------------------------------------

// [CTO] Swagger deve estar disponível ANTES da autenticação
app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SVSharp API v1");
    c.RoutePrefix = "swagger"; // Força o swagger a ficar em /swagger
});

// [CISO] Habilitar CORS para integração com frontend
app.UseCors("AllowAll");

// [CISO] Middleware de autenticação e autorização
app.UseAuthentication();
app.UseAuthorization();

// [CTO] Mapear todos os controllers
app.MapControllers();

// 5. LOG DE INICIALIZAÇÃO
// ---------------------------------------------------------
Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                  🛡️  SVSharp API - INICIADA                     ║");
Console.WriteLine("╠════════════════════════════════════════════════════════════════╣");
Console.WriteLine($"║ URL: http://localhost:5073                                    ║");
Console.WriteLine($"║ Swagger: http://localhost:5073/swagger                        ║");
Console.WriteLine($"║ Health: http://localhost:5073/health                          ║");
Console.WriteLine($"║ Ambiente: {app.Environment.EnvironmentName,-44} ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");

app.Run();
