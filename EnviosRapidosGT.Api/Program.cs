using System.Text.Json.Serialization;
using EnviosRapidosGT.Api.Data;
using EnviosRapidosGT.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=envios.db"));

builder.Services.AddScoped<EnvioService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapGet("/", () => new
{
    mensaje = "API Envíos Rápidos GT funcionando",
    endpoints = new[]
    {
        "POST /api/envios",
        "GET /api/envios",
        "GET /api/envios/{codigo}",
        "PUT /api/envios/{codigo}/estado",
        "POST /api/envios/{codigo}/intento-fallido",
        "GET /api/envios/{codigo}/historial",
        "GET /api/reportes/eficiencia",
        "GET /health"
    }
});

app.MapGet("/health", () => new { status = "ok", fecha = DateTime.UtcNow });

app.MapControllers();

app.Run();

public partial class Program { }
