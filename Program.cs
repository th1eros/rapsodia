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

// ─── CONNECTION STRING ───────────────────────────────────────────────────────
// Lê de env var (Render) ou appsettings.json (local).
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string não configurada.");

// Traduz formato postgresql:// (Render) para formato Npgsql.
if (connectionString.StartsWith("postgresql://"))
{
    var uri      = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};" +
                       $"Database={uri.AbsolutePath.TrimStart('/')};" +
                       $"Username={userInfo[0]};Password={userInfo[1]};" +
                       "SslMode=Require;Trust Server Certificate=true;";
}

// ─── JWT KEY ─────────────────────────────────────────────────────────────────
// NUNCA commitar o valor real. Definir em env var: Jwt__Key no Render.
var jwtKey = Environment.GetEnvironmentVariable("Jwt__Key")
          ?? builder.Configuration["Jwt:Key"]
          ?? throw new InvalidOperationException("JWT Key não configurada.");

// ─── CORS ─────────────────────────────────────────────────────────────────────
// Em produção, definir env var CORS_ALLOWED_ORIGIN com a URL do frontend (Vercel/Netlify).
// Em dev, usa o padrão do Vite: http://localhost:5173.
var allowedOrigin = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGIN")
                 ?? builder.Configuration["Cors:AllowedOrigin"]
                 ?? "http://localhost:5173";

// ─── BANCO DE DADOS ───────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// ─── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(allowedOrigin)
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ─── AUTENTICAÇÃO JWT ─────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ─── SERVIÇOS ─────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IVulnService, VulnService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// ─── CONTROLLERS + JSON ───────────────────────────────────────────────────────
// JsonStringEnumConverter: frontend TypeScript recebe "Alta" em vez de 2.
// IgnoreCycles: evita loop infinito nas navegações N:N.
builder.Services.AddControllers().AddJsonOptions(x =>
{
    x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// ─── HEALTH CHECK ─────────────────────────────────────────────────────────────
// Render usa /health para saber se o container está vivo.
builder.Services.AddHealthChecks();

// ─── SWAGGER ──────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SVSharp API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT no header. Exemplo: Bearer {token}",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
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

// ─── PIPELINE ─────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SVSharp API v1");
    c.RoutePrefix = "swagger";
});

// Health check para o Render (deve vir antes dos demais middlewares).
app.MapHealthChecks("/health");

app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ─── VERIFICAÇÃO DE CONEXÃO NO STARTUP ───────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    try
    {
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        ctx.Database.CanConnect();
        Console.WriteLine("✅ PostgreSQL conectado.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Falha na conexão com o banco: {ex.Message}");
    }
}

app.Run();
