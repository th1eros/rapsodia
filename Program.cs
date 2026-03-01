using API_SVsharp.Data;
using API_SVsharp.Application.Interfaces;
using API_SVsharp.Services.Assets;
using API_SVsharp.Services.Vulns;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Configuração de enums como string
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ==========================
// CORS
// ==========================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Services (Injeção de Dependência)
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IVulnService, VulnService>();

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// CORS
app.UseCors("AllowReact");

app.UseAuthorization();

app.MapControllers();

app.Run();