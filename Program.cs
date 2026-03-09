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
// 1. CONFIGURAÇÃO DE INFRA (CIO / CTO)
// ============================================================
// [CISO] Ocultar assinatura do servidor Kestrel por segurança
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// [CIO] Porta dinâmica para suporte ao Render/Azure/Containers
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

// ============================================================
// 2. CONTROLLERS & SERIALIZAÇÃO
// ============================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Converte Enums para String no JSON (Melhor leitura no Frontend)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ============================================================
// 3. SEGURANÇA: JWT AUTHENTICATION (CISO Standard)
// ============================================================
var jwtSettings = builder.Configuration.GetSection("Jwt");
var keyString = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key não configurada.");
var key = Encoding.UTF8.GetBytes(keyString);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true; // Força HTTPS
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero // Expiração rígida
        };
    });

builder.Services.AddAuthorization();

// ============================================================
// 4. INFRA: CORS (Restrição de Origem Segura)
// ============================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("CyberPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",             // Desenvolvimento
                "https://th1eros.github.io",         // Produção Dashboard
                "https://api-svsharp.onrender.com"   // Sua própria URL no Render
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ============================================================
// 5. DOCUMENTAÇÃO: SWAGGER COM JWT
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
options.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "SVSharp API - Cybersecurity Dashboard",
    Version = "v1",
    Description = "API de Gestão de Ativos e Vulnerabilidades"
});

options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Description = "Autenticação JWT. Digite: Bearer {seu_token}",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT"
});