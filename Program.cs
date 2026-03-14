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

// 1. CONFIGURAÇÃO DE CORS - DEVE VIR PRIMEIRO (ANTES DE AddControllers)
// ---------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin() // Aceita qualquer origem (resolve o erro na hora)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 2. CONFIGURAÇÃO DE BANCO DE DADOS
// ---------------------------------------------------------

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// [CTO] Tradutor de URL do Render para .NET
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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. INJEÇÃO DE DEPENDÊNCIA
// ---------------------------------------------------------

builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IVulnService, VulnService>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddControllers().AddJsonOptions(x =>
    x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// 4. AUTENTICAÇÃO JWT
// ---------------------------------------------------------

var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key") ?? builder.Configuration["Jwt:Key"] ?? "CHAVE_ULTRA_SECRETA_DETECCAO_DE_VULNERABILIDADES_2026";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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

// 5. SWAGGER
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

// 6. MIDDLEWARE E TESTE DE CONEXÃO
// ---------------------------------------------------------

// [CIO] Rota de Health Check (Prioridade máxima para o Render)
app.MapGet("/health", () => Results.Ok(new { status = "API Online", message = "Acesse /swagger para documentação" }));

app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SVSharp API v1");
    c.RoutePrefix = "swagger";
});

// [CISO] CORS DEVE VIR ANTES DE UseAuthentication
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// [CTO] Teste de conexão no log para o estagiário ver
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // O Migrate é o comando mágico que cria as tabelas no Render
        context.Database.Migrate();

        Console.WriteLine("✅ Banco de dados sincronizado e tabelas prontas!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erro ao preparar o banco: {ex.Message}");
    }
}

app.Run();