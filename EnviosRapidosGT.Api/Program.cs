using System.Text.Json.Serialization;
using EnviosRapidosGT.Api.Data;
using EnviosRapidosGT.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Base de datos SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=envios.db"
    )
);

// Servicios propios
builder.Services.AddScoped<EnvioService>();

// Controladores y configuración JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Crear la base de datos automáticamente si no existe
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Activar Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Ruta principal
app.MapGet("/", () => new
{
    mensaje = "API Envios Rapidos GT funcionando",
    swagger = "/swagger",
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

// Ruta de salud para Render
app.MapGet("/health", () => new
{
    status = "ok",
    fecha = DateTime.UtcNow
});

app.MapControllers();

app.Run();

public partial class Program { }