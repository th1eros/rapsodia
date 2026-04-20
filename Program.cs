using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Diagnostics;
using Rapsodia.Data;
using Rapsodia.DTO.Response;
using Rapsodia.Infrastructure; 
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envFile)) Env.Load(envFile);

var builder = WebApplication.CreateBuilder(args);

builder.Configuration

    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

Console.WriteLine("🔍 DEBUG: Listando chaves de configuração carregadas:");
foreach (var pair in builder.Configuration.AsEnumerable())
{
    if (pair.Key.Contains("ConnectionStrings"))
    {
        Console.WriteLine($"🔑 {pair.Key} = {pair.Value}");
    }
}

builder.Configuration["AllowedHosts"] = "*";
builder.WebHost.ConfigureKestrel(options => { options.AllowSynchronousIO = true; options.DisableStringReuse = false; });

// Validações
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? builder.Configuration["DB_PASSWORD"] ?? throw new InvalidOperationException("❌ DB_PASSWORD não encontrada!");
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                       ?? builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("❌ String de conexão não encontrada. Verifique seu .env ou appsettings.");

if (connectionString.Contains("{DB_PASSWORD}")) connectionString = connectionString.Replace("{DB_PASSWORD}", dbPassword);
var dbPort = Environment.GetEnvironmentVariable("DB_PORT_RUNTIME") ?? "5432";
if (connectionString.Contains("{DB_PORT_RUNTIME}")) connectionString = connectionString.Replace("{DB_PORT_RUNTIME}", dbPort);

connectionString = Rapsodia.Infrastructure.PostgresConnectionString.Normalize(connectionString);

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString, npg => npg.EnableRetryOnFailure(5)));
builder.Services.AddAppServices(builder.Configuration, builder.Environment);

var app = builder.Build();

// Pipeline
app.UseExceptionHandler(errorApp => errorApp.Run(async context => {
    var feature = context.Features.Get<IExceptionHandlerPathFeature>();
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsJsonAsync(new ResponseModel<object> { Status = false, Mensagem = "Erro interno no servidor." });
}));

if (!app.Environment.IsDevelopment()) { app.UseHttpsRedirection(); app.UseHsts(); }

app.Use(async (context, next) => {
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;");
    await next();
});

app.UseSwagger();
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger")) app.UseSwaggerUI();

app.UseRouting();
app.UseCors("DefaultPolicy");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "API Online", timestamp = DateTime.UtcNow }));
app.MapControllers();


Console.WriteLine($"DEBUG: ConnectionString é nula? {string.IsNullOrEmpty(connectionString)}");
Console.WriteLine($"DEBUG: Valor da ConnectionString: '{connectionString}'");

MigrationRunner.ApplyMigrations(app.Services, connectionString!, dbPassword!);

app.Run();